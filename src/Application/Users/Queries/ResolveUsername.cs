using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Users.Queries
{
    public record ResolveUsernameQuery(string Username) : IRequest<ResolvedUserDto?>;

    public record ResolvedUserDto(string DisplayName, string UserName);

    public class ResolveUsernameHandler : IRequestHandler<ResolveUsernameQuery, ResolvedUserDto?>
    {
        private readonly IAppDbContext _db;

        public ResolveUsernameHandler(IAppDbContext db) => _db = db;

        public async Task<ResolvedUserDto?> Handle(ResolveUsernameQuery request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Username))
                return null;

            var normalizedUsername = request.Username.Trim().ToLowerInvariant();

            var user = await _db.Users
                .AsNoTracking()
                .Where(u => u.UserName == normalizedUsername)
                .Select(u => new ResolvedUserDto(u.DisplayName, u.UserName))
                .FirstOrDefaultAsync(cancellationToken);

            return user;
        }
    }
}
