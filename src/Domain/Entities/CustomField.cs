using Domain.Enums;

namespace Domain.Entities
{
    public class CustomField : BaseEntity
    {
        public Guid TenantId { get; set; }
        public string Name { get; set; } = string.Empty;
        public FieldType FieldType { get; set; }
        public DomainType DomainType { get; set; }
        public bool IsRequired { get; set; }

        /// <summary>
        /// Work item hierarchy level this field applies to (e.g., "1" for Epic/Program, "2" for Story/Work Package, "3" for Task).
        /// Null means the field applies to all levels.
        /// </summary>
        public string? EntityType { get; set; }

        /// <summary>
        /// JSON-serialized options for Dropdown/MultiSelect field types.
        /// </summary>
        public string? Options { get; set; }

        /// <summary>
        /// Optional validation rule expression (e.g., regex, range).
        /// </summary>
        public string? ValidationRule { get; set; }

        /// <summary>
        /// When true, the values for this field are considered sensitive PII/financial data
        /// and will be encrypted at rest. The <see cref="CustomFieldValue.Value"/> is encrypted
        /// on write and decrypted on read via EF Core value converters.
        /// </summary>
        public bool IsSensitive { get; set; }

        /// <summary>
        /// Optional link to the template that created this field definition.
        /// </summary>
        public Guid? DomainTemplateId { get; set; }
        public DomainTemplate? DomainTemplate { get; set; }

        public List<CustomFieldValue> Values { get; set; } = new();
    }
}
