using API.Controllers;
using Application.TimeTracking.Commands;
using Application.TimeTracking.Queries;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace API.UnitTests.Controllers;

public class TimeTrackingControllerTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly TimeTrackingController _controller;

    public TimeTrackingControllerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _controller = new TimeTrackingController(_mediatorMock.Object);
    }

    [Fact]
    public async Task LogTime_ValidCommand_ShouldReturnOk()
    {
        var timeEntryId = Guid.NewGuid();
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<LogTimeEntryCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(timeEntryId);
        var cmd = new LogTimeEntryCommand(Guid.NewGuid(), 2.5m, DateTime.UtcNow, "Worked on feature", true);

        var result = await _controller.LogTime(cmd);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task LogTime_UnauthorizedAccess_ShouldReturn403()
    {
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<LogTimeEntryCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedAccessException("Not allowed"));
        var cmd = new LogTimeEntryCommand(Guid.NewGuid(), 2.5m, DateTime.UtcNow, "test", false);

        var result = await _controller.LogTime(cmd);

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task LogTime_InvalidOperation_ShouldReturnBadRequest()
    {
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<LogTimeEntryCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Invalid work item"));
        var cmd = new LogTimeEntryCommand(Guid.NewGuid(), 2.5m, DateTime.UtcNow, "test", false);

        var result = await _controller.LogTime(cmd);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetTimeEntries_ShouldReturnOk()
    {
        var entries = new List<TimeEntryDto>();
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetTimeEntriesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(entries);

        var result = await _controller.GetTimeEntries(null, null, null, null, null);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetTimeEntries_WithFilters_ShouldReturnOk()
    {
        var entries = new List<TimeEntryDto>();
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetTimeEntriesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(entries);
        var workItemId = Guid.NewGuid();
        var from = DateTime.UtcNow.AddDays(-7);
        var to = DateTime.UtcNow;

        var result = await _controller.GetTimeEntries(workItemId, null, null, from, to);

        result.Should().BeOfType<OkObjectResult>();
    }
}
