using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Users.Queries
{
    public record GetUsersQuery(string? SearchTerm = null) : IRequest<List<UserDto>>;
    
    public record UserDto(Guid Id, string Email, string DisplayName);

    public class GetUsersHandler : IRequestHandler<GetUsersQuery, List<UserDto>>
    {
        private readonly IAppDbContext _db;
        public GetUsersHandler(IAppDbContext db) => _db = db;

        public async Task<List<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
        {
            var query = _db.Users.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.ToLower();
                query = query.Where(u => 
                    u.DisplayName.ToLower().Contains(term) || 
                    u.Email.ToLower().Contains(term));
            }

            return await query
                .OrderBy(u => u.DisplayName)
                .Take(20)
                .Select(u => new UserDto(u.Id, u.Email, u.DisplayName))
                .ToListAsync(cancellationToken);
        }
    }
}
