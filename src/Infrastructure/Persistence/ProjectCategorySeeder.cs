using Domain.Constants;
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
    public static class ProjectCategorySeeder
    {
        private static readonly string[] DomainFiles = new[]
        {
            "Healthcare", "PublicSafety", "Infrastructure",
            "EconomicDevelopment", "Technology"
        };

        public static async Task SeedProjectCategoriesAsync(this IServiceProvider services, ILogger logger)
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            if (await db.ProjectCategories.AnyAsync())
            {
                logger.LogInformation("Project categories already seeded — skipping.");
                return;
            }

            var assembly = Assembly.GetExecutingAssembly();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            foreach (var domainName in DomainFiles)
            {
                var resourceName = $"Infrastructure.Seed.Categories.{domainName}.json";
                using var stream = assembly.GetManifestResourceStream(resourceName);

                if (stream is null)
                {
                    logger.LogWarning("Category seed resource not found: {Resource}", resourceName);
                    continue;
                }

                var dto = await JsonSerializer.DeserializeAsync<CategoryFileSeedDto>(stream, options);
                if (dto is null)
                {
                    logger.LogWarning("Failed to deserialize category seed: {Domain}", domainName);
                    continue;
                }

                if (!Enum.TryParse<DomainType>(dto.DomainType, out var domainType))
                {
                    logger.LogWarning("Unknown DomainType in category seed: {DomainType}", dto.DomainType);
                    continue;
                }

                if (dto.Categories is null) continue;

                foreach (var catDto in dto.Categories)
                {
                    var category = new ProjectCategory
                    {
                        Id = Guid.NewGuid(),
                        Name = catDto.Name,
                        DomainType = domainType,
                        Description = catDto.Description,
                        DefaultTeamRoles = catDto.DefaultTeamRoles is not null
                            ? JsonSerializer.Serialize(catDto.DefaultTeamRoles)
                            : null,
                        TenantId = TenantConstants.DefaultTenantId,
                        CreatedBy = "system-seed"
                    };
                    db.ProjectCategories.Add(category);
                }

                logger.LogInformation("Seeded {Count} project categories for {Domain}",
                    dto.Categories.Count, domainName);
            }

            await db.SaveChangesAsync();
            logger.LogInformation("All project categories seeded successfully.");
        }

        // ── DTOs ──

        private sealed class CategoryFileSeedDto
        {
            public string DomainType { get; set; } = string.Empty;
            public List<CategorySeedDto>? Categories { get; set; }
        }

        private sealed class CategorySeedDto
        {
            public string Name { get; set; } = string.Empty;
            public string? Description { get; set; }
            public List<object>? DefaultTeamRoles { get; set; }
        }
    }
}
