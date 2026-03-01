using Application.Common.Interfaces;
using Application.Common.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Application.Auth.Commands
{
    public record RegisterUserCommand(string Email, string Password, string DisplayName, string? UserName = null) : IRequest<RegisterUserResult>;
    public record RegisterUserResult(Guid UserId, string UserName);

    public class RegisterUserHandler : IRequestHandler<RegisterUserCommand, RegisterUserResult>
    {
        private readonly IAppDbContext _db;
        private readonly IConfiguration _config;
        private readonly ITenantService _tenantService;
        public RegisterUserHandler(IAppDbContext db, IConfiguration config, ITenantService tenantService)
        {
            _db = db; _config = config; _tenantService = tenantService;
        }

        public async Task<RegisterUserResult> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
        {
            // Registration: bypass tenant filter for email uniqueness check (email must be globally unique)
            if (await _db.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == request.Email, cancellationToken))
                throw new InvalidOperationException("Email already registered");

            // Derive username: use provided value, or default to part before @
            var baseUserName = !string.IsNullOrWhiteSpace(request.UserName)
                ? request.UserName.Trim().ToLowerInvariant()
                : request.Email.Split('@')[0].ToLowerInvariant();

            // Enforce uniqueness: append numeric suffix if needed (globally unique)
            var userName = baseUserName;
            var suffix = 1;
            while (await _db.Users.IgnoreQueryFilters().AnyAsync(u => u.UserName == userName, cancellationToken))
            {
                userName = $"{baseUserName}{suffix}";
                suffix++;
            }

            // Resolve tenant: use middleware-provided context, fall back to default tenant
            Guid tenantId;
            try { tenantId = _tenantService.GetCurrentTenantId(); }
            catch { tenantId = Guid.Parse("00000000-0000-0000-0000-000000000001"); }

            var salt = _config["DEMO_SALT"] ?? "demo-salt";
            var user = new Domain.Entities.User
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Email = request.Email,
                DisplayName = request.DisplayName,
                UserName = userName,
                PasswordHash = SimplePasswordHasher.Hash(request.Password, salt),
                Role = Domain.Entities.Role.Member
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync(cancellationToken);
            return new RegisterUserResult(user.Id, user.UserName);
        }
    }
}
