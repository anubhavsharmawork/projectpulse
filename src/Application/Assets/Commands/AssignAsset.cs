using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Application.Assets.Commands
{
    public record AssignAssetCommand(
        Guid AssetId,
        Guid AssigneeUserId,
        DateTime? ExpectedReturnDate,
        string? Notes) : IRequest<AssignAssetResult>;

    public record AssignAssetResult(Guid CheckoutId);

    public class AssignAssetHandler : IRequestHandler<AssignAssetCommand, AssignAssetResult>
    {
        private readonly IAppDbContext _db;
        private readonly IHttpContextAccessor _http;

        public AssignAssetHandler(IAppDbContext db, IHttpContextAccessor http)
        {
            _db = db;
            _http = http;
        }

        public async Task<AssignAssetResult> Handle(AssignAssetCommand request, CancellationToken cancellationToken)
        {
            var asset = await _db.Assets
                .FirstOrDefaultAsync(a => a.Id == request.AssetId && a.IsActive, cancellationToken)
                ?? throw new KeyNotFoundException("Asset not found.");

            if (asset.Status == AssetStatus.InUse)
                throw new InvalidOperationException("Asset is already assigned.");

            var userIdClaim = _http.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var checkedOutBy = userIdClaim ?? string.Empty;

            var checkout = new AssetCheckout
            {
                Id = Guid.NewGuid(),
                AssetId = request.AssetId,
                CheckedOutToUserId = request.AssigneeUserId,
                CheckedOutAt = DateTime.UtcNow,
                ExpectedReturnDate = request.ExpectedReturnDate,
                CheckedOutBy = checkedOutBy,
                Condition = "Good",
                Notes = request.Notes
            };

            asset.AssignedToUserId = request.AssigneeUserId;
            asset.Status = AssetStatus.InUse;
            asset.UpdatedAt = DateTime.UtcNow;

            _db.AssetCheckouts.Add(checkout);

            _db.AssetHistoryEntries.Add(new AssetHistoryEntry
            {
                Id = Guid.NewGuid(),
                AssetId = asset.Id,
                ChangeType = AssetChangeType.AssignmentChanged,
                NewValue = request.AssigneeUserId.ToString(),
                ChangedBy = checkedOutBy,
                ChangedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync(cancellationToken);
            return new AssignAssetResult(checkout.Id);
        }
    }
}
