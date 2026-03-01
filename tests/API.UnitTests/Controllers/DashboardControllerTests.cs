using API.Controllers;
using Application.Dashboard.Queries;
using Application.Budget.Queries;
using Domain.Enums;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace API.UnitTests.Controllers;

public class DashboardControllerTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly DashboardController _controller;

    public DashboardControllerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _controller = new DashboardController(_mediatorMock.Object);
    }

    [Fact]
    public async Task GetMetrics_NoDomainType_ShouldReturnOk()
    {
        var common = new CommonKpis(10, 5, 50m, 2, 75m, new Dictionary<Guid, int>());
        var metrics = new DashboardResult(common, null, null, null, null);
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetMetricsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(metrics);

        var result = await _controller.GetMetrics(null);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(metrics);
    }

    [Fact]
    public async Task GetMetrics_ValidDomainType_ShouldPassParsedValue()
    {
        var common = new CommonKpis(5, 2, 40m, 1, 60m, new Dictionary<Guid, int>());
        var metrics = new DashboardResult(common, null, null, null, null);
        _mediatorMock
            .Setup(m => m.Send(It.Is<GetMetricsQuery>(q => q.DomainType == DomainType.IT), It.IsAny<CancellationToken>()))
            .ReturnsAsync(metrics);

        var result = await _controller.GetMetrics("IT");

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetMetrics_InvalidDomainType_ShouldPassNull()
    {
        var common = new CommonKpis(10, 5, 50m, 2, 75m, new Dictionary<Guid, int>());
        var metrics = new DashboardResult(common, null, null, null, null);
        _mediatorMock
            .Setup(m => m.Send(It.Is<GetMetricsQuery>(q => q.DomainType == null), It.IsAny<CancellationToken>()))
            .ReturnsAsync(metrics);

        var result = await _controller.GetMetrics("InvalidDomain");

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetMetrics_EmptyString_ShouldPassNull()
    {
        var common = new CommonKpis(10, 5, 50m, 2, 75m, new Dictionary<Guid, int>());
        var metrics = new DashboardResult(common, null, null, null, null);
        _mediatorMock
            .Setup(m => m.Send(It.Is<GetMetricsQuery>(q => q.DomainType == null), It.IsAny<CancellationToken>()))
            .ReturnsAsync(metrics);

        var result = await _controller.GetMetrics("");

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetBudgetStatus_ShouldReturnOk()
    {
        var budgets = new List<ProjectBudgetDto>
        {
            new(Guid.NewGuid(), "Project A", "IT", 100000m, 75000m, 25000m, 25m, 5, 80000m, 60000m)
        };
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetBudgetStatusQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(budgets);

        var result = await _controller.GetBudgetStatus();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(budgets);
    }
}
