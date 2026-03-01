using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Asp.Versioning;
using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/tenants")]
    [Authorize]
    public class TenantsController : ControllerBase
    {
        private readonly IAppDbContext _db;
        private readonly ITenantService _tenantService;
        private readonly ILogger<TenantsController> _logger;

        public TenantsController(IAppDbContext db, ITenantService tenantService, ILogger<TenantsController> logger)
        {
            _db = db;
            _tenantService = tenantService;
            _logger = logger;
        }

        /// <summary>
        /// Get current tenant details (any authenticated user).
        /// </summary>
        [HttpGet("current")]
        public async Task<IActionResult> GetCurrent(CancellationToken ct)
        {
            try
            {
                var tenantId = _tenantService.GetCurrentTenantId();
                _logger.LogInformation("Fetching current tenant. TenantId: {TenantId}", tenantId);

                var tenant = await _db.Tenants
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == tenantId, ct);

                if (tenant is null)
                {
                    _logger.LogWarning("Current tenant not found for TenantId: {TenantId}", tenantId);
                    return NotFound(new { error = "Current tenant not found." });
                }

                return Ok(new TenantResponse(tenant));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Tenant context is not available for current user.");
                return NotFound(new { error = "Tenant context is not available." });
            }
        }

        /// <summary>
        /// Update current tenant settings (Admin only).
        /// </summary>
        [HttpPut("current")]
        [Authorize(Policy = "AdminPolicy")]
        public async Task<IActionResult> UpdateCurrent([FromBody] UpdateTenantRequest request, CancellationToken ct)
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var tenant = await _db.Tenants.FindAsync(new object[] { tenantId }, ct);
            if (tenant is null) return NotFound();

            if (!string.IsNullOrWhiteSpace(request.Name)) tenant.Name = request.Name;
            if (request.Settings is not null) tenant.Settings = request.Settings;
            tenant.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
            return Ok(new TenantResponse(tenant));
        }

        /// <summary>
        /// Get usage metrics for current tenant (Admin only).
        /// </summary>
        [HttpGet("current/usage")]
        [Authorize(Policy = "AdminPolicy")]
        public async Task<IActionResult> GetUsage(CancellationToken ct)
        {
            try
            {
                var tenantId = _tenantService.GetCurrentTenantId();
                _logger.LogInformation("Fetching usage for tenant: {TenantId}", tenantId);

                var tenant = await _db.Tenants
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == tenantId, ct);

                if (tenant is null)
                    return NotFound(new { error = "Current tenant not found." });

                var userCount = await _db.Users.CountAsync(ct);
                var projectCount = await _db.Projects.CountAsync(ct);
                var storageBytes = await _db.Attachments.SumAsync(a => a.SizeBytes, ct);

                return Ok(new
                {
                    tenant.Tier,
                    Users = new { Current = userCount, Max = tenant.MaxUsers, Unlimited = tenant.MaxUsers < 0 },
                    Projects = new { Current = projectCount, Max = tenant.MaxProjects, Unlimited = tenant.MaxProjects < 0 },
                    Storage = new { CurrentBytes = storageBytes, MaxBytes = tenant.MaxStorageBytes, Unlimited = tenant.MaxStorageBytes < 0 }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch tenant usage.");
                return NotFound(new { error = "Tenant context is not available." });
            }
        }

        /// <summary>
        /// List all tenants (SystemAdmin only, including read-only demo users).
        /// </summary>
        [HttpGet]
        [Authorize(Policy = "SystemAdminPolicy")]
        public async Task<IActionResult> ListAll(CancellationToken ct)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst("system_role")?.Value;
            _logger.LogInformation("Fetching all tenants. User: {UserId}, Role: {Role}", userId, role);

            try
            {
                var tenants = await _db.Tenants
                    .AsNoTracking()
                    .OrderBy(t => t.Name)
                    .ToListAsync(ct);

                _logger.LogInformation("Returning {Count} tenants.", tenants.Count);
                return Ok(tenants.Select(t => new TenantResponse(t)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load tenants: {Message}", ex.Message);
                return StatusCode(500, new { error = "Failed to load tenants.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Health check for tenant system (no auth required).
        /// </summary>
        [HttpGet("health")]
        [AllowAnonymous]
        public async Task<IActionResult> Health(CancellationToken ct)
        {
            try
            {
                var tenantsCount = await _db.Tenants.CountAsync(ct);
                return Ok(new
                {
                    tenantsCount,
                    dbConnected = true,
                    tenantServiceAvailable = _tenantService is not null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tenant health check failed.");
                return Ok(new
                {
                    tenantsCount = -1,
                    dbConnected = false,
                    tenantServiceAvailable = _tenantService is not null,
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Create a new tenant (SystemAdmin only, not demo users).
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "SystemAdminWritePolicy")]
        public async Task<IActionResult> Create([FromBody] CreateTenantRequest request, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length < 2)
                return BadRequest(new { error = "Organization name must be at least 2 characters." });

            // Auto-generate subdomain from name if not explicitly provided
            var subdomain = string.IsNullOrWhiteSpace(request.Subdomain)
                ? GenerateSubdomain(request.Name)
                : request.Subdomain.Trim().ToLowerInvariant();

            // Ensure subdomain uniqueness
            var baseSubdomain = subdomain;
            var suffix = 2;
            while (await _db.Tenants.AnyAsync(t => t.Subdomain == subdomain, ct))
            {
                subdomain = $"{baseSubdomain}{suffix}";
                suffix++;
            }

            var (maxUsers, maxProjects, maxStorage) = request.Tier switch
            {
                TenantTier.Starter => (5, 10, 10L * 1024 * 1024 * 1024),
                TenantTier.Business => (50, -1, 100L * 1024 * 1024 * 1024),
                TenantTier.Enterprise => (-1, -1, -1L),
                _ => (5, 10, 10L * 1024 * 1024 * 1024)
            };

            var tenant = new Tenant
            {
                Id = Guid.NewGuid(),
                Name = request.Name.Trim(),
                Subdomain = subdomain,
                Tier = request.Tier,
                MaxUsers = maxUsers,
                MaxProjects = maxProjects,
                MaxStorageBytes = maxStorage,
                IsActive = true
            };

            _db.Tenants.Add(tenant);
            await _db.SaveChangesAsync(ct);

            return CreatedAtAction(nameof(GetCurrent), new { version = "1" }, new TenantResponse(tenant));
        }

        /// <summary>
        /// Update a specific tenant (SystemAdmin only, not demo users).
        /// </summary>
        [HttpPut("{tenantId:guid}")]
        [Authorize(Policy = "SystemAdminWritePolicy")]
        public async Task<IActionResult> Update(Guid tenantId, [FromBody] AdminUpdateTenantRequest request, CancellationToken ct)
        {
            var tenant = await _db.Tenants.FindAsync(new object[] { tenantId }, ct);
            if (tenant is null) return NotFound();

            if (request.Tier.HasValue) tenant.Tier = request.Tier.Value;
            if (request.MaxUsers.HasValue) tenant.MaxUsers = request.MaxUsers.Value;
            if (request.MaxProjects.HasValue) tenant.MaxProjects = request.MaxProjects.Value;
            if (request.MaxStorageBytes.HasValue) tenant.MaxStorageBytes = request.MaxStorageBytes.Value;
            if (request.IsActive.HasValue) tenant.IsActive = request.IsActive.Value;
            tenant.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
            return Ok(new TenantResponse(tenant));
        }

        /// <summary>
        /// Suspend a tenant (SystemAdmin only, not demo users).
        /// </summary>
        [HttpPost("{tenantId:guid}/suspend")]
        [Authorize(Policy = "SystemAdminWritePolicy")]
        public async Task<IActionResult> Suspend(Guid tenantId, CancellationToken ct)
        {
            var tenant = await _db.Tenants.FindAsync(new object[] { tenantId }, ct);
            if (tenant is null) return NotFound();

            tenant.IsActive = false;
            tenant.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return Ok(new { message = "Tenant suspended." });
        }

        /// <summary>
        /// Activate a tenant (SystemAdmin only, not demo users).
        /// </summary>
        [HttpPost("{tenantId:guid}/activate")]
        [Authorize(Policy = "SystemAdminWritePolicy")]
        public async Task<IActionResult> Activate(Guid tenantId, CancellationToken ct)
        {
            var tenant = await _db.Tenants.FindAsync(new object[] { tenantId }, ct);
            if (tenant is null) return NotFound();

            tenant.IsActive = true;
            tenant.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return Ok(new { message = "Tenant activated." });
        }

        private static string GenerateSubdomain(string name)
        {
            // Convert "Acme Corporation" → "acme-corporation", strip non-alphanumeric
            var slug = System.Text.RegularExpressions.Regex.Replace(name.Trim().ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');
            if (slug.Length < 3) slug = slug.PadRight(3, '0');
            if (slug.Length > 50) slug = slug[..50].TrimEnd('-');
            return slug;
        }
    }

    // Request/Response DTOs
    public record TenantResponse(Guid Id, string Name, string Subdomain, string Tier, int MaxUsers, int MaxProjects, long MaxStorageBytes, bool IsActive, DateTime CreatedAt)
    {
        public TenantResponse(Tenant t) : this(t.Id, t.Name, t.Subdomain, t.Tier.ToString(), t.MaxUsers, t.MaxProjects, t.MaxStorageBytes, t.IsActive, t.CreatedAt) { }
    }

    public record CreateTenantRequest(string Name, string? Subdomain, TenantTier Tier);
    public record UpdateTenantRequest(string? Name, string? Settings);
    public record AdminUpdateTenantRequest(TenantTier? Tier, int? MaxUsers, int? MaxProjects, long? MaxStorageBytes, bool? IsActive);
}
