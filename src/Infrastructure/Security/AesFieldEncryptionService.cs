using System.Buffers;
using System.Security.Cryptography;
using System.Text;
using Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Security;

/// <summary>
/// AES-256-GCM field-level encryption service with per-tenant key derivation.
///
/// Design:
///   - Master key is read from the FIELD_ENCRYPTION_KEY environment variable (or config).
///   - A unique 256-bit subkey is derived per tenant via HKDF-SHA256 so tenants cannot
///     decrypt each other's data even if they share the same database.
///   - Each encrypted value uses a random 12-byte nonce (IV) and produces a 16-byte auth tag.
///   - Ciphertext format: "ENC:" + Base64( nonce[12] || ciphertext[N] || tag[16] )
///   - When no master key is configured (dev/test), Encrypt/Decrypt act as pass-through
///     so the application remains functional without mandatory encryption setup.
///
/// Key rotation strategy:
///   - Set FIELD_ENCRYPTION_KEY_PREVIOUS to the old key when rotating.
///   - Decrypt falls back to the previous key if the current key fails (re-encrypt on next write).
///
/// OWASP compliance:
///   - AES-256-GCM (NIST-approved AEAD cipher)
///   - Keys never logged; master key sourced from environment only
///   - Per-tenant isolation via HKDF
/// </summary>
public sealed class AesFieldEncryptionService : IEncryptionService
{
    private const string EncPrefix = "ENC:";
    private const int NonceSize = 12;   // AES-GCM standard nonce
    private const int TagSize = 16;     // AES-GCM standard tag
    private const int KeySize = 32;     // 256 bits

    private readonly byte[]? _masterKey;
    private readonly byte[]? _previousKey;
    private readonly ILogger<AesFieldEncryptionService> _logger;

    public bool IsEnabled => _masterKey is not null;

    public AesFieldEncryptionService(IConfiguration configuration, ILogger<AesFieldEncryptionService> logger)
    {
        _logger = logger;

        var keyBase64 = configuration["FIELD_ENCRYPTION_KEY"]
                        ?? Environment.GetEnvironmentVariable("FIELD_ENCRYPTION_KEY");

        if (!string.IsNullOrWhiteSpace(keyBase64))
        {
            try
            {
                _masterKey = Convert.FromBase64String(keyBase64);
                if (_masterKey.Length < KeySize)
                {
                    _logger.LogWarning("FIELD_ENCRYPTION_KEY is shorter than 256 bits; deriving full key via HKDF");
                    _masterKey = DeriveKeyFromShort(_masterKey);
                }
            }
            catch (FormatException)
            {
                // Allow raw UTF-8 string keys (minimum 32 bytes)
                var rawBytes = Encoding.UTF8.GetBytes(keyBase64);
                _masterKey = rawBytes.Length >= KeySize ? rawBytes : DeriveKeyFromShort(rawBytes);
            }
        }

        // Optional previous key for rotation
        var prevBase64 = configuration["FIELD_ENCRYPTION_KEY_PREVIOUS"]
                         ?? Environment.GetEnvironmentVariable("FIELD_ENCRYPTION_KEY_PREVIOUS");
        if (!string.IsNullOrWhiteSpace(prevBase64))
        {
            try { _previousKey = NormalizeKey(prevBase64); }
            catch { _previousKey = null; }
        }

        if (_masterKey is not null)
            _logger.LogInformation("Field-level encryption is ENABLED (per-tenant AES-256-GCM)");
        else
            _logger.LogWarning("Field-level encryption is DISABLED — FIELD_ENCRYPTION_KEY not configured");
    }

    public string Encrypt(string plaintext, Guid tenantId)
    {
        if (_masterKey is null || string.IsNullOrEmpty(plaintext))
            return plaintext;

        var tenantKey = DeriveTenantKey(_masterKey, tenantId);
        try
        {
            return EncryptWithKey(plaintext, tenantKey);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(tenantKey);
        }
    }

