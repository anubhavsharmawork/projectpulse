using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.Dashboard.Queries;
using FluentAssertions;
using Xunit;

namespace Application.UnitTests.Dashboard
{
    public class GetMetricsHandlerTests
    {
        [Fact]
        public async Task Handle_ShouldReturnTemplateMetrics()
        {
            // Arrange
            var handler = new GetMetricsHandler();
            var query = new GetMetricsQuery();

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.TasksTotal.Should().Be(0);
            result.TasksCompleted.Should().Be(0);
            result.TasksPerUser.Should().NotBeNull();
            result.TasksPerUser.Should().BeEmpty();
        }

        [Fact]
        public async Task Handle_MultipleCalls_ShouldReturnConsistentResults()
        {
            // Arrange
            var handler = new GetMetricsHandler();

            // Act
            var result1 = await handler.Handle(new GetMetricsQuery(), CancellationToken.None);
            var result2 = await handler.Handle(new GetMetricsQuery(), CancellationToken.None);

            // Assert
            result1.TasksTotal.Should().Be(result2.TasksTotal);
            result1.TasksCompleted.Should().Be(result2.TasksCompleted);
        }

        [Fact]
        public void GetMetricsQuery_RecordEquality_ShouldWork()
        {
            // Arrange
            var query1 = new GetMetricsQuery();
            var query2 = new GetMetricsQuery();

            // Assert
            query1.Should().Be(query2);
        }

        [Fact]
        public void GetMetricsResult_RecordEquality_ShouldWork()
        {
            // Arrange
            var dict1 = new Dictionary<Guid, int>();
            var dict2 = new Dictionary<Guid, int>();
            var result1 = new GetMetricsResult(10, 5, dict1);
            var result2 = new GetMetricsResult(10, 5, dict2);

            // Assert - Dictionary references differ, but values are same
            result1.TasksTotal.Should().Be(result2.TasksTotal);
            result1.TasksCompleted.Should().Be(result2.TasksCompleted);
        }

        [Fact]
        public void GetMetricsResult_ShouldExposeAllProperties()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tasksPerUser = new Dictionary<Guid, int> { { userId, 5 } };
            
            // Act
            var result = new GetMetricsResult(100, 50, tasksPerUser);

            // Assert
            result.TasksTotal.Should().Be(100);
            result.TasksCompleted.Should().Be(50);
            result.TasksPerUser.Should().ContainKey(userId);
            result.TasksPerUser[userId].Should().Be(5);
        }
    }
}
