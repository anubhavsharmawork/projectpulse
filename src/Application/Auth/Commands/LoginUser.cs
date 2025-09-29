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
            var user = await _db.Users.SingleOrDefaultAsync(u => u.Email == request.Email, cancellationToken);
            if (user == null) throw new UnauthorizedAccessException("Invalid credentials");

            var salt = _config["DEMO_SALT"] ?? "demo-salt";
            if (!SimplePasswordHasher.Verify(request.Password, salt, user.PasswordHash))
                throw new UnauthorizedAccessException("Invalid credentials");

            var token = _jwt.GenerateToken(user.Id, user.Email, user.Role.ToString());
            return new LoginUserResult(token);
        }
    }
}
