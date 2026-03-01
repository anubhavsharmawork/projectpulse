using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Application.Workflows.Commands;
using Application.Workflows.Queries;
using Application.UnitTests.TestHelpers;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace Application.UnitTests.Workflows;

public class GetWorkflowDomainsHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsAllDomainsWithAvailability()
    {
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Workflows.Add(new Workflow { Id = Guid.NewGuid(), Name = "IT Workflow", DomainType = DomainType.IT });
        });
        var handler = new GetWorkflowDomainsHandler(db);

        var result = await handler.Handle(new GetWorkflowDomainsQuery(), CancellationToken.None);

        result.Should().NotBeEmpty();
        result.Should().Contain(d => d.DomainType == "IT" && d.HasDefault);
        result.Should().Contain(d => d.DomainType == "Healthcare" && !d.HasDefault);
    }
}

public class GetWorkflowByDomainHandlerTests
{
    [Fact]
    public async Task Handle_WorkflowExists_ReturnsDto()
    {
        var workflowId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Workflows.Add(new Workflow
            {
                Id = workflowId, Name = "IT Workflow", DomainType = DomainType.IT,
                States = new List<WorkflowState>
                {
                    new() { Id = Guid.NewGuid(), WorkflowId = workflowId, Name = "Open", Order = 1, Color = "#000", IsInitial = true },
                    new() { Id = Guid.NewGuid(), WorkflowId = workflowId, Name = "Done", Order = 2, Color = "#0F0", IsFinal = true }
                }
            });
        });
        var handler = new GetWorkflowByDomainHandler(db);

        var result = await handler.Handle(new GetWorkflowByDomainQuery(DomainType.IT), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Name.Should().Be("IT Workflow");
        result.States.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_NoWorkflow_ReturnsNull()
    {
        using var db = TestDbContextFactory.Create();
        var handler = new GetWorkflowByDomainHandler(db);

        var result = await handler.Handle(new GetWorkflowByDomainQuery(DomainType.Healthcare), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_StateWithJsonFields_ParsesCorrectly()
    {
        var workflowId = Guid.NewGuid();
        var stateId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Workflows.Add(new Workflow
            {
                Id = workflowId, Name = "Workflow", DomainType = DomainType.IT,
                States = new List<WorkflowState>
                {
                    new()
                    {
                        Id = stateId, WorkflowId = workflowId, Name = "Review", Order = 1, Color = "#00F",
                        AllowedTransitions = $"[\"{Guid.NewGuid()}\"]",
                        RequiredFields = "[\"description\",\"assignee\"]",
                        NotifyOnEntry = true
                    }
                }
            });
        });
        var handler = new GetWorkflowByDomainHandler(db);

        var result = await handler.Handle(new GetWorkflowByDomainQuery(DomainType.IT), CancellationToken.None);

        result!.States[0].RequiredFields.Should().Contain("description");
        result.States[0].NotifyOnEntry.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_InvalidJsonInFields_ReturnsEmptyLists()
    {
        var workflowId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Workflows.Add(new Workflow
            {
                Id = workflowId, Name = "Workflow", DomainType = DomainType.IT,
                States = new List<WorkflowState>
                {
                    new() { Id = Guid.NewGuid(), WorkflowId = workflowId, Name = "State", Order = 1, Color = "#000", AllowedTransitions = "invalid-json", RequiredFields = "not-json" }
                }
            });
        });
        var handler = new GetWorkflowByDomainHandler(db);

        var result = await handler.Handle(new GetWorkflowByDomainQuery(DomainType.IT), CancellationToken.None);

        result!.States[0].AllowedTransitions.Should().BeEmpty();
        result.States[0].RequiredFields.Should().BeEmpty();
    }
}

