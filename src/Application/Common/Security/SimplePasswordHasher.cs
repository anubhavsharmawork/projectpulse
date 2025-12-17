using System.Security.Cryptography;
using System.Text;

namespace Application.Common.Security
{
    /// <summary>
    /// OWASP A02 compliant password hasher using BCrypt.
    /// BCrypt provides adaptive hashing with built-in salting, making it resistant to:
    /// - Rainbow table attacks (per-password salt)
    /// - Brute force attacks (configurable work factor)
    /// - GPU/ASIC acceleration (memory-hard algorithm)
    /// </summary>
    public static class SimplePasswordHasher
    {
        // Work factor of 12 = ~250ms per hash (good balance of security vs performance)
        private const int WorkFactor = 12;

        public static string Hash(string password, string saltKey)
        {
            // BCrypt generates its own cryptographic salt internally.
            // The saltKey is used as pepper for defense-in-depth.
            var peppered = $"{password}|{saltKey}";
            return BCrypt.Net.BCrypt.HashPassword(peppered, WorkFactor);
        }

        public static bool Verify(string password, string saltKey, string hash)
        {
            var peppered = $"{password}|{saltKey}";
            try
            {
                return BCrypt.Net.BCrypt.Verify(peppered, hash);
            }
            catch
            {
                // Handle legacy SHA256 hashes during migration period
                return VerifyLegacy(password, saltKey, hash);
            }
        }

        /// <summary>
        /// Legacy SHA256 verification for backward compatibility with existing users.
        /// This allows existing users to log in and have their passwords upgraded to BCrypt.
        /// </summary>
        private static bool VerifyLegacy(string password, string saltKey, string hash)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes($"{password}|{saltKey}");
            var computed = sha.ComputeHash(bytes);
            return Convert.ToBase64String(computed) == hash;
        }
    }
}
