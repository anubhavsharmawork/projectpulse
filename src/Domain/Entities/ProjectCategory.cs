using Domain.Enums;

namespace Domain.Entities
{
    public class ProjectCategory : BaseEntity
    {
        public Guid TenantId { get; set; }
        public string Name { get; set; } = string.Empty;
        public DomainType DomainType { get; set; }
        public string? Description { get; set; }

        /// <summary>
        /// JSON array of default team role assignments for this category.
        /// Each entry: { "role": "...", "description": "..." }
        /// </summary>
        public string? DefaultTeamRoles { get; set; }
    }
}
