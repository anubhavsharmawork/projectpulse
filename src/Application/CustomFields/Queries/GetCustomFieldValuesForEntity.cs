using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.CustomFields.Queries
{
    public record GetCustomFieldValuesForEntityQuery(Guid EntityId) : IRequest<List<CustomFieldValueDto>>;

    public record CustomFieldValueDto(
        Guid Id,
        Guid CustomFieldId,
        string FieldName,
        string FieldType,
        Guid EntityId,
        string? Value,
        bool IsRequired,
        string? Options);

    public class GetCustomFieldValuesForEntityHandler : IRequestHandler<GetCustomFieldValuesForEntityQuery, List<CustomFieldValueDto>>
    {
        private readonly IAppDbContext _db;

        public GetCustomFieldValuesForEntityHandler(IAppDbContext db)
        {
            _db = db;
        }

        public async Task<List<CustomFieldValueDto>> Handle(GetCustomFieldValuesForEntityQuery request, CancellationToken cancellationToken)
        {
            var values = await _db.CustomFieldValues
                .AsNoTracking()
                .Where(v => v.EntityId == request.EntityId)
                .Include(v => v.CustomField)
                .Select(v => new CustomFieldValueDto(
                    v.Id,
                    v.CustomFieldId,
                    v.CustomField.Name,
                    v.CustomField.FieldType.ToString(),
                    v.EntityId,
                    v.Value,
                    v.CustomField.IsRequired,
                    v.CustomField.Options))
                .ToListAsync(cancellationToken);

            return values;
        }
    }
}
