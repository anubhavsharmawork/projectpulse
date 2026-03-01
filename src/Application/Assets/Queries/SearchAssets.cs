using Application.Common.Interfaces;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Assets.Queries
{
    public record SearchAssetsQuery(
        string SearchTerm,
        int Page = 1,
        int PageSize = 50) : IRequest<AssetsByProjectResult>;

    public class SearchAssetsHandler : IRequestHandler<SearchAssetsQuery, AssetsByProjectResult>
    {
        private readonly IAppDbContext _db;

        public SearchAssetsHandler(IAppDbContext db) => _db = db;

        public async Task<AssetsByProjectResult> Handle(SearchAssetsQuery request, CancellationToken cancellationToken)
        {
            var search = request.SearchTerm.ToLower();

            var query = _db.Assets
                .AsNoTracking()
                .Include(a => a.AssignedToUser)
                .Where(a => a.IsActive &&
                    (a.Name.ToLower().Contains(search) ||
                     a.AssetTag.ToLower().Contains(search) ||
                     (a.SerialNumber != null && a.SerialNumber.ToLower().Contains(search)) ||
                     (a.Manufacturer != null && a.Manufacturer.ToLower().Contains(search))));

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderBy(a => a.Name)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(a => new AssetListItemDto(
                    a.Id,
                    a.AssetTag,
                    a.Name,
                    a.Type,
                    a.Status,
                    a.Location,
                    a.AssignedToUserId,
                    a.AssignedToUser != null ? a.AssignedToUser.DisplayName : null,
                    a.PurchaseDate,
                    a.CurrentValue,
                    a.Manufacturer,
                    a.Model))
                .ToListAsync(cancellationToken);

            return new AssetsByProjectResult(items, totalCount, request.Page, request.PageSize);
        }
    }
}
