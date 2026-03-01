using System.Security.Cryptography;
using Application.Common.Security;

/// <summary>
/// Offline utility to generate a BCrypt password hash for the admin account.
/// The resulting hash and salt should be stored in environment variables:
///   ADMIN_PASSWORD_HASH  →  the BCrypt hash output
///   ADMIN_SALT           →  the pepper/salt value used
///
/// Usage (from solution root):
///   dotnet run --project tools/PasswordHashGenerator -- "YourPasswordHere"
///
/// Example with explicit salt environment variable:
///   $env:ADMIN_SALT = "your-secret-pepper"           # PowerShell
///   export ADMIN_SALT="your-secret-pepper"            # Bash
///   dotnet run --project tools/PasswordHashGenerator -- "YourPasswordHere"
///
/// If ADMIN_SALT is not set, a secure random salt is generated automatically.
/// Never commit the output values to source control.
/// </summary>
if (args.Length == 0 || string.IsNullOrWhiteSpace(args[0]))
{
    Console.Error.WriteLine("Usage: PasswordHashGenerator <plaintext-password>");
    Console.Error.WriteLine("  Reads ADMIN_SALT from environment (generates random if not set).");
    return 1;
}

var password = args[0];
var salt = Environment.GetEnvironmentVariable("ADMIN_SALT");

if (string.IsNullOrWhiteSpace(salt))
{
    // Generate a cryptographically secure random salt (32 bytes, base64-encoded)
    var saltBytes = new byte[32];
    using var rng = RandomNumberGenerator.Create();
    rng.GetBytes(saltBytes);
    salt = Convert.ToBase64String(saltBytes);
    Console.WriteLine("[INFO] No ADMIN_SALT environment variable found — generated a new random salt.");
}
else
{
    Console.WriteLine("[INFO] Using ADMIN_SALT from environment variable.");
}

var hash = SimplePasswordHasher.Hash(password, salt);

Console.WriteLine();
Console.WriteLine("──────────────────────────────────────────────────");
Console.WriteLine($"ADMIN_SALT={salt}");
Console.WriteLine($"ADMIN_PASSWORD_HASH={hash}");
Console.WriteLine("──────────────────────────────────────────────────");
Console.WriteLine();
Console.WriteLine("Set ADMIN_PASSWORD_HASH to this hash and ADMIN_SALT to this salt");
Console.WriteLine("in your deployment environment variables (never commit these values).");

return 0;
