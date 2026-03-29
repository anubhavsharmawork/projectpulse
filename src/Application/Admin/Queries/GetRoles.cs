using System.Diagnostics.CodeAnalysis;
using Application.Common.Interfaces;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Admin.Queries
{
    // ── Query ──
    public record GetRolesQuery : IRequest<List<RoleDto>>;

    // ── DTOs ──

    [ExcludeFromCodeCoverage]
    public record RoleDto(
        string Name,
        string SystemRole,
        string? Description,
        List<PermissionCategoryDto> PermissionCategories);

    [ExcludeFromCodeCoverage]
    public record PermissionCategoryDto(
        string Category,
        List<PermissionItemDto> Permissions);

    [ExcludeFromCodeCoverage]
    public record PermissionItemDto(
        string Name,
        string? Description,
        bool Granted);

    // ── Handler ──
    /// <summary>
    /// Reads all roles from the database (seeded from RolePermissions.json)
    /// and returns them with permissions grouped by category.
    /// All known permission names are included per role so the UI can show granted/denied.
    /// </summary>
    public class GetRolesHandler : IRequestHandler<GetRolesQuery, List<RoleDto>>
    {
        private readonly IAppDbContext _db;

        public GetRolesHandler(IAppDbContext db) => _db = db;

        public async Task<List<RoleDto>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
        {
            // Load all permissions so we can show granted vs denied per role
            var allPermissions = await _db.Permissions
                .AsNoTracking()
                .OrderBy(p => p.Category)
                .ThenBy(p => p.Name)
                .ToListAsync(cancellationToken);

            // Load all roles with their permission links
            var roles = await _db.AppRoles
                .AsNoTracking()
                .Include(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
                .OrderBy(r => r.SystemRole)
                .ToListAsync(cancellationToken);

            // Group all permissions by category once
            var permissionsByCategory = allPermissions
                .GroupBy(p => p.Category.ToString())
                .OrderBy(g => g.Key)
                .ToList();

            var result = new List<RoleDto>();

            foreach (var role in roles)
            {
                var grantedNames = new HashSet<string>(
                    role.RolePermissions
                        .Where(rp => rp.Permission != null)
                        .Select(rp => rp.Permission.Name));

                var categories = permissionsByCategory.Select(g => new PermissionCategoryDto(
                    g.Key,
                    g.Select(p => new PermissionItemDto(
                        p.Name,
                        p.Description,
                        grantedNames.Contains(p.Name)
                    )).ToList()
                )).ToList();

                result.Add(new RoleDto(
                    role.Name,
                    role.SystemRole.ToString(),
                    role.Description,
                    categories));
            }

            return result;
        }
    }
}
