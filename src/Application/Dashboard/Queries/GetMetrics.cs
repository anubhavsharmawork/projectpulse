using MediatR;

namespace Application.Dashboard.Queries
{
    public record GetMetricsQuery() : IRequest<GetMetricsResult>;

    public record GetMetricsResult(int TasksTotal, int TasksCompleted, IDictionary<Guid, int> TasksPerUser);

    public class GetMetricsHandler : IRequestHandler<GetMetricsQuery, GetMetricsResult>
    {
        public Task<GetMetricsResult> Handle(GetMetricsQuery request, CancellationToken cancellationToken)
        {
            // template only
            return Task.FromResult(new GetMetricsResult(0, 0, new Dictionary<Guid, int>()));
        }
    }
}
