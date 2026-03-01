namespace Domain.Entities
{
    public class Team : BaseEntity
    {
        public Guid TenantId { get; set; }
        public string Name { get; set; } = string.Empty;
        public Guid ProjectId { get; set; }
        public Project Project { get; set; } = null!;
        public List<TeamMember> Members { get; set; } = new();
    }
}
