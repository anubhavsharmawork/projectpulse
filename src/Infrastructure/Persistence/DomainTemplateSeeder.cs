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
    public static class DomainTemplateSeeder
    {
        private static readonly string[] TemplateFiles = new[]
        {
            "IT", "Healthcare", "PublicSafety", "Construction",
            "Infrastructure", "EconomicDevelopment", "Technology"
        };

        public static async Task SeedDomainTemplatesAsync(this IServiceProvider services, ILogger logger)
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            if (await db.DomainTemplates.AnyAsync())
            {
                logger.LogInformation("Domain templates already seeded — checking for missing WorkItemTypeLabels.");
                await BackfillWorkItemTypeLabelsAsync(db, logger);
                return;
            }

            var assembly = Assembly.GetExecutingAssembly();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            foreach (var templateName in TemplateFiles)
            {
                var resourceName = $"Infrastructure.Seed.Templates.{templateName}.json";
                using var stream = assembly.GetManifestResourceStream(resourceName);

                if (stream is null)
                {
                    logger.LogWarning("Seed template resource not found: {Resource}", resourceName);
                    continue;
                }

                var dto = await JsonSerializer.DeserializeAsync<TemplateSeedDto>(stream, options);
                if (dto is null)
                {
                    logger.LogWarning("Failed to deserialize seed template: {Template}", templateName);
                    continue;
                }

                if (!Enum.TryParse<DomainType>(dto.DomainType, out var domainType))
                {
                    logger.LogWarning("Unknown DomainType in template: {DomainType}", dto.DomainType);
                    continue;
                }

                // Create workflow
                Workflow? workflow = null;
                if (dto.DefaultWorkflow is not null)
                {
                    workflow = new Workflow
                    {
                        Id = Guid.NewGuid(),
                        Name = dto.DefaultWorkflow.Name,
                        DomainType = domainType,
                        TenantId = TenantConstants.DefaultTenantId,
                        CreatedBy = "system-seed"
                    };

                    if (dto.DefaultWorkflow.States is not null)
                    {
                        foreach (var stateDto in dto.DefaultWorkflow.States)
                        {
                            workflow.States.Add(new WorkflowState
                            {
                                Id = Guid.NewGuid(),
                                WorkflowId = workflow.Id,
                                Name = stateDto.Name,
                                Order = stateDto.Order,
                                IsInitial = stateDto.IsInitial,
                                IsFinal = stateDto.IsFinal,
                                TenantId = TenantConstants.DefaultTenantId,
                                CreatedBy = "system-seed"
                            });
                        }
                    }

                    db.Workflows.Add(workflow);
                }

                // Create template
                var template = new DomainTemplate
                {
                    Id = Guid.NewGuid(),
                    Name = dto.Name,
                    DomainType = domainType,
                    DefaultWorkflowId = workflow?.Id,
                    DefaultNotificationRules = dto.DefaultNotificationRules is not null
                        ? JsonSerializer.Serialize(dto.DefaultNotificationRules)
                        : null,
                    WorkItemTypeLabels = dto.WorkItemTypeLabels is not null
                        ? JsonSerializer.Serialize(dto.WorkItemTypeLabels)
                        : null,
                    CreatedBy = "system-seed"
                };

                // Create custom fields
                if (dto.CustomFields is not null)
                {
                    foreach (var fieldDto in dto.CustomFields)
                    {
                        if (!Enum.TryParse<FieldType>(fieldDto.FieldType, out var fieldType))
                        {
                            logger.LogWarning("Unknown FieldType '{FieldType}' in template {Template}", fieldDto.FieldType, templateName);
                            continue;
                        }

                        template.CustomFields.Add(new CustomField
                        {
                            Id = Guid.NewGuid(),
                            Name = fieldDto.Name,
                            FieldType = fieldType,
                            DomainType = domainType,
                            IsRequired = fieldDto.IsRequired,
                            Options = fieldDto.Options,
                            ValidationRule = fieldDto.ValidationRule,
                            EntityType = fieldDto.EntityType,
                            DomainTemplateId = template.Id,
                            TenantId = TenantConstants.DefaultTenantId,
                            CreatedBy = "system-seed"
                        });
                    }
                }

                db.DomainTemplates.Add(template);
                logger.LogInformation("Seeded domain template: {Template} ({Count} custom fields)",
                    dto.Name, template.CustomFields.Count);
            }

            await db.SaveChangesAsync();
            logger.LogInformation("All domain templates seeded successfully.");
        }

        /// <summary>
        /// Patches existing templates that were seeded before WorkItemTypeLabels was added.
        /// Uses the same authoritative map as GetProjectConfigHandler.
        /// </summary>
        private static async Task BackfillWorkItemTypeLabelsAsync(AppDbContext db, ILogger logger)
        {
            var templatesWithoutLabels = await db.DomainTemplates
                .Where(t => t.WorkItemTypeLabels == null)
                .ToListAsync();

            if (templatesWithoutLabels.Count == 0)
            {
                logger.LogInformation("All templates already have WorkItemTypeLabels — nothing to backfill.");
                return;
            }

            var labelsByDomain = new Dictionary<DomainType, Dictionary<string, string>>
            {
                [DomainType.IT] = new() { ["1"] = "Epic", ["2"] = "User Story", ["3"] = "Task", ["4"] = "SubTask" },
                [DomainType.Healthcare] = new() { ["1"] = "Initiative", ["2"] = "Action Item", ["3"] = "Task", ["4"] = "SubTask" },
                [DomainType.PublicSafety] = new() { ["1"] = "Operation", ["2"] = "Action Plan", ["3"] = "Task", ["4"] = "SubTask" },
                [DomainType.Construction] = new() { ["1"] = "Phase", ["2"] = "Activity", ["3"] = "Punch Item", ["4"] = "SubItem" },
                [DomainType.Infrastructure] = new() { ["1"] = "Program", ["2"] = "Work Package", ["3"] = "Task", ["4"] = "SubTask" },
                [DomainType.EconomicDevelopment] = new() { ["1"] = "Program", ["2"] = "Initiative", ["3"] = "Task", ["4"] = "SubTask" },
                [DomainType.Technology] = new() { ["1"] = "Epic", ["2"] = "Feature", ["3"] = "Task", ["4"] = "SubTask" },
            };

            foreach (var template in templatesWithoutLabels)
            {
                if (labelsByDomain.TryGetValue(template.DomainType, out var labels))
                {
                    template.WorkItemTypeLabels = JsonSerializer.Serialize(labels);
                    logger.LogInformation("Backfilled WorkItemTypeLabels for template: {Name} ({Domain})",
                        template.Name, template.DomainType);
                }
            }

            await db.SaveChangesAsync();
        }

        // ── DTOs for JSON deserialization ──

        private sealed class TemplateSeedDto
        {
            public string Name { get; set; } = string.Empty;
            public string DomainType { get; set; } = string.Empty;
            public WorkflowSeedDto? DefaultWorkflow { get; set; }
            public object? DefaultNotificationRules { get; set; }
            public List<CustomFieldSeedDto>? CustomFields { get; set; }
            public Dictionary<string, string>? WorkItemTypeLabels { get; set; }
        }

        private sealed class WorkflowSeedDto
        {
            public string Name { get; set; } = string.Empty;
            public List<WorkflowStateSeedDto>? States { get; set; }
        }

        private sealed class WorkflowStateSeedDto
        {
            public string Name { get; set; } = string.Empty;
            public int Order { get; set; }
            public bool IsInitial { get; set; }
            public bool IsFinal { get; set; }
        }

        private sealed class CustomFieldSeedDto
        {
            public string Name { get; set; } = string.Empty;
            public string FieldType { get; set; } = string.Empty;
            public bool IsRequired { get; set; }
            public string? Options { get; set; }
            public string? ValidationRule { get; set; }
            public string? EntityType { get; set; }
        }
    }
}
