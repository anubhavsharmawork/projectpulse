using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Application.Assets.Commands
{
    public record DeleteAssetCommand(Guid AssetId) : IRequest<Unit>;

    public class DeleteAssetHandler : IRequestHandler<DeleteAssetCommand, Unit>
    {
        private readonly IAppDbContext _db;
        private readonly IHttpContextAccessor _http;

        public DeleteAssetHandler(IAppDbContext db, IHttpContextAccessor http)
        {
            _db = db;
            _http = http;
        }

        public async Task<Unit> Handle(DeleteAssetCommand request, CancellationToken cancellationToken)
        {
            var asset = await _db.Assets
                .FirstOrDefaultAsync(a => a.Id == request.AssetId && a.IsActive, cancellationToken)
                ?? throw new KeyNotFoundException("Asset not found.");

            var userIdClaim = _http.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var changedBy = userIdClaim ?? string.Empty;

            asset.IsActive = false;
            asset.UpdatedAt = DateTime.UtcNow;

            _db.AssetHistoryEntries.Add(new AssetHistoryEntry
            {
                Id = Guid.NewGuid(),
                AssetId = asset.Id,
                ChangeType = AssetChangeType.Disposed,
                OldValue = asset.Status.ToString(),
                NewValue = "Deleted",
                ChangedBy = changedBy,
                ChangedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync(cancellationToken);
            return Unit.Value;
        }
    }
}
