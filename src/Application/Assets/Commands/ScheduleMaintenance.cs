using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Application.Assets.Commands
{
    public record ScheduleMaintenanceCommand(
        Guid AssetId,
        MaintenanceType MaintenanceType,
        DateTime ScheduledDate,
        string Description,
        decimal EstimatedCost,
        string? Notes) : IRequest<ScheduleMaintenanceResult>;

    public record ScheduleMaintenanceResult(Guid MaintenanceRecordId);

    public class ScheduleMaintenanceHandler : IRequestHandler<ScheduleMaintenanceCommand, ScheduleMaintenanceResult>
    {
        private readonly IAppDbContext _db;
        private readonly IHttpContextAccessor _http;

        public ScheduleMaintenanceHandler(IAppDbContext db, IHttpContextAccessor http)
        {
            _db = db;
            _http = http;
        }

        public async Task<ScheduleMaintenanceResult> Handle(ScheduleMaintenanceCommand request, CancellationToken cancellationToken)
        {
            var asset = await _db.Assets
                .FirstOrDefaultAsync(a => a.Id == request.AssetId && a.IsActive, cancellationToken)
                ?? throw new KeyNotFoundException("Asset not found.");

            var userIdClaim = _http.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var createdBy = userIdClaim ?? string.Empty;

            var record = new MaintenanceRecord
            {
                Id = Guid.NewGuid(),
                AssetId = request.AssetId,
                MaintenanceType = request.MaintenanceType,
                ScheduledDate = request.ScheduledDate,
                Description = request.Description,
                Cost = request.EstimatedCost,
                Notes = request.Notes,
                CreatedBy = createdBy
            };

            _db.MaintenanceRecords.Add(record);
            await _db.SaveChangesAsync(cancellationToken);
            return new ScheduleMaintenanceResult(record.Id);
        }
    }
}
