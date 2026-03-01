using Application.Common.Interfaces;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.CustomFields.Queries
{
    public record GetCustomFieldsByDomainQuery(string DomainType, string? EntityType = null) : IRequest<List<CustomFieldDto>>;

    public record CustomFieldDto(
        Guid Id,
        string Name,
        string FieldType,
        string DomainType,
        bool IsRequired,
        string? Options,
        string? ValidationRule,
        string? EntityType);

    public class GetCustomFieldsByDomainHandler : IRequestHandler<GetCustomFieldsByDomainQuery, List<CustomFieldDto>>
    {
        private readonly IAppDbContext _db;

        public GetCustomFieldsByDomainHandler(IAppDbContext db)
        {
            _db = db;
        }

        public async Task<List<CustomFieldDto>> Handle(GetCustomFieldsByDomainQuery request, CancellationToken cancellationToken)
        {
            if (!Enum.TryParse<DomainType>(request.DomainType, ignoreCase: true, out var domainType))
                return new List<CustomFieldDto>();

            var query = _db.CustomFields
                .AsNoTracking()
                .Where(f => f.DomainType == domainType);

            // Filter by entity type: show fields matching the requested level OR fields with no level restriction (null)
            if (!string.IsNullOrWhiteSpace(request.EntityType))
            {
                var entityType = request.EntityType.Trim();
                query = query.Where(f => f.EntityType == null || f.EntityType == entityType);
            }

            var fields = await query
                .OrderBy(f => f.Name)
                .Select(f => new CustomFieldDto(
                    f.Id,
                    f.Name,
                    f.FieldType.ToString(),
                    f.DomainType.ToString(),
                    f.IsRequired,
                    f.Options,
                    f.ValidationRule,
                    f.EntityType))
                .ToListAsync(cancellationToken);

            return fields;
        }
    }
}