public class GetProjectWorkflowHandlerTests
{
    [Fact]
    public async Task Handle_ProjectNotFound_ReturnsNull()
    {
        using var db = TestDbContextFactory.Create();
        var handler = new GetProjectWorkflowHandler(db);

        var result = await handler.Handle(new GetProjectWorkflowQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ProjectWithOverrideWorkflow_ReturnsProjectWorkflow()
    {
        var projectId = Guid.NewGuid();
        var workflowId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Workflows.Add(new Workflow
            {
                Id = workflowId, Name = "Custom", DomainType = DomainType.IT,
                States = new List<WorkflowState> { new() { Id = Guid.NewGuid(), WorkflowId = workflowId, Name = "Open", Order = 1, Color = "#000" } }
            });
            ctx.Projects.Add(new Project { Id = projectId, Name = "Project", DomainType = DomainType.IT, WorkflowId = workflowId });
        });
        var handler = new GetProjectWorkflowHandler(db);

        var result = await handler.Handle(new GetProjectWorkflowQuery(projectId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Custom");
    }

    [Fact]
    public async Task Handle_ProjectWithoutWorkflow_FallsBackToDomain()
    {
        var projectId = Guid.NewGuid();
        var workflowId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Workflows.Add(new Workflow
            {
                Id = workflowId, Name = "IT Default", DomainType = DomainType.IT,
                States = new List<WorkflowState> { new() { Id = Guid.NewGuid(), WorkflowId = workflowId, Name = "Open", Order = 1, Color = "#000" } }
            });
            ctx.Projects.Add(new Project { Id = projectId, Name = "Project", DomainType = DomainType.IT, WorkflowId = null });
        });
        var handler = new GetProjectWorkflowHandler(db);

        var result = await handler.Handle(new GetProjectWorkflowQuery(projectId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Name.Should().Be("IT Default");
    }

    [Fact]
    public async Task Handle_NoDomainWorkflow_ReturnsNull()
    {
        var projectId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Projects.Add(new Project { Id = projectId, Name = "Project", DomainType = DomainType.Healthcare, WorkflowId = null });
        });
        var handler = new GetProjectWorkflowHandler(db);

        var result = await handler.Handle(new GetProjectWorkflowQuery(projectId), CancellationToken.None);

        result.Should().BeNull();
    }
}

public class GetAvailableTransitionsHandlerTests
{
    [Fact]
    public async Task Handle_DelegatesToEngine()
    {
        var workItemId = Guid.NewGuid();
        var engineMock = new Mock<IWorkflowEngine>();
        engineMock.Setup(e => e.GetAvailableTransitionsAsync(workItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AvailableTransitionDto>
            {
                new(Guid.NewGuid(), "InProgress", "#00F", false, new List<string>())
            });
        var handler = new GetAvailableTransitionsHandler(engineMock.Object);

        var result = await handler.Handle(new GetAvailableTransitionsQuery(workItemId), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].StateName.Should().Be("InProgress");
    }
}

public class TransitionWorkItemStateHandlerTests
{
    private static Mock<IHttpContextAccessor> CreateHttpAccessor(Guid? userId = null)
    {
        var mock = new Mock<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext();
        if (userId.HasValue)
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()) }, "Test"));
        mock.Setup(x => x.HttpContext).Returns(httpContext);
        return mock;
    }

    [Fact]
    public async Task Handle_ValidTransition_ReturnsResult()
    {
        var userId = Guid.NewGuid();
        var transitionId = Guid.NewGuid();
        var engineMock = new Mock<IWorkflowEngine>();
        engineMock.Setup(e => e.TransitionAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), userId, "comment", It.IsAny<CancellationToken>()))
            .ReturnsAsync(transitionId);
        var handler = new TransitionWorkItemStateHandler(engineMock.Object, CreateHttpAccessor(userId).Object);

        var result = await handler.Handle(new TransitionWorkItemStateCommand(Guid.NewGuid(), Guid.NewGuid(), "comment"), CancellationToken.None);

        result.TransitionId.Should().Be(transitionId);
    }

    [Fact]
    public async Task Handle_NoUserClaim_UsesEmptyGuid()
    {
        var engineMock = new Mock<IWorkflowEngine>();
        engineMock.Setup(e => e.TransitionAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), Guid.Empty, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());
        var handler = new TransitionWorkItemStateHandler(engineMock.Object, CreateHttpAccessor().Object);

        var result = await handler.Handle(new TransitionWorkItemStateCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        result.TransitionId.Should().NotBe(Guid.Empty);
    }
}
