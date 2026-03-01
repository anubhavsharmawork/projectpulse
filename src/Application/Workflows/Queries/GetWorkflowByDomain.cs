using Application.Common.Interfaces;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Workflows.Queries
{
    public record GetWorkflowByDomainQuery(DomainType DomainType) : IRequest<WorkflowDto?>;

    public record WorkflowDto(
        Guid Id,
        string Name,
        string DomainType,
        List<WorkflowStateDto> States);

    public record WorkflowStateDto(
        Guid Id,
        string Name,
        int Order,
        string Color,
        bool IsInitial,
        bool IsFinal,
        List<Guid> AllowedTransitions,
        List<string> RequiredFields,
        bool NotifyOnEntry);

    public class GetWorkflowByDomainHandler : IRequestHandler<GetWorkflowByDomainQuery, WorkflowDto?>
    {
        private readonly IAppDbContext _db;

        public GetWorkflowByDomainHandler(IAppDbContext db)
        {
            _db = db;
        }

        public async Task<WorkflowDto?> Handle(GetWorkflowByDomainQuery request, CancellationToken cancellationToken)
        {
            var workflow = await _db.Workflows
                .AsNoTracking()
                .Include(w => w.States.OrderBy(s => s.Order))
                .FirstOrDefaultAsync(w => w.DomainType == request.DomainType, cancellationToken);

            if (workflow is null)
                return null;

            return new WorkflowDto(
                workflow.Id,
                workflow.Name,
                workflow.DomainType.ToString(),
                workflow.States.Select(s => new WorkflowStateDto(
                    s.Id,
                    s.Name,
                    s.Order,
                    s.Color,
                    s.IsInitial,
                    s.IsFinal,
                    ParseGuidList(s.AllowedTransitions),
                    ParseStringList(s.RequiredFields),
                    s.NotifyOnEntry
                )).ToList());
        }

        private static List<Guid> ParseGuidList(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new();
            try { return System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(json) ?? new(); }
            catch { return new(); }
        }

        private static List<string> ParseStringList(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new();
            try { return System.Text.Json.JsonSerializer.Deserialize<List<string>>(json) ?? new(); }
            catch { return new(); }
        }
    }
}
