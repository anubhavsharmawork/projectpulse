using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.TimeTracking.Queries;
using Application.UnitTests.TestHelpers;
using Domain.Entities;
using FluentAssertions;
using Xunit;

namespace Application.UnitTests.TimeTracking
{
    public class GetTimeEntriesHandlerTests
    {
        [Fact]
        public async Task Handle_ReturnsTimeEntries_WithFilters()
        {
            var userId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var workItemId = Guid.NewGuid();

            using var db = TestDbContextFactory.CreateWithData(ctx =>
            {
                var user = new User { Id = userId, DisplayName = "Tester", Email = "t@example.com", PasswordHash = "h" };
                var workItem = new EpicWorkItem { Id = workItemId, ProjectId = projectId, Title = "W1" };
                ctx.Users.Add(user);
                ctx.WorkItems.Add(workItem);

                var te = new TimeEntry { Id = Guid.NewGuid(), WorkItem = workItem, WorkItemId = workItemId, User = user, UserId = userId, Hours = 2.5m, LoggedDate = DateTime.UtcNow.AddDays(-1), Description = "work", IsBillable = true };
                ctx.TimeEntries.Add(te);
            });

            var handler = new GetTimeEntriesHandler(db);
            var all = await handler.Handle(new GetTimeEntriesQuery(), CancellationToken.None);
            all.Should().HaveCount(1);

            var byWorkItem = await handler.Handle(new GetTimeEntriesQuery(WorkItemId: workItemId), CancellationToken.None);
            byWorkItem.Should().HaveCount(1);

            var byUser = await handler.Handle(new GetTimeEntriesQuery(UserId: userId), CancellationToken.None);
            byUser.Should().HaveCount(1);

            var byProject = await handler.Handle(new GetTimeEntriesQuery(ProjectId: projectId), CancellationToken.None);
            byProject.Should().HaveCount(1);

            var fromFilter = await handler.Handle(new GetTimeEntriesQuery(From: DateTime.UtcNow.AddDays(-2)), CancellationToken.None);
            fromFilter.Should().HaveCount(1);

            var toFilter = await handler.Handle(new GetTimeEntriesQuery(To: DateTime.UtcNow.AddHours(-12)), CancellationToken.None);
            toFilter.Should().HaveCount(1);
        }
    }
}
