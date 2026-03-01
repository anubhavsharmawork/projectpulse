using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Application.Assets.Commands
{
    public record RecordMaintenanceCommand(
        Guid MaintenanceRecordId,
        DateTime CompletedDate,
        string? PerformedBy,
        decimal ActualCost,
        string? Notes) : IRequest<Unit>;

    public class RecordMaintenanceHandler : IRequestHandler<RecordMaintenanceCommand, Unit>
    {
        private readonly IAppDbContext _db;
        private readonly IHttpContextAccessor _http;

        public RecordMaintenanceHandler(IAppDbContext db, IHttpContextAccessor http)
        {
            _db = db;
            _http = http;
        }

        public async Task<Unit> Handle(RecordMaintenanceCommand request, CancellationToken cancellationToken)
        {
            var record = await _db.MaintenanceRecords
                .Include(m => m.Asset)
                .FirstOrDefaultAsync(m => m.Id == request.MaintenanceRecordId, cancellationToken)
                ?? throw new KeyNotFoundException("Maintenance record not found.");

            var userIdClaim = _http.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var changedBy = userIdClaim ?? string.Empty;

            record.CompletedDate = request.CompletedDate;
            record.PerformedBy = request.PerformedBy;
            record.Cost = request.ActualCost;
            record.Notes = request.Notes;
            record.UpdatedAt = DateTime.UtcNow;

            // Update asset maintenance tracking
            if (record.Asset.MaintenanceIntervalDays.HasValue)
            {
                record.Asset.LastMaintenanceDate = request.CompletedDate;
                record.Asset.NextMaintenanceDate = request.CompletedDate.AddDays(record.Asset.MaintenanceIntervalDays.Value);
            }

            _db.AssetHistoryEntries.Add(new AssetHistoryEntry
            {
                Id = Guid.NewGuid(),
                AssetId = record.AssetId,
                ChangeType = AssetChangeType.MaintenancePerformed,
                NewValue = record.MaintenanceType.ToString(),
                ChangedBy = changedBy,
                ChangedAt = DateTime.UtcNow,
                Reason = record.Description
            });

            await _db.SaveChangesAsync(cancellationToken);
            return Unit.Value;
        }
    }
}
