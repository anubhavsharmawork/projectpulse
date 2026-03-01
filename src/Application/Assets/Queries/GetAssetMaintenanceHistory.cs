using Application.Common.Interfaces;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Assets.Queries
{
    public record GetAssetMaintenanceHistoryQuery(Guid AssetId) : IRequest<List<MaintenanceRecordDto>>;

    public record MaintenanceRecordDto(
        Guid Id,
        Guid AssetId,
        DateTime ScheduledDate,
        DateTime? CompletedDate,
        MaintenanceType MaintenanceType,
        string Description,
        string? PerformedBy,
        decimal Cost,
        string? Notes,
        DateTime? NextMaintenanceDate);

    public class GetAssetMaintenanceHistoryHandler : IRequestHandler<GetAssetMaintenanceHistoryQuery, List<MaintenanceRecordDto>>
    {
        private readonly IAppDbContext _db;

        public GetAssetMaintenanceHistoryHandler(IAppDbContext db) => _db = db;

        public async Task<List<MaintenanceRecordDto>> Handle(GetAssetMaintenanceHistoryQuery request, CancellationToken cancellationToken)
        {
            return await _db.MaintenanceRecords
                .AsNoTracking()
                .Where(m => m.AssetId == request.AssetId)
                .OrderByDescending(m => m.ScheduledDate)
                .Select(m => new MaintenanceRecordDto(
                    m.Id,
                    m.AssetId,
                    m.ScheduledDate,
                    m.CompletedDate,
                    m.MaintenanceType,
                    m.Description,
                    m.PerformedBy,
                    m.Cost,
                    m.Notes,
                    m.NextMaintenanceDate))
                .ToListAsync(cancellationToken);
        }
    }
}
