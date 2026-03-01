namespace Domain.Entities
{
    public class RolePermission : BaseEntity
    {
        public Guid AppRoleId { get; set; }
        public AppRole AppRole { get; set; } = null!;

        public Guid PermissionId { get; set; }
        public Permission Permission { get; set; } = null!;
    }
}
