using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;

namespace Infrastructure.Persistence
{
    [ExcludeFromCodeCoverage]
    public static class RolePermissionSeeder
    {
        public static async Task SeedRolesAndPermissionsAsync(this IServiceProvider services, ILogger logger)
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            if (await db.AppRoles.AnyAsync())
            {
                logger.LogInformation("Roles and permissions already seeded — skipping.");
                return;
            }

            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "Infrastructure.Seed.RolePermissions.json";
            using var stream = assembly.GetManifestResourceStream(resourceName);

            if (stream is null)
            {
                logger.LogWarning("Role permissions seed resource not found: {Resource}", resourceName);
                return;
            }

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var seedData = await JsonSerializer.DeserializeAsync<RolePermissionSeedDto>(stream, options);
            if (seedData is null)
            {
                logger.LogWarning("Failed to deserialize role permissions seed data.");
                return;
            }

            // Create permissions
            var permissionMap = new Dictionary<string, Permission>();
            if (seedData.Permissions is not null)
            {
                foreach (var permDto in seedData.Permissions)
                {
                    if (!Enum.TryParse<PermissionCategory>(permDto.Category, out var category))
                    {
                        logger.LogWarning("Unknown permission category: {Category}", permDto.Category);
                        continue;
                    }

                    var permission = new Permission
                    {
                        Id = Guid.NewGuid(),
                        Name = permDto.Name,
                        Category = category,
                        Description = permDto.Description,
                        CreatedBy = "system-seed"
                    };
                    permissionMap[permDto.Name] = permission;
                    db.Permissions.Add(permission);
                }
            }

            // Create roles and link permissions
            if (seedData.Roles is not null)
            {
                foreach (var roleDto in seedData.Roles)
                {
                    if (!Enum.TryParse<SystemRole>(roleDto.SystemRole, out var systemRole))
                    {
                        logger.LogWarning("Unknown system role: {SystemRole}", roleDto.SystemRole);
                        continue;
                    }

                    var appRole = new AppRole
                    {
                        Id = Guid.NewGuid(),
                        Name = roleDto.Name,
                        SystemRole = systemRole,
                        Description = roleDto.Description,
                        CreatedBy = "system-seed"
                    };

                    if (roleDto.Permissions is not null)
                    {
                        foreach (var permName in roleDto.Permissions)
                        {
                            if (permissionMap.TryGetValue(permName, out var perm))
                            {
                                appRole.RolePermissions.Add(new RolePermission
                                {
                                    Id = Guid.NewGuid(),
                                    AppRoleId = appRole.Id,
                                    PermissionId = perm.Id,
                                    CreatedBy = "system-seed"
                                });
                            }
                            else
                            {
                                logger.LogWarning("Permission '{Permission}' not found for role '{Role}'", permName, roleDto.Name);
                            }
                        }
                    }

                    db.AppRoles.Add(appRole);
                    logger.LogInformation("Seeded role: {Role} ({Count} permissions)", roleDto.Name, appRole.RolePermissions.Count);
                }
            }

            await db.SaveChangesAsync();
            logger.LogInformation("All roles and permissions seeded successfully.");
        }

        // ── DTOs for JSON deserialization ──

        private sealed class RolePermissionSeedDto
        {
            public List<PermissionSeedDto>? Permissions { get; set; }
            public List<RoleSeedDto>? Roles { get; set; }
        }

        private sealed class PermissionSeedDto
        {
            public string Name { get; set; } = string.Empty;
            public string Category { get; set; } = string.Empty;
            public string? Description { get; set; }
        }

        private sealed class RoleSeedDto
        {
            public string Name { get; set; } = string.Empty;
            public string SystemRole { get; set; } = string.Empty;
            public string? Description { get; set; }
            public List<string>? Permissions { get; set; }
        }
    }
}
