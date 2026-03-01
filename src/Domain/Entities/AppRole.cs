using Domain.Enums;

namespace Domain.Entities
{
    public class AppRole : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public SystemRole SystemRole { get; set; }
        public string? Description { get; set; }

        public List<RolePermission> RolePermissions { get; set; } = new();
    }
}
