using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Budget.Queries;
using Application.UnitTests.TestHelpers;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Xunit;

namespace Application.UnitTests.Budget
{
    public class GetBudgetStatusHandlerTests
    {
        [Fact]
        public async Task Handle_CalculatesVarianceAndEpicSums()
        {
            var projectId = Guid.NewGuid();
            using var db = TestDbContextFactory.CreateWithData(ctx =>
            {
                var project = new Project { Id = projectId, Name = "Proj", IsActive = true, EstimatedCost = 100m, ActualCost = 120m, DomainType = DomainType.IT };
                var epic1 = new EpicWorkItem { Id = Guid.NewGuid(), ProjectId = projectId, Title = "E1", EstimatedCost = 30m, ActualCost = 40m };
                var epic2 = new EpicWorkItem { Id = Guid.NewGuid(), ProjectId = projectId, Title = "E2", EstimatedCost = 20m, ActualCost = 10m };
                project.WorkItems.Add(epic1);
                project.WorkItems.Add(epic2);
                ctx.Projects.Add(project);
            });

            var handler = new GetBudgetStatusHandler(db);
            var result = await handler.Handle(new GetBudgetStatusQuery(), CancellationToken.None);

            result.Should().ContainSingle(r => r.ProjectId == projectId);
            var dto = result.Single(r => r.ProjectId == projectId);
            dto.EstimatedCost.Should().Be(100m);
            dto.ActualCost.Should().Be(120m);
            dto.BudgetVariance.Should().Be(20m);
            dto.EpicCount.Should().Be(2);
            dto.EpicEstimatedTotal.Should().Be(50m);
            dto.EpicActualTotal.Should().Be(50m);
        }
    }
}
