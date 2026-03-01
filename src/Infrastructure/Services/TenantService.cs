using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace Infrastructure.Services
{
    public class TenantService : ITenantService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IServiceProvider _serviceProvider;
        private Guid? _explicitTenantId;

        // Feature-to-minimum-tier mapping
        private static readonly Dictionary<string, TenantTier> FeatureTierMap = new(StringComparer.OrdinalIgnoreCase)
        {
            ["custom-workflows"] = TenantTier.Business,
            ["advanced-reporting"] = TenantTier.Business,
            ["api-access"] = TenantTier.Business,
            ["sso"] = TenantTier.Enterprise,
            ["audit-logs"] = TenantTier.Business,
            ["custom-fields"] = TenantTier.Business,
            ["unlimited-projects"] = TenantTier.Business,
            ["unlimited-users"] = TenantTier.Enterprise
        };

        public TenantService(IHttpContextAccessor httpContextAccessor, IServiceProvider serviceProvider)
        {
            _httpContextAccessor = httpContextAccessor;
            _serviceProvider = serviceProvider;
        }

        public Guid GetCurrentTenantId()
        {
            // 1. Explicit override (background jobs, system operations)
            if (_explicitTenantId.HasValue)
                return _explicitTenantId.Value;

            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext is null)
                throw new InvalidTenantContextException("No HTTP context available. Use SetTenantId() for background operations.");

            // 2. Tenant resolved by middleware (stored in HttpContext.Items)
            if (httpContext.Items.TryGetValue("TenantId", out var tenantIdObj) && tenantIdObj is Guid tenantId)
                return tenantId;

            // 3. JWT claim fallback
            var tenantClaim = httpContext.User.FindFirst("tenant_id")?.Value;
            if (!string.IsNullOrEmpty(tenantClaim) && Guid.TryParse(tenantClaim, out var claimTenantId))
                return claimTenantId;

            throw new InvalidTenantContextException();
        }

        public async Task<Tenant> GetCurrentTenantAsync(CancellationToken cancellationToken = default)
        {
            var tenantId = GetCurrentTenantId();
            var db = GetDbContext();

            var tenant = await db.Set<Tenant>()
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

            if (tenant is null || !tenant.IsActive)
                throw new TenantNotFoundException(tenantId);

            return tenant;
        }

        public async Task<Tenant?> GetTenantBySubdomainAsync(string subdomain, CancellationToken cancellationToken = default)
        {
            var db = GetDbContext();
            return await db.Set<Tenant>()
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Subdomain == subdomain.ToLowerInvariant() && t.IsActive, cancellationToken);
        }

        public async Task<bool> HasFeatureAsync(string featureName, CancellationToken cancellationToken = default)
        {
            if (!FeatureTierMap.TryGetValue(featureName, out var requiredTier))
                return true; // Unknown features are allowed by default

            var tenant = await GetCurrentTenantAsync(cancellationToken);
            return tenant.Tier >= requiredTier;
        }

        public async Task<bool> IsWithinLimitAsync(string resource, int currentCount, CancellationToken cancellationToken = default)
        {
            var tenant = await GetCurrentTenantAsync(cancellationToken);

            var limit = resource.ToLowerInvariant() switch
            {
                "users" => tenant.MaxUsers,
                "projects" => tenant.MaxProjects,
                "storage" => (int)(tenant.MaxStorageBytes / (1024 * 1024)), // compare in MB
                _ => -1
            };

            // -1 means unlimited
            if (limit < 0)
                return true;

            return currentCount < limit;
        }

        public void SetTenantId(Guid tenantId)
        {
            _explicitTenantId = tenantId;
        }

        public bool IsSystemAdminContext()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext is null)
                return false;

            return httpContext.User.HasClaim("system_role", "SystemAdmin")
                || httpContext.User.IsInRole("Admin"); // legacy Admin role maps to system-level access
        }

        public async Task<List<Tenant>> GetAllTenantsAsync(CancellationToken cancellationToken = default)
        {
            if (!IsSystemAdminContext())
                throw new UnauthorizedTenantAccessException("Only SystemAdmin users can list all tenants.");

            var db = GetDbContext();
            return await db.Set<Tenant>()
                .AsNoTracking()
                .OrderBy(t => t.Name)
                .ToListAsync(cancellationToken);
        }

        private DbContext GetDbContext()
        {
            // Resolve DbContext from service provider to avoid circular dependency
            // (AppDbContext depends on ITenantService, ITenantService queries Tenant table).
            // Note: The scope is NOT disposed here — it lives for the duration of the
            // service provider's own scope (request-scoped in ASP.NET Core).
            var scope = _serviceProvider.CreateScope();
            return scope.ServiceProvider.GetRequiredService<Infrastructure.Persistence.AppDbContext>();
        }
    }
}
