using Application.Common.Interfaces;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Assets.Queries
{
    public record GetAssetHistoryQuery(Guid AssetId) : IRequest<List<AssetHistoryDto>>;

    public record AssetHistoryDto(
        Guid Id,
        Guid AssetId,
        AssetChangeType ChangeType,
        string? OldValue,
        string? NewValue,
        string ChangedBy,
        DateTime ChangedAt,
        string? Reason);

    public class GetAssetHistoryHandler : IRequestHandler<GetAssetHistoryQuery, List<AssetHistoryDto>>
    {
        private readonly IAppDbContext _db;

        public GetAssetHistoryHandler(IAppDbContext db) => _db = db;

        public async Task<List<AssetHistoryDto>> Handle(GetAssetHistoryQuery request, CancellationToken cancellationToken)
        {
            return await _db.AssetHistoryEntries
                .AsNoTracking()
                .Where(h => h.AssetId == request.AssetId)
                .OrderByDescending(h => h.ChangedAt)
                .Select(h => new AssetHistoryDto(
                    h.Id,
                    h.AssetId,
                    h.ChangeType,
                    h.OldValue,
                    h.NewValue,
                    h.ChangedBy,
                    h.ChangedAt,
                    h.Reason))
                .ToListAsync(cancellationToken);
        }
    }
}
