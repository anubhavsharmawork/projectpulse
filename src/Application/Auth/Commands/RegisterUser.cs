using Application.Common.Interfaces;
using Application.Common.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Application.Auth.Commands
{
    public record RegisterUserCommand(string Email, string Password, string DisplayName) : IRequest<RegisterUserResult>;
    public record RegisterUserResult(Guid UserId);

    public class RegisterUserHandler : IRequestHandler<RegisterUserCommand, RegisterUserResult>
    {
        private readonly IAppDbContext _db;
        private readonly IConfiguration _config;
        public RegisterUserHandler(IAppDbContext db, IConfiguration config)
        {
            _db = db; _config = config;
        }

        public async Task<RegisterUserResult> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
        {
            if (await _db.Users.AnyAsync(u => u.Email == request.Email, cancellationToken))
                throw new InvalidOperationException("Email already registered");

            var salt = _config["DEMO_SALT"] ?? "demo-salt";
            var user = new Domain.Entities.User
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                DisplayName = request.DisplayName,
                PasswordHash = SimplePasswordHasher.Hash(request.Password, salt),
                Role = Domain.Entities.Role.Member
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync(cancellationToken);
            return new RegisterUserResult(user.Id);
        }
    }
}
