using Application.Common.Interfaces;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Assets.Queries
{
    public record GetAssetsByProjectQuery(
        Guid ProjectId,
        AssetStatus? Status = null,
        AssetType? Type = null,
        string? Search = null,
        int Page = 1,
        int PageSize = 50) : IRequest<AssetsByProjectResult>;

    public record AssetListItemDto(
        Guid Id,
        string AssetTag,
        string Name,
        AssetType Type,
        AssetStatus Status,
        string Location,
        Guid? AssignedToUserId,
        string? AssignedToUserName,
        DateTime PurchaseDate,
        decimal CurrentValue,
        string? Manufacturer,
        string? Model);

    public record AssetsByProjectResult(
        List<AssetListItemDto> Items,
        int TotalCount,
        int Page,
        int PageSize);

    public class GetAssetsByProjectHandler : IRequestHandler<GetAssetsByProjectQuery, AssetsByProjectResult>
    {
        private readonly IAppDbContext _db;

        public GetAssetsByProjectHandler(IAppDbContext db) => _db = db;

        public async Task<AssetsByProjectResult> Handle(GetAssetsByProjectQuery request, CancellationToken cancellationToken)
        {
            var query = _db.Assets
                .AsNoTracking()
                .Include(a => a.AssignedToUser)
                .Where(a => a.ProjectId == request.ProjectId && a.IsActive);

            if (request.Status.HasValue)
                query = query.Where(a => a.Status == request.Status.Value);

            if (request.Type.HasValue)
                query = query.Where(a => a.Type == request.Type.Value);

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var search = request.Search.ToLower();
                query = query.Where(a =>
                    a.Name.ToLower().Contains(search) ||
                    a.AssetTag.ToLower().Contains(search) ||
                    (a.SerialNumber != null && a.SerialNumber.ToLower().Contains(search)) ||
                    (a.Manufacturer != null && a.Manufacturer.ToLower().Contains(search)));
            }

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
