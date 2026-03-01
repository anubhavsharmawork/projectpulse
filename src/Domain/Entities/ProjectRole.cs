using Domain.Enums;

namespace Domain.Entities
{
    /// <summary>
    /// A role scoped to a specific project (and optionally domain type).
    /// Used to populate the Role dropdown in Team Management.
    /// </summary>
    public class ProjectRole
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid ProjectId { get; set; }
        public Project Project { get; set; } = null!;
        public string RoleName { get; set; } = string.Empty;
        public DomainType? DomainType { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
