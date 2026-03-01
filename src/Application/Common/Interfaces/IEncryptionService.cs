namespace Application.Common.Interfaces;

/// <summary>
/// Provides field-level encryption and decryption for sensitive PII and financial data.
/// Implementations must use AES-256 or equivalent OWASP-recommended algorithms.
/// Keys are derived per-tenant to ensure multi-tenant isolation.
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Encrypts a plaintext value for a specific tenant.
    /// Returns the ciphertext as a Base64 string prefixed with "ENC:" marker.
    /// Returns the original value unchanged if encryption is not configured.
    /// </summary>
    string Encrypt(string plaintext, Guid tenantId);

    /// <summary>
    /// Decrypts a ciphertext value for a specific tenant.
    /// Expects input prefixed with "ENC:" marker from <see cref="Encrypt"/>.
    /// Returns the original value unchanged if it is not encrypted (no "ENC:" prefix).
    /// </summary>
    string Decrypt(string ciphertext, Guid tenantId);

    /// <summary>
    /// Returns true when a valid encryption key is configured and encryption is active.
    /// When false, <see cref="Encrypt"/> and <see cref="Decrypt"/> act as pass-through.
    /// </summary>
    bool IsEnabled { get; }
}