    public string Decrypt(string ciphertext, Guid tenantId)
    {
        if (_masterKey is null || string.IsNullOrEmpty(ciphertext) || !ciphertext.StartsWith(EncPrefix))
            return ciphertext;

        var tenantKey = DeriveTenantKey(_masterKey, tenantId);
        try
        {
            return DecryptWithKey(ciphertext, tenantKey);
        }
        catch (CryptographicException) when (_previousKey is not null)
        {
            // Fallback to previous key for rotation support
            var prevTenantKey = DeriveTenantKey(_previousKey, tenantId);
            try
            {
                return DecryptWithKey(ciphertext, prevTenantKey);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(prevTenantKey);
            }
        }
        catch (CryptographicException ex)
        {
            _logger.LogError(ex, "Failed to decrypt field for tenant {TenantId}; returning raw value", tenantId);
            return ciphertext;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(tenantKey);
        }
    }

    // ── Private helpers ──

    private static string EncryptWithKey(string plaintext, byte[] key)
    {
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var nonce = new byte[NonceSize];
        RandomNumberGenerator.Fill(nonce);

        var ciphertextBytes = new byte[plaintextBytes.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plaintextBytes, ciphertextBytes, tag);

        // Format: nonce || ciphertext || tag
        var combined = new byte[NonceSize + ciphertextBytes.Length + TagSize];
        Buffer.BlockCopy(nonce, 0, combined, 0, NonceSize);
        Buffer.BlockCopy(ciphertextBytes, 0, combined, NonceSize, ciphertextBytes.Length);
        Buffer.BlockCopy(tag, 0, combined, NonceSize + ciphertextBytes.Length, TagSize);

        return EncPrefix + Convert.ToBase64String(combined);
    }

    private static string DecryptWithKey(string ciphertext, byte[] key)
    {
        var payload = Convert.FromBase64String(ciphertext[EncPrefix.Length..]);
        if (payload.Length < NonceSize + TagSize)
            throw new CryptographicException("Encrypted payload too short.");

        var nonce = payload.AsSpan(0, NonceSize);
        var ciphertextLength = payload.Length - NonceSize - TagSize;
        var encrypted = payload.AsSpan(NonceSize, ciphertextLength);
        var tag = payload.AsSpan(NonceSize + ciphertextLength, TagSize);

        var plaintextBytes = new byte[ciphertextLength];

        using var aes = new AesGcm(key, TagSize);
        aes.Decrypt(nonce, encrypted, tag, plaintextBytes);

        return Encoding.UTF8.GetString(plaintextBytes);
    }

    /// <summary>
    /// Derives a unique 256-bit subkey per tenant using HKDF-SHA256.
    /// This ensures multi-tenant cryptographic isolation.
    /// </summary>
    private static byte[] DeriveTenantKey(byte[] masterKey, Guid tenantId)
    {
        var info = Encoding.UTF8.GetBytes($"field-encryption-tenant-{tenantId}");
        return HKDF.DeriveKey(HashAlgorithmName.SHA256, masterKey, KeySize, info: info);
    }

    private static byte[] DeriveKeyFromShort(byte[] shortKey)
    {
        var info = Encoding.UTF8.GetBytes("field-encryption-key-expand");
        return HKDF.DeriveKey(HashAlgorithmName.SHA256, shortKey, KeySize, info: info);
    }

    private static byte[]? NormalizeKey(string keyValue)
    {
        try
        {
            var bytes = Convert.FromBase64String(keyValue);
            return bytes.Length >= KeySize ? bytes : DeriveKeyFromShort(bytes);
        }
        catch (FormatException)
        {
            var rawBytes = Encoding.UTF8.GetBytes(keyValue);
            return rawBytes.Length >= KeySize ? rawBytes : DeriveKeyFromShort(rawBytes);
        }
    }
}
