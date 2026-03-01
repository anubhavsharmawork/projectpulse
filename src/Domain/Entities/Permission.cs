using Domain.Enums;

namespace Domain.Entities
{
    public class Permission : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public PermissionCategory Category { get; set; }
        public string? Description { get; set; }

        public List<RolePermission> RolePermissions { get; set; } = new();
    }
}
