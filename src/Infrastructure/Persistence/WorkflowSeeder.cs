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
    public static class WorkflowSeeder
    {
        private static readonly string[] DomainFiles = new[]
        {
            "IT", "Healthcare", "PublicSafety", "Construction",
            "Infrastructure", "EconomicDevelopment", "Technology"
        };

        public static async Task SeedWorkflowsAsync(this IServiceProvider services, ILogger logger)
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Only seed if no workflows with AllowedTransitions exist (i.e., old basic ones may exist)
            var hasFullWorkflows = await db.WorkflowStates.AnyAsync(ws => ws.AllowedTransitions != null);
            if (hasFullWorkflows)
            {
                logger.LogInformation("Full workflow states already seeded — skipping.");
                return;
            }

            var assembly = Assembly.GetExecutingAssembly();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            foreach (var domainName in DomainFiles)
            {
                var resourceName = $"Infrastructure.Seed.Workflows.{domainName}.json";
                using var stream = assembly.GetManifestResourceStream(resourceName);

                if (stream is null)
                {
                    logger.LogWarning("Workflow seed resource not found: {Resource}", resourceName);
                    continue;
                }

                var dto = await JsonSerializer.DeserializeAsync<WorkflowSeedDto>(stream, options);
                if (dto is null)
                {
                    logger.LogWarning("Failed to deserialize workflow seed: {Domain}", domainName);
                    continue;
                }

                if (!Enum.TryParse<DomainType>(dto.DomainType, out var domainType))
                {
                    logger.LogWarning("Unknown DomainType in workflow seed: {DomainType}", dto.DomainType);
                    continue;
                }

                // Check if a workflow for this domain already exists
                var existing = await db.Workflows
                    .Include(w => w.States)
                    .FirstOrDefaultAsync(w => w.DomainType == domainType && w.Name == dto.Name);

                if (existing is not null)
                {
                    // Update existing states with new fields
                    await UpdateExistingWorkflow(db, existing, dto, logger);
                }
                else
                {
                    await CreateNewWorkflow(db, dto, domainType, logger);
                }
            }

            await db.SaveChangesAsync();
            logger.LogInformation("All workflow seeds processed successfully.");
        }

        private static async Task UpdateExistingWorkflow(AppDbContext db, Workflow existing, WorkflowSeedDto dto, ILogger logger)
        {
            if (dto.States is null) return;

            // Build name-to-Id map from existing states
            var stateMap = existing.States.ToDictionary(s => s.Name, s => s.Id);

            foreach (var stateDto in dto.States)
            {
                var state = existing.States.FirstOrDefault(s => s.Name == stateDto.Name);
                if (state is null) continue;

                state.Color = stateDto.Color ?? "#6B7280";
                state.NotifyOnEntry = stateDto.NotifyOnEntry;
                state.RequiredFields = stateDto.RequiredFields is not null && stateDto.RequiredFields.Count > 0
                    ? JsonSerializer.Serialize(stateDto.RequiredFields)
                    : null;
                state.AllowedTransitions = ResolveTransitionIds(stateDto.AllowedTransitions, stateMap);
                state.UpdatedAt = DateTime.UtcNow;
            }

            logger.LogInformation("Updated existing workflow: {Name} ({Count} states)", existing.Name, existing.States.Count);
            await Task.CompletedTask;
        }

        private static async Task CreateNewWorkflow(AppDbContext db, WorkflowSeedDto dto, DomainType domainType, ILogger logger)
        {
            if (dto.States is null) return;

            var workflow = new Workflow
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                DomainType = domainType,
                TenantId = TenantConstants.DefaultTenantId,
                CreatedBy = "system-seed"
            };

            // First pass: create all states (no transitions yet)
            var stateMap = new Dictionary<string, Guid>();
            foreach (var stateDto in dto.States)
            {
                var state = new WorkflowState
                {
                    Id = Guid.NewGuid(),
                    WorkflowId = workflow.Id,
                    Name = stateDto.Name,
                    Order = stateDto.Order,
                    Color = stateDto.Color ?? "#6B7280",
                    IsInitial = stateDto.IsInitial,
                    IsFinal = stateDto.IsFinal,
                    NotifyOnEntry = stateDto.NotifyOnEntry,
                    RequiredFields = stateDto.RequiredFields is not null && stateDto.RequiredFields.Count > 0
                        ? JsonSerializer.Serialize(stateDto.RequiredFields)
                        : null,
                    TenantId = TenantConstants.DefaultTenantId,
                    CreatedBy = "system-seed"
                };
                stateMap[stateDto.Name] = state.Id;
                workflow.States.Add(state);
            }

            // Second pass: resolve AllowedTransitions by name → GUID
            for (int i = 0; i < dto.States.Count; i++)
            {
                var stateDto = dto.States[i];
                workflow.States[i].AllowedTransitions = ResolveTransitionIds(stateDto.AllowedTransitions, stateMap);
            }

            db.Workflows.Add(workflow);
            logger.LogInformation("Seeded workflow: {Name} ({Count} states)", dto.Name, workflow.States.Count);
            await Task.CompletedTask;
        }

        private static string? ResolveTransitionIds(List<string>? transitionNames, Dictionary<string, Guid> stateMap)
        {
            if (transitionNames is null || transitionNames.Count == 0)
                return null;

            var ids = new List<Guid>();
            foreach (var name in transitionNames)
            {
                if (stateMap.TryGetValue(name, out var id))
                    ids.Add(id);
            }
            return ids.Count > 0 ? JsonSerializer.Serialize(ids) : null;
        }

        // ── DTOs ──

        private sealed class WorkflowSeedDto
        {
            public string Name { get; set; } = string.Empty;
            public string DomainType { get; set; } = string.Empty;
            public List<WorkflowStateSeedDto>? States { get; set; }
        }

        private sealed class WorkflowStateSeedDto
        {
            public string Name { get; set; } = string.Empty;
            public int Order { get; set; }
            public string? Color { get; set; }
            public bool IsInitial { get; set; }
            public bool IsFinal { get; set; }
            public List<string>? AllowedTransitions { get; set; }
            public List<string>? RequiredFields { get; set; }
            public bool NotifyOnEntry { get; set; }
        }
    }
}
