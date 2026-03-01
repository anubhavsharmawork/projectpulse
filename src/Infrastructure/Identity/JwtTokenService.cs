using Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Infrastructure
{
    public class JwtTokenService : IJwtTokenService
    {
        private readonly IConfiguration _config;
        private const string FallbackKey = "dev-secret-change-me-please-at-least-32-chars";
        public JwtTokenService(IConfiguration config)
        {
            _config = config;
        }

        public string GenerateToken(Guid userId, string email, string role)
        {
            return GenerateToken(userId, Guid.Empty, email, role, null, null);
        }

        public string GenerateToken(Guid userId, string email, string role, string? systemRole, IEnumerable<string>? permissions)
        {
            return GenerateToken(userId, Guid.Empty, email, role, systemRole, permissions, false);
        }

        public string GenerateToken(Guid userId, Guid tenantId, string email, string role, string? systemRole, IEnumerable<string>? permissions, bool isDemo = false)
        {
            var issuer = _config["JWT:Issuer"]; // may be null in demo
            var audience = _config["JWT:Audience"]; // may be null in demo
            var key = _config["JWT:Key"];
            if (string.IsNullOrWhiteSpace(key) || Encoding.UTF8.GetByteCount(key) < 32)
            {
                key = FallbackKey; // ensure non-empty, >= 256-bit
            }
            var creds = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>{
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(ClaimTypes.Role, role)
            };

            if (tenantId != Guid.Empty)
            {
                claims.Add(new Claim("tenant_id", tenantId.ToString()));
            }

            if (!string.IsNullOrWhiteSpace(systemRole))
            {
                claims.Add(new Claim("system_role", systemRole));
            }

            if (permissions is not null)
            {
                foreach (var perm in permissions)
                {
                    claims.Add(new Claim("permission", perm));
                }
            }

            if (isDemo)
            {
                claims.Add(new Claim("is_demo", "true"));
            }

            var token = new JwtSecurityToken(issuer, audience, claims, expires: DateTime.UtcNow.AddHours(8), signingCredentials: creds);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
