using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Assets.Queries
{
    public record GetAssetCheckoutHistoryQuery(Guid AssetId) : IRequest<List<AssetCheckoutDto>>;

    public record AssetCheckoutDto(
        Guid Id,
        Guid AssetId,
        Guid CheckedOutToUserId,
        string? CheckedOutToUserName,
        DateTime CheckedOutAt,
        DateTime? ExpectedReturnDate,
        DateTime? ActualReturnDate,
        string CheckedOutBy,
        string? CheckedInBy,
        string Condition,
        string? Notes);

    public class GetAssetCheckoutHistoryHandler : IRequestHandler<GetAssetCheckoutHistoryQuery, List<AssetCheckoutDto>>
    {
        private readonly IAppDbContext _db;

        public GetAssetCheckoutHistoryHandler(IAppDbContext db) => _db = db;

        public async Task<List<AssetCheckoutDto>> Handle(GetAssetCheckoutHistoryQuery request, CancellationToken cancellationToken)
        {
            return await _db.AssetCheckouts
                .AsNoTracking()
                .Include(c => c.CheckedOutToUser)
                .Where(c => c.AssetId == request.AssetId)
                .OrderByDescending(c => c.CheckedOutAt)
                .Select(c => new AssetCheckoutDto(
                    c.Id,
                    c.AssetId,
                    c.CheckedOutToUserId,
                    c.CheckedOutToUser != null ? c.CheckedOutToUser.DisplayName : null,
                    c.CheckedOutAt,
                    c.ExpectedReturnDate,
                    c.ActualReturnDate,
                    c.CheckedOutBy,
                    c.CheckedInBy,
                    c.Condition,
                    c.Notes))
                .ToListAsync(cancellationToken);
        }
    }
}
