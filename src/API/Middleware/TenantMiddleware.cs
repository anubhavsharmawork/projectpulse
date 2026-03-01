using Application.Common.Interfaces;
using Domain.Constants;

namespace API.Middleware
{
    public class TenantMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<TenantMiddleware> _logger;

        /// <summary>
        /// Well-known default tenant ID — delegates to <see cref="TenantConstants.DefaultTenantId"/>.
        /// </summary>
        public static readonly Guid DefaultTenantId = TenantConstants.DefaultTenantId;

        public TenantMiddleware(RequestDelegate next, ILogger<TenantMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, ITenantService tenantService)
        {
            // Skip tenant resolution for non-API paths (static files, health checks, auth endpoints)
            var path = context.Request.Path.Value ?? "";
            if (!path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            // Allow auth endpoints without tenant context (login/register resolve tenant internally)
            if (path.Contains("/auth/", StringComparison.OrdinalIgnoreCase))
            {
                // For auth, use default tenant or JWT tenant_id if present
                var authTenantId = ResolveFromJwt(context) ?? DefaultTenantId;
                context.Items["TenantId"] = authTenantId;
                await _next(context);
                return;
            }

            // 1. Check if user is SystemAdmin (cross-tenant access)
            if (tenantService.IsSystemAdminContext())
            {
                // SystemAdmin: use JWT tenant or allow explicit header override
                var systemTenantId = ResolveFromHeader(context) ?? ResolveFromJwt(context) ?? DefaultTenantId;
                context.Items["TenantId"] = systemTenantId;
                await _next(context);
                return;
            }

            // 2. Resolve from JWT claim (primary method for authenticated users)
            var jwtTenantId = ResolveFromJwt(context);
            if (jwtTenantId.HasValue)
            {
                context.Items["TenantId"] = jwtTenantId.Value;
                await _next(context);
                return;
            }

            // 3. Fallback to default tenant for backward compatibility
            context.Items["TenantId"] = DefaultTenantId;
            await _next(context);
        }

        private static Guid? ResolveFromJwt(HttpContext context)
        {
            var tenantClaim = context.User.FindFirst("tenant_id")?.Value;
            if (!string.IsNullOrEmpty(tenantClaim) && Guid.TryParse(tenantClaim, out var tenantId))
                return tenantId;
            return null;
        }

        private static Guid? ResolveFromHeader(HttpContext context)
        {
            // X-Tenant-Id header allows SystemAdmin to switch tenant context
            var headerValue = context.Request.Headers["X-Tenant-Id"].FirstOrDefault();
            if (!string.IsNullOrEmpty(headerValue) && Guid.TryParse(headerValue, out var tenantId))
                return tenantId;
            return null;
        }
    }
}
