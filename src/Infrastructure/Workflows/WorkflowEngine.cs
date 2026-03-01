using Application.Common.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Infrastructure.Workflows
{
    public class WorkflowEngine : IWorkflowEngine
    {
        private readonly IAppDbContext _db;

        public WorkflowEngine(IAppDbContext db)
        {
            _db = db;
        }

        public async Task<(bool IsValid, string? Error)> ValidateTransitionAsync(
            Guid workItemId,
            Guid targetStateId,
            CancellationToken cancellationToken = default)
        {
            var workItem = await _db.WorkItems
                .AsNoTracking()
                .Include(wi => wi.CurrentState)
                .Include(wi => wi.CustomFieldValues)
                    .ThenInclude(cfv => cfv.CustomField)
                .FirstOrDefaultAsync(wi => wi.Id == workItemId, cancellationToken);

            if (workItem is null)
                return (false, "Work item not found");

            if (workItem.CurrentStateId is null)
                return (false, "Work item has no current workflow state");

            var currentState = workItem.CurrentState;
            if (currentState is null)
                return (false, "Current workflow state not found");

            // Resolve the effective workflow for this work item's project
            var project = await _db.Projects
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == workItem.ProjectId, cancellationToken);

            if (project is not null)
            {
                var effectiveWorkflowId = project.WorkflowId;
                if (!effectiveWorkflowId.HasValue)
                {
                    var domainWorkflow = await _db.Workflows
                        .AsNoTracking()
                        .FirstOrDefaultAsync(w => w.DomainType == project.DomainType, cancellationToken);
                    effectiveWorkflowId = domainWorkflow?.Id;
                }

                if (effectiveWorkflowId.HasValue)
                {
                    var targetBelongsToWorkflow = await _db.WorkflowStates
                        .AsNoTracking()
                        .AnyAsync(ws => ws.Id == targetStateId && ws.WorkflowId == effectiveWorkflowId.Value, cancellationToken);

                    if (!targetBelongsToWorkflow)
                        return (false, "Target state does not belong to this project's workflow");
                }
            }

            // Check AllowedTransitions
            var allowed = ParseGuidList(currentState.AllowedTransitions);
            if (allowed.Count > 0 && !allowed.Contains(targetStateId))
                return (false, $"Transition from '{currentState.Name}' to the target state is not allowed");

            // Check RequiredFields on the target state
            var targetState = await _db.WorkflowStates
                .AsNoTracking()
                .FirstOrDefaultAsync(ws => ws.Id == targetStateId, cancellationToken);

            if (targetState is null)
                return (false, "Target workflow state not found");

            var requiredFields = ParseStringList(targetState.RequiredFields);
            if (requiredFields.Count > 0)
            {
                var filledFieldNames = workItem.CustomFieldValues
                    .Where(cfv => !string.IsNullOrWhiteSpace(cfv.Value))
                    .Select(cfv => cfv.CustomField?.Name ?? string.Empty)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var missing = requiredFields.Where(rf => !filledFieldNames.Contains(rf)).ToList();
                if (missing.Count > 0)
                    return (false, $"Required fields missing before entering '{targetState.Name}': {string.Join(", ", missing)}");
            }

            return (true, null);
        }

        public async Task<Guid> TransitionAsync(
            Guid workItemId,
            Guid targetStateId,
            Guid userId,
            string? comment = null,
            CancellationToken cancellationToken = default)
        {
            var (isValid, error) = await ValidateTransitionAsync(workItemId, targetStateId, cancellationToken);
            if (!isValid)
                throw new InvalidOperationException(error);

            // Re-fetch with tracking
            var workItem = await _db.WorkItems
                .FirstOrDefaultAsync(wi => wi.Id == workItemId, cancellationToken)
                ?? throw new InvalidOperationException("Work item not found");

            var fromStateId = workItem.CurrentStateId
                ?? throw new InvalidOperationException("Work item has no current state");

            var targetState = await _db.WorkflowStates
                .FirstOrDefaultAsync(ws => ws.Id == targetStateId, cancellationToken)
                ?? throw new InvalidOperationException("Target state not found");

            // Create transition log
            var transition = new WorkflowTransition
            {
                Id = Guid.NewGuid(),
                WorkItemId = workItemId,
                FromStateId = fromStateId,
                ToStateId = targetStateId,
                TransitionedByUserId = userId,
                Comment = comment,
                CreatedBy = userId.ToString()
            };
            _db.WorkflowTransitions.Add(transition);

            // Update work item state
            workItem.CurrentStateId = targetStateId;
            workItem.UpdatedAt = DateTime.UtcNow;

            // If entering a final state, mark completed
            if (targetState.IsFinal)
            {
                workItem.IsCompleted = true;
                workItem.CompletedAt = DateTime.UtcNow;
            }
            else if (workItem.IsCompleted)
            {
                // Moving out of a final state resets completion
                workItem.IsCompleted = false;
                workItem.CompletedAt = null;
            }

            await _db.SaveChangesAsync(cancellationToken);
            return transition.Id;
        }

        public async Task<List<AvailableTransitionDto>> GetAvailableTransitionsAsync(
            Guid workItemId,
            CancellationToken cancellationToken = default)
        {
            var workItem = await _db.WorkItems
                .AsNoTracking()
                .Include(wi => wi.CurrentState)
                .FirstOrDefaultAsync(wi => wi.Id == workItemId, cancellationToken);

            if (workItem?.CurrentState is null)
                return new List<AvailableTransitionDto>();

            var allowed = ParseGuidList(workItem.CurrentState.AllowedTransitions);
            if (allowed.Count == 0)
                return new List<AvailableTransitionDto>();

            var states = await _db.WorkflowStates
                .AsNoTracking()
                .Where(ws => allowed.Contains(ws.Id))
                .OrderBy(ws => ws.Order)
                .ToListAsync(cancellationToken);

            return states.Select(s => new AvailableTransitionDto(
                s.Id,
                s.Name,
                s.Color,
                s.IsFinal,
                ParseStringList(s.RequiredFields)
            )).ToList();
        }

        public async Task AssignInitialStateAsync(
            Guid workItemId,
            Guid targetStateId,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            var workItem = await _db.WorkItems
                .FirstOrDefaultAsync(wi => wi.Id == workItemId, cancellationToken)
                ?? throw new InvalidOperationException("Work item not found");

            // Resolve the effective workflow for this work item's project
            var project = await _db.Projects
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == workItem.ProjectId, cancellationToken)
                ?? throw new InvalidOperationException("Project not found");

            var effectiveWorkflowId = project.WorkflowId;
            if (!effectiveWorkflowId.HasValue)
            {
                var domainWorkflow = await _db.Workflows
                    .AsNoTracking()
                    .FirstOrDefaultAsync(w => w.DomainType == project.DomainType, cancellationToken);
                effectiveWorkflowId = domainWorkflow?.Id;
            }

            if (!effectiveWorkflowId.HasValue)
                throw new InvalidOperationException("No workflow configured for this project");

            var targetState = await _db.WorkflowStates
                .AsNoTracking()
                .FirstOrDefaultAsync(ws => ws.Id == targetStateId && ws.WorkflowId == effectiveWorkflowId.Value, cancellationToken)
                ?? throw new InvalidOperationException("Target state does not belong to this project's workflow");

            workItem.CurrentStateId = targetStateId;
            workItem.UpdatedAt = DateTime.UtcNow;

            if (targetState.IsFinal)
            {
                workItem.IsCompleted = true;
                workItem.CompletedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync(cancellationToken);
        }

        private static List<Guid> ParseGuidList(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new List<Guid>();

            try
            {
                return JsonSerializer.Deserialize<List<Guid>>(json) ?? new List<Guid>();
            }
            catch
            {
                return new List<Guid>();
            }
        }

        private static List<string> ParseStringList(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new List<string>();

            try
            {
                return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }
    }
}
