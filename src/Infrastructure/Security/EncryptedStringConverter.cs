using Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Infrastructure.Security;

/// <summary>
/// EF Core value converter that automatically encrypts string values on write
/// and decrypts on read, using the <see cref="IEncryptionService"/> with per-tenant key derivation.
///
/// The tenantId is resolved lazily via a delegate so the converter works correctly
/// with EF Core's singleton converter lifecycle while still respecting scoped tenant context.
/// </summary>
public sealed class EncryptedStringConverter : ValueConverter<string, string>
{
    public EncryptedStringConverter(IEncryptionService encryptionService, Func<Guid> tenantIdAccessor)
        : base(
            plaintext => encryptionService.Encrypt(plaintext ?? string.Empty, tenantIdAccessor()),
            ciphertext => encryptionService.Decrypt(ciphertext ?? string.Empty, tenantIdAccessor()))
    {
    }
}
