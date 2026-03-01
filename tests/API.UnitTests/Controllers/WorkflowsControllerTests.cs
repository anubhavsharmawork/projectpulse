using API.Controllers;
using Application.Common.Interfaces;
using Application.Workflows.Commands;
using Application.Workflows.Queries;
using Domain.Enums;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace API.UnitTests.Controllers;

public class WorkflowsControllerTests
{
    private readonly Mock<IMediator> _mediator = new();
    private readonly Mock<ILogger<WorkflowsController>> _logger = new();
    private readonly WorkflowsController _controller;

    public WorkflowsControllerTests()
    {
        _controller = new WorkflowsController(_mediator.Object, _logger.Object);
    }

    [Fact]
    public async Task GetDomains_Success_ReturnsOk()
    {
        _mediator.Setup(m => m.Send(It.IsAny<GetWorkflowDomainsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WorkflowDomainDto> { new("IT", true) });

        var result = await _controller.GetDomains();

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetDomains_Exception_Returns500()
    {
        _mediator.Setup(m => m.Send(It.IsAny<GetWorkflowDomainsQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DB error"));

        var result = await _controller.GetDomains();

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task GetByDomain_ValidDomain_ReturnsOk()
    {
        _mediator.Setup(m => m.Send(It.IsAny<GetWorkflowByDomainQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkflowDto(Guid.NewGuid(), "IT Workflow", "IT", new List<WorkflowStateDto>()));

        var result = await _controller.GetByDomain("IT");

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetByDomain_InvalidDomain_ReturnsBadRequest()
    {
        var result = await _controller.GetByDomain("InvalidDomain");

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetByDomain_NotFound_ReturnsNotFound()
    {
        _mediator.Setup(m => m.Send(It.IsAny<GetWorkflowByDomainQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkflowDto?)null);

        var result = await _controller.GetByDomain("IT");

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetByDomain_Exception_Returns500()
    {
        _mediator.Setup(m => m.Send(It.IsAny<GetWorkflowByDomainQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DB error"));

        var result = await _controller.GetByDomain("IT");

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task GetAvailableTransitions_Success_ReturnsOk()
    {
        _mediator.Setup(m => m.Send(It.IsAny<GetAvailableTransitionsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AvailableTransitionDto>());

        var result = await _controller.GetAvailableTransitions(Guid.NewGuid());

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAvailableTransitions_Exception_Returns500()
    {
        _mediator.Setup(m => m.Send(It.IsAny<GetAvailableTransitionsQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DB error"));

        var result = await _controller.GetAvailableTransitions(Guid.NewGuid());

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task TransitionState_Success_ReturnsOk()
    {
        _mediator.Setup(m => m.Send(It.IsAny<TransitionWorkItemStateCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TransitionWorkItemStateResult(Guid.NewGuid()));
        var request = new TransitionRequest(Guid.NewGuid(), "Moving forward");

        var result = await _controller.TransitionState(Guid.NewGuid(), request);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task TransitionState_Unauthorized_Returns403()
    {
        _mediator.Setup(m => m.Send(It.IsAny<TransitionWorkItemStateCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedAccessException("Forbidden"));
        var request = new TransitionRequest(Guid.NewGuid());

        var result = await _controller.TransitionState(Guid.NewGuid(), request);

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task TransitionState_InvalidOperation_ReturnsBadRequest()
    {
        _mediator.Setup(m => m.Send(It.IsAny<TransitionWorkItemStateCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Invalid transition"));
        var request = new TransitionRequest(Guid.NewGuid());

        var result = await _controller.TransitionState(Guid.NewGuid(), request);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task TransitionState_Exception_Returns500()
    {
        _mediator.Setup(m => m.Send(It.IsAny<TransitionWorkItemStateCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DB error"));
        var request = new TransitionRequest(Guid.NewGuid());

        var result = await _controller.TransitionState(Guid.NewGuid(), request);

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(500);
    }
}
