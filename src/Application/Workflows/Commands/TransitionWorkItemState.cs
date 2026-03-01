using Application.Common.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Application.Workflows.Commands
{
    public record TransitionWorkItemStateCommand(
        Guid WorkItemId,
        Guid TargetStateId,
        string? Comment = null) : IRequest<TransitionWorkItemStateResult>;

    public record TransitionWorkItemStateResult(Guid TransitionId);

    public class TransitionWorkItemStateHandler : IRequestHandler<TransitionWorkItemStateCommand, TransitionWorkItemStateResult>
    {
        private readonly IWorkflowEngine _engine;
        private readonly IHttpContextAccessor _http;

        public TransitionWorkItemStateHandler(IWorkflowEngine engine, IHttpContextAccessor http)
        {
            _engine = engine;
            _http = http;
        }

        public async Task<TransitionWorkItemStateResult> Handle(
            TransitionWorkItemStateCommand request,
            CancellationToken cancellationToken)
        {
            var userIdClaim = _http.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userId = Guid.TryParse(userIdClaim, out var id) ? id : Guid.Empty;

            var transitionId = await _engine.TransitionAsync(
                request.WorkItemId,
                request.TargetStateId,
                userId,
                request.Comment,
                cancellationToken);

            return new TransitionWorkItemStateResult(transitionId);
        }
    }
}
