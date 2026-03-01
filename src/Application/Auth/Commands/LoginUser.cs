using Application.Common.Interfaces;
using Application.Common.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Application.Auth.Commands
{
    public record LoginUserCommand(string Email, string Password) : IRequest<LoginUserResult>;
    public record LoginUserResult(string Token);

    public class LoginUserHandler : IRequestHandler<LoginUserCommand, LoginUserResult>
    {
        private readonly IAppDbContext _db;
        private readonly IJwtTokenService _jwt;
        private readonly IConfiguration _config;
        public LoginUserHandler(IAppDbContext db, IJwtTokenService jwt, IConfiguration config)
        {
            _db = db; _jwt = jwt; _config = config;
        }

        public async Task<LoginUserResult> Handle(LoginUserCommand request, CancellationToken cancellationToken)
        {
            // Support login by email OR username — the Email field accepts either.
            var identifier = request.Email?.Trim();

            // Authentication must bypass tenant query filters.
            // The user's tenant is determined AFTER credential verification, not before.
            var user = await _db.Users
                .IgnoreQueryFilters()
                .Include(u => u.AppRole)
                    .ThenInclude(r => r!.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
                .SingleOrDefaultAsync(
                    u => u.Email == identifier || u.UserName == identifier,
                    cancellationToken);
            if (user == null) throw new UnauthorizedAccessException("Invalid credentials");

            // Admin users are hashed with ADMIN_SALT; other users use DEMO_SALT.
            // Try the appropriate salt based on the user's Role enum.
            var verified = false;
            if (user.Role == Domain.Entities.Role.Admin)
            {
                var adminSalt = _config["ADMIN_SALT"];
                if (!string.IsNullOrWhiteSpace(adminSalt))
                    verified = SimplePasswordHasher.Verify(request.Password, adminSalt, user.PasswordHash);
            }

            if (!verified)
            {
                // Fall back to demo salt (covers demo users and legacy admin@demo.local)
                var demoSalt = _config["DEMO_SALT"] ?? "demo-salt";
                verified = SimplePasswordHasher.Verify(request.Password, demoSalt, user.PasswordHash);
            }

            if (!verified)
                throw new UnauthorizedAccessException("Invalid credentials");

            string? systemRole = user.AppRole?.SystemRole.ToString();
            var permissions = user.AppRole?.RolePermissions
                .Where(rp => rp.Permission != null)
                .Select(rp => rp.Permission.Name)
                .ToList();

            // Detect demo users by email pattern — demo users get read-only access in system admin
            var isDemo = user.Email.EndsWith("@demo.local", StringComparison.OrdinalIgnoreCase);

            var token = _jwt.GenerateToken(user.Id, user.TenantId, user.Email, user.Role.ToString(), systemRole, permissions, isDemo);
            return new LoginUserResult(token);
        }
    }
}
