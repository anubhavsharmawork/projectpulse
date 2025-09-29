using System.Security.Cryptography;
using System.Text;

namespace Application.Common.Security
{
    public static class SimplePasswordHasher
    {
        public static string Hash(string password, string saltKey)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes($"{password}|{saltKey}");
            var hash = sha.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        public static bool Verify(string password, string saltKey, string hash)
        {
            return Hash(password, saltKey) == hash;
        }
    }
}
