using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Dashboard.Queries
{
    public record GetMetricsQuery(DomainType? DomainType = null) : IRequest<DashboardResult>;

    // ── Shared KPIs (all domains) ──
    public record CommonKpis(
        int TotalTasks,
        int CompletedTasks,
        decimal CompletionRate,
        int OverdueItems,
        decimal TeamUtilization,
        IDictionary<Guid, int> TasksPerUser);

    // ── IT-specific ──
    public record ItKpis(
        List<decimal> VelocityTrend,
        int OpenBugs,
        IDictionary<string, int> BugsBySeverity,
        decimal TechDebtRatio);

    // ── Healthcare-specific ──
    public record HealthcareKpis(
        IDictionary<string, int> ComplianceStatus,
        int PatientsAffectedTotal,
        decimal TrainingProgressPercent);

    // ── Construction-specific ──
    public record ConstructionKpis(
        IDictionary<string, int> PermitStatusSummary,
        decimal InspectionPassRate,
        int SafetyIncidents);

    // ── Infrastructure-specific ──
    public record InfrastructureKpis(
        decimal BudgetVariancePercent,
        decimal MaintenanceAdherencePercent);

    public record DashboardResult(
        CommonKpis Common,
        ItKpis? IT,
        HealthcareKpis? Healthcare,
        ConstructionKpis? Construction,
        InfrastructureKpis? Infrastructure);

    public class GetMetricsHandler : IRequestHandler<GetMetricsQuery, DashboardResult>
    {
        private readonly IAppDbContext _db;

        public GetMetricsHandler(IAppDbContext db) => _db = db;

        public async Task<DashboardResult> Handle(GetMetricsQuery request, CancellationToken cancellationToken)
        {
            // ── Base query scoped by domain if provided ──
            var workItemsQuery = _db.WorkItems.AsNoTracking().AsQueryable();
            var projectsQuery = _db.Projects.AsNoTracking().Where(p => p.IsActive);

            if (request.DomainType.HasValue)
            {
                var projectIds = await projectsQuery
                    .Where(p => p.DomainType == request.DomainType.Value)
                    .Select(p => p.Id)
                    .ToListAsync(cancellationToken);
                workItemsQuery = workItemsQuery.Where(w => projectIds.Contains(w.ProjectId));
            }

            // ── Common KPIs ──
            var total = await workItemsQuery.CountAsync(cancellationToken);
            var completed = await workItemsQuery.CountAsync(w => w.IsCompleted, cancellationToken);
            var completionRate = total > 0 ? Math.Round((decimal)completed / total * 100, 1) : 0;
            var overdue = await workItemsQuery.CountAsync(
                w => !w.IsCompleted && w.CreatedAt < DateTime.UtcNow.AddDays(-14), cancellationToken);

            var tasksPerUser = await workItemsQuery
                .Where(w => w.AssigneeId.HasValue)
                .GroupBy(w => w.AssigneeId!.Value)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.UserId, x => x.Count, cancellationToken);

            // Team utilization: average across active team members
            var teamMembers = await _db.TeamMembers.AsNoTracking()
                .Where(tm => tm.IsActive && tm.AvailabilityHoursPerWeek > 0)
                .ToListAsync(cancellationToken);
            var weekAgo = DateTime.UtcNow.AddDays(-7);
            var memberIds = teamMembers.Select(m => m.UserId).ToList();
            var loggedHours = await _db.TimeEntries.AsNoTracking()
                .Where(te => memberIds.Contains(te.UserId) && te.LoggedDate >= weekAgo)
                .GroupBy(te => te.UserId)
                .Select(g => new { UserId = g.Key, Hours = g.Sum(te => te.Hours) })
                .ToDictionaryAsync(x => x.UserId, x => x.Hours, cancellationToken);

            var totalAvail = teamMembers.Sum(m => m.AvailabilityHoursPerWeek);
            var totalLogged = teamMembers.Sum(m => loggedHours.GetValueOrDefault(m.UserId, 0));
            var utilization = totalAvail > 0 ? Math.Round(totalLogged / totalAvail * 100, 1) : 0;

            var common = new CommonKpis(total, completed, completionRate, overdue, utilization, tasksPerUser);

            // ── Domain-specific KPIs ──
            ItKpis? itKpis = null;
            HealthcareKpis? healthcareKpis = null;
            ConstructionKpis? constructionKpis = null;
            InfrastructureKpis? infraKpis = null;

            var domain = request.DomainType;

            if (!domain.HasValue || domain is DomainType.IT or DomainType.Technology)
                itKpis = await BuildItKpis(workItemsQuery, cancellationToken);

            if (!domain.HasValue || domain is DomainType.Healthcare)
                healthcareKpis = await BuildHealthcareKpis(workItemsQuery, cancellationToken);

            if (!domain.HasValue || domain is DomainType.Construction)
                constructionKpis = await BuildConstructionKpis(workItemsQuery, cancellationToken);

            if (!domain.HasValue || domain is DomainType.Infrastructure)
                infraKpis = await BuildInfrastructureKpis(projectsQuery, workItemsQuery, cancellationToken);

            return new DashboardResult(common, itKpis, healthcareKpis, constructionKpis, infraKpis);
        }

        private async Task<ItKpis> BuildItKpis(IQueryable<WorkItem> items, CancellationToken ct)
        {
            // Velocity: completed items per week for last 4 weeks
            var velocity = new List<decimal>();
            for (int i = 3; i >= 0; i--)
            {
                var weekStart = DateTime.UtcNow.AddDays(-7 * (i + 1));
                var weekEnd = DateTime.UtcNow.AddDays(-7 * i);
                var count = await items.CountAsync(
                    w => w.IsCompleted && w.CompletedAt >= weekStart && w.CompletedAt < weekEnd, ct);
                velocity.Add(count);
            }

            // Open bugs: use WorkItemType discriminator instead of title heuristic
            var openBugs = await items.CountAsync(
                w => !w.IsCompleted && w.Type == WorkItemType.Bug, ct);

            // Bugs by severity: query actual BugSeverity from BugWorkItem entities
            var severityCounts = await items
                .OfType<BugWorkItem>()
                .Where(b => !b.IsCompleted)
                .GroupBy(b => b.Severity)
                .Select(g => new { Severity = g.Key, Count = g.Count() })
                .ToListAsync(ct);

            var bugsBySeverity = new Dictionary<string, int>
            {
                ["Critical"] = 0,
                ["High"] = 0,
                ["Medium"] = 0,
                ["Low"] = 0
            };
            foreach (var sc in severityCounts)
            {
                bugsBySeverity[sc.Severity.ToString()] = sc.Count;
            }

            // Tech debt ratio: items with type-based or title-based detection
            var totalOpen = await items.CountAsync(w => !w.IsCompleted, ct);
            var debtItems = await items.CountAsync(
                w => !w.IsCompleted &&
                     (w.Title.ToLower().Contains("tech debt") || w.Title.ToLower().Contains("refactor")), ct);
            var techDebtRatio = totalOpen > 0 ? Math.Round((decimal)debtItems / totalOpen * 100, 1) : 0;

            return new ItKpis(velocity, openBugs, bugsBySeverity, techDebtRatio);
        }

        private async Task<HealthcareKpis> BuildHealthcareKpis(IQueryable<WorkItem> items, CancellationToken ct)
        {
            var total = await items.CountAsync(ct);
            var completed = await items.CountAsync(w => w.IsCompleted, ct);
            var complianceStatus = new Dictionary<string, int>
            {
                ["Compliant"] = completed,
                ["Pending Review"] = total - completed,
                ["Non-Compliant"] = 0
            };

            // Custom field "EstimatedPatientsAffected" sum (fallback 0)
            var patientValues = await items
                .SelectMany(w => w.CustomFieldValues)
                .Where(cfv => cfv.CustomField != null && cfv.CustomField.Name == "EstimatedPatientsAffected" && cfv.Value != null)
                .Select(cfv => cfv.Value!)
                .ToListAsync(ct);
            var patientsAffected = patientValues
                .Sum(v => int.TryParse(v, out var n) ? n : 0);

            var trainingProgress = total > 0 ? Math.Round((decimal)completed / total * 100, 1) : 0;

            return new HealthcareKpis(complianceStatus, patientsAffected, trainingProgress);
        }

        private async Task<ConstructionKpis> BuildConstructionKpis(IQueryable<WorkItem> items, CancellationToken ct)
        {
            // Permit status: items in "Permitting" state vs total
            var total = await items.CountAsync(ct);
            var inPermitting = await items.CountAsync(
                w => w.CurrentState != null && w.CurrentState.Name == "Permitting", ct);
            var permitted = await items.CountAsync(
                w => w.CurrentState != null && w.CurrentState.Order > 2, ct); // past Permitting

            var permitSummary = new Dictionary<string, int>
            {
                ["Pending"] = inPermitting,
                ["Approved"] = permitted,
                ["Total"] = total
            };

            // Inspection pass: items past "Inspection" state
            var pastInspection = await items.CountAsync(
                w => w.CurrentState != null && w.CurrentState.Name != "Inspection"
                     && w.CurrentState.Order > 6, ct);
            var atInspection = await items.CountAsync(
                w => w.CurrentState != null && w.CurrentState.Name == "Inspection", ct);
            var inspectionTotal = pastInspection + atInspection;
            var passRate = inspectionTotal > 0
                ? Math.Round((decimal)pastInspection / inspectionTotal * 100, 1) : 0;

            return new ConstructionKpis(permitSummary, passRate, 0);
        }

        private async Task<InfrastructureKpis> BuildInfrastructureKpis(
            IQueryable<Project> projects, IQueryable<WorkItem> items, CancellationToken ct)
        {
            var projs = await projects.Where(p => p.DomainType == DomainType.Infrastructure)
                .Select(p => new { p.EstimatedCost, p.ActualCost })
                .ToListAsync(ct);

            var totalEstimated = projs.Sum(p => p.EstimatedCost);
            var totalActual = projs.Sum(p => p.ActualCost);
            var variancePct = totalEstimated > 0
                ? Math.Round((totalActual - totalEstimated) / totalEstimated * 100, 1) : 0;

            // Maintenance adherence: completed items with "maintenance" in title / total
            var totalMaint = await items.CountAsync(
                w => w.Title.ToLower().Contains("maintenance"), ct);
            var completedMaint = await items.CountAsync(
                w => w.IsCompleted && w.Title.ToLower().Contains("maintenance"), ct);
            var adherence = totalMaint > 0
                ? Math.Round((decimal)completedMaint / totalMaint * 100, 1) : 0;

            return new InfrastructureKpis(variancePct, adherence);
        }
    }
}

