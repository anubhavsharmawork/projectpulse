using Application.Common.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Application.CustomFields.Commands
{
    public record SaveCustomFieldValueCommand(
        Guid EntityId,
        Guid CustomFieldId,
        string Value) : IRequest<Guid>;

    public class SaveCustomFieldValueHandler : IRequestHandler<SaveCustomFieldValueCommand, Guid>
    {
        private readonly IAppDbContext _db;
        private readonly IHttpContextAccessor _http;

        public SaveCustomFieldValueHandler(IAppDbContext db, IHttpContextAccessor http)
        {
            _db = db;
            _http = http;
        }

        public async Task<Guid> Handle(SaveCustomFieldValueCommand request, CancellationToken cancellationToken)
        {
            var field = await _db.CustomFields
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == request.CustomFieldId, cancellationToken);
            if (field is null)
                throw new InvalidOperationException("Custom field not found");

            var createdBy = _http.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

            // Upsert: update existing value or create new one
            var existing = await _db.CustomFieldValues
                .FirstOrDefaultAsync(v => v.CustomFieldId == request.CustomFieldId && v.EntityId == request.EntityId, cancellationToken);

            if (existing is not null)
            {
                existing.Value = request.Value;
                return existing.Id;
            }

            var entry = new Domain.Entities.CustomFieldValue
            {
                Id = Guid.NewGuid(),
                CustomFieldId = request.CustomFieldId,
                EntityId = request.EntityId,
                EntityType = "WorkItem",
                Value = request.Value,
                CreatedBy = createdBy
            };

            _db.CustomFieldValues.Add(entry);
            await _db.SaveChangesAsync(cancellationToken);
            return entry.Id;
        }
    }
}
