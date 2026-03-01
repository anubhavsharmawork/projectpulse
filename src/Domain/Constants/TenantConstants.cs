namespace Domain.Constants
{
    public static class TenantConstants
    {
        /// <summary>
        /// Well-known default tenant ID used for the initial "Default Organization" created during migration.
        /// All pre-existing and demo-seeded data is assigned to this tenant for backward compatibility.
        /// </summary>
        public static readonly Guid DefaultTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    }
}
