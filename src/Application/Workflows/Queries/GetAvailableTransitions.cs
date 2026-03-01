using Application.Common.Interfaces;
using MediatR;

namespace Application.Workflows.Queries
{
    public record GetAvailableTransitionsQuery(Guid WorkItemId) : IRequest<List<AvailableTransitionDto>>;

    public class GetAvailableTransitionsHandler : IRequestHandler<GetAvailableTransitionsQuery, List<AvailableTransitionDto>>
    {
        private readonly IWorkflowEngine _engine;

        public GetAvailableTransitionsHandler(IWorkflowEngine engine)
        {
            _engine = engine;
        }

        public Task<List<AvailableTransitionDto>> Handle(
            GetAvailableTransitionsQuery request,
            CancellationToken cancellationToken)
        {
            return _engine.GetAvailableTransitionsAsync(request.WorkItemId, cancellationToken);
        }
    }
}
