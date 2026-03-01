using API.Controllers;
using Application.Audit.Queries;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace API.UnitTests.Controllers;

public class AuditControllerTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly AuditController _controller;

    public AuditControllerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _controller = new AuditController(_mediatorMock.Object);
    }

    [Fact]
    public async Task GetLogs_NoFilters_ShouldReturnOk()
    {
        var logs = new List<AuditLogDto>();
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetAuditLogsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(logs);

        var result = await _controller.GetLogs(null, null, null, null, null, 100);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetLogs_WithFilters_ShouldReturnOk()
    {
        var logs = new List<AuditLogDto>();
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetAuditLogsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(logs);

        var result = await _controller.GetLogs(
            "Project",
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow.AddDays(-30),
            DateTime.UtcNow,
            50);

        result.Should().BeOfType<OkObjectResult>();
    }
}
