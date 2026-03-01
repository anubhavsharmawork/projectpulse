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
    public static class DomainAssetConfigSeeder
    {
        private static readonly Dictionary<string, DomainType> DomainFileMap = new()
        {
            ["IT"] = DomainType.IT,
            ["Healthcare"] = DomainType.Healthcare,
            ["PublicSafety"] = DomainType.PublicSafety,
            ["Construction"] = DomainType.Construction,
            ["Infrastructure"] = DomainType.Infrastructure,
            ["EconomicDevelopment"] = DomainType.EconomicDevelopment,
            ["Technology"] = DomainType.Technology
        };

        public static async Task SeedDomainAssetConfigsAsync(this IServiceProvider services, ILogger logger)
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            if (await db.DomainAssetConfigs.AnyAsync())
            {
                logger.LogInformation("Domain asset configs already seeded — skipping.");
                return;
            }

            var assembly = Assembly.GetExecutingAssembly();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var totalSeeded = 0;

            foreach (var (fileName, domainType) in DomainFileMap)
            {
                var resourceName = $"Infrastructure.Seed.AssetConfigs.{fileName}.json";
                using var stream = assembly.GetManifestResourceStream(resourceName);

                if (stream is null)
                {
                    logger.LogWarning("Asset config seed resource not found: {Resource}", resourceName);
                    continue;
                }

                var items = await JsonSerializer.DeserializeAsync<List<AssetConfigSeedDto>>(stream, options);
                if (items is null || items.Count == 0)
                {
                    logger.LogWarning("Failed to deserialize asset config seed: {File}", fileName);
                    continue;
                }

                foreach (var dto in items)
                {
                    if (!Enum.TryParse<AssetType>(dto.AssetType, out var assetType))
                    {
                        logger.LogWarning("Unknown AssetType '{AssetType}' in {File} seed", dto.AssetType, fileName);
                        continue;
                    }

                    if (!Enum.TryParse<AssetCategory>(dto.Category, out var category))
                    {
                        logger.LogWarning("Unknown AssetCategory '{Category}' in {File} seed", dto.Category, fileName);
                        continue;
                    }

                    var depMethod = DepreciationMethod.StraightLine;
                    if (!string.IsNullOrWhiteSpace(dto.DefaultDepreciationMethod))
                        Enum.TryParse(dto.DefaultDepreciationMethod, out depMethod);

                    db.DomainAssetConfigs.Add(new DomainAssetConfig
                    {
                        Id = Guid.NewGuid(),
                        DomainType = domainType,
                        AssetType = assetType,
                        Category = category,
                        DisplayLabel = dto.DisplayLabel,
                        Description = dto.Description,
                        DefaultDepreciationMethod = depMethod,
                        DefaultUsefulLifeYears = dto.DefaultUsefulLifeYears,
                        DefaultMaintenanceIntervalDays = dto.DefaultMaintenanceIntervalDays,
                        ComplianceNotes = dto.ComplianceNotes,
                        SortOrder = dto.SortOrder,
                        CreatedBy = "system-seed"
                    });
                }

                logger.LogInformation("Seeded {Count} asset configs for domain: {Domain}", items.Count, domainType);
                totalSeeded += items.Count;
            }

            await db.SaveChangesAsync();
            logger.LogInformation("Domain asset config seeding complete: {Total} configs across {DomainCount} domains.",
                totalSeeded, DomainFileMap.Count);
        }

        private sealed class AssetConfigSeedDto
        {
            public string AssetType { get; set; } = string.Empty;
            public string Category { get; set; } = string.Empty;
            public string DisplayLabel { get; set; } = string.Empty;
            public string? Description { get; set; }
            public string? DefaultDepreciationMethod { get; set; }
            public int DefaultUsefulLifeYears { get; set; } = 5;
            public int? DefaultMaintenanceIntervalDays { get; set; }
            public string? ComplianceNotes { get; set; }
            public string? DefaultFields { get; set; }
            public int SortOrder { get; set; }
        }
    }
}
