using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Application.Assets.Commands
{
    public record ReturnAssetCommand(
        Guid AssetId,
        string Condition,
        string? Notes) : IRequest<Unit>;

    public class ReturnAssetHandler : IRequestHandler<ReturnAssetCommand, Unit>
    {
        private readonly IAppDbContext _db;
        private readonly IHttpContextAccessor _http;

        public ReturnAssetHandler(IAppDbContext db, IHttpContextAccessor http)
        {
            _db = db;
            _http = http;
        }

        public async Task<Unit> Handle(ReturnAssetCommand request, CancellationToken cancellationToken)
        {
            var asset = await _db.Assets
                .FirstOrDefaultAsync(a => a.Id == request.AssetId && a.IsActive, cancellationToken)
                ?? throw new KeyNotFoundException("Asset not found.");

            var checkout = await _db.AssetCheckouts
                .Where(c => c.AssetId == request.AssetId && c.ActualReturnDate == null)
                .OrderByDescending(c => c.CheckedOutAt)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new InvalidOperationException("No active checkout found for this asset.");

            var userIdClaim = _http.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var checkedInBy = userIdClaim ?? string.Empty;

            checkout.ActualReturnDate = DateTime.UtcNow;
            checkout.CheckedInBy = checkedInBy;
            checkout.Condition = request.Condition;
            checkout.Notes = request.Notes;

            var previousAssignee = asset.AssignedToUserId;
            asset.AssignedToUserId = null;
            asset.Status = request.Condition == "Damaged" ? AssetStatus.Damaged : AssetStatus.Available;
            asset.UpdatedAt = DateTime.UtcNow;

            _db.AssetHistoryEntries.Add(new AssetHistoryEntry
            {
                Id = Guid.NewGuid(),
                AssetId = asset.Id,
                ChangeType = AssetChangeType.AssignmentChanged,
                OldValue = previousAssignee?.ToString(),
                NewValue = null,
                ChangedBy = checkedInBy,
                ChangedAt = DateTime.UtcNow,
                Reason = $"Returned in {request.Condition} condition"
            });

            await _db.SaveChangesAsync(cancellationToken);
            return Unit.Value;
        }
    }
}
