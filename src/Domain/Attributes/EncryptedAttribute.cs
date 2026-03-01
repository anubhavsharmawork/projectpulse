namespace Domain.Attributes;

/// <summary>
/// Marks a string property as containing sensitive data that must be encrypted at rest.
/// EF Core value converters in AppDbContext automatically encrypt properties with this attribute
/// on write and decrypt on read.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class EncryptedAttribute : Attribute
{
}
