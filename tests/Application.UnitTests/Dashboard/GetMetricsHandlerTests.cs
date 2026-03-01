using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.Dashboard.Queries;
using Application.UnitTests.TestHelpers;
using FluentAssertions;
using Xunit;

namespace Application.UnitTests.Dashboard
{
    public class GetMetricsHandlerTests
    {
        [Fact]
        public async Task Handle_EmptyDb_ShouldReturnZeroCommonKpis()
        {
            // Arrange
            using var db = TestDbContextFactory.Create();
            var handler = new GetMetricsHandler(db);
            var query = new GetMetricsQuery();

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Common.Should().NotBeNull();
            result.Common.TotalTasks.Should().Be(0);
            result.Common.CompletedTasks.Should().Be(0);
            result.Common.CompletionRate.Should().Be(0);
            result.Common.OverdueItems.Should().Be(0);
            result.Common.TasksPerUser.Should().BeEmpty();
        }

        [Fact]
        public async Task Handle_MultipleCalls_ShouldReturnConsistentResults()
        {
            // Arrange
            using var db = TestDbContextFactory.Create();
            var handler = new GetMetricsHandler(db);

            // Act
            var result1 = await handler.Handle(new GetMetricsQuery(), CancellationToken.None);
            var result2 = await handler.Handle(new GetMetricsQuery(), CancellationToken.None);

            // Assert
            result1.Common.TotalTasks.Should().Be(result2.Common.TotalTasks);
            result1.Common.CompletedTasks.Should().Be(result2.Common.CompletedTasks);
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
        public async Task Handle_WithDomainType_ShouldReturnDomainSpecificKpis()
        {
            // Arrange
            using var db = TestDbContextFactory.Create();
            var handler = new GetMetricsHandler(db);

            // Act
            var result = await handler.Handle(
                new GetMetricsQuery(global::Domain.Enums.DomainType.IT), CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IT.Should().NotBeNull();
            result.IT!.VelocityTrend.Should().HaveCount(4);
        }

        [Fact]
        public async Task Handle_HealthcareDomain_ShouldReturnHealthcareKpis()
        {
            // Arrange
            using var db = TestDbContextFactory.Create();
            var handler = new GetMetricsHandler(db);

            // Act
            var result = await handler.Handle(
                new GetMetricsQuery(global::Domain.Enums.DomainType.Healthcare), CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Healthcare.Should().NotBeNull();
            result.Healthcare!.ComplianceStatus.Should().NotBeNull();
        }

        [Fact]
        public async Task Handle_NoDomainType_ShouldReturnAllDomainKpis()
        {
            // Arrange
            using var db = TestDbContextFactory.Create();
            var handler = new GetMetricsHandler(db);

            // Act
            var result = await handler.Handle(new GetMetricsQuery(), CancellationToken.None);

            // Assert
            result.IT.Should().NotBeNull();
            result.Healthcare.Should().NotBeNull();
            result.Construction.Should().NotBeNull();
            result.Infrastructure.Should().NotBeNull();
        }
    }
}
