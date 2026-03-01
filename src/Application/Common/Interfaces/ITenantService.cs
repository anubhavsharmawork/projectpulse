using Domain.Entities;

namespace Application.Common.Interfaces
{
    public interface ITenantService
    {
        Guid GetCurrentTenantId();
        Task<Tenant> GetCurrentTenantAsync(CancellationToken cancellationToken = default);
        Task<Tenant?> GetTenantBySubdomainAsync(string subdomain, CancellationToken cancellationToken = default);
        Task<bool> HasFeatureAsync(string featureName, CancellationToken cancellationToken = default);
        Task<bool> IsWithinLimitAsync(string resource, int currentCount, CancellationToken cancellationToken = default);
        void SetTenantId(Guid tenantId);
        bool IsSystemAdminContext();
        Task<List<Tenant>> GetAllTenantsAsync(CancellationToken cancellationToken = default);
    }
}
