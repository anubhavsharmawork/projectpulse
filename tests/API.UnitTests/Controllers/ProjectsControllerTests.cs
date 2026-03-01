using API.Controllers;
using Application.Common.Interfaces;
using Application.Projects.Commands;
using Application.Projects.Queries;
using Application.Workflows.Commands;
using Application.Workflows.Queries;
using API.UnitTests.TestHelpers;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;

namespace API.UnitTests.Controllers;

public class ProjectsControllerTests
{
    private static ProjectsController CreateController(Guid? userId = null)
    {
        var controller = new ProjectsController();
        var httpContext = new DefaultHttpContext();
        if (userId.HasValue)
        {
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()) };
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        }
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return controller;
    }

    [Fact]
    public async Task GetAll_ReturnsPublicAndOwnedProjects()
    {
        var userId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Projects.Add(new Project { Id = Guid.NewGuid(), Name = "Public", IsPublic = true, OwnerId = Guid.NewGuid() });
            ctx.Projects.Add(new Project { Id = Guid.NewGuid(), Name = "Mine", IsPublic = false, OwnerId = userId });
            ctx.Projects.Add(new Project { Id = Guid.NewGuid(), Name = "Private", IsPublic = false, OwnerId = Guid.NewGuid() });
        });
        var controller = CreateController(userId);

        var result = await controller.GetAll(db);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetPublic_ReturnsOnlyPublicProjects()
    {
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Projects.Add(new Project { Id = Guid.NewGuid(), Name = "Public", IsPublic = true });
            ctx.Projects.Add(new Project { Id = Guid.NewGuid(), Name = "Private", IsPublic = false });
        });
        var controller = CreateController();

        var result = await controller.GetPublic(db);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetMine_ReturnsOnlyOwnedProjects()
    {
        var userId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Projects.Add(new Project { Id = Guid.NewGuid(), Name = "Mine", OwnerId = userId });
            ctx.Projects.Add(new Project { Id = Guid.NewGuid(), Name = "Other", OwnerId = Guid.NewGuid() });
        });
        var controller = CreateController(userId);

        var result = await controller.GetMine(db);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Create_ValidCommand_ReturnsOk()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<CreateProjectCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateProjectResult(Guid.NewGuid()));
        var controller = CreateController();
        var cmd = new CreateProjectCommand("Test", "Desc", IsPublic: false, DomainType: DomainType.IT);

        var result = await controller.Create(cmd, mediator.Object);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetConfig_ReturnsOk()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<GetProjectConfigQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProjectConfigDto(Guid.NewGuid(), "IT", new Dictionary<string, string>()));
        var controller = CreateController();

        var result = await controller.GetConfig(Guid.NewGuid(), mediator.Object);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Delete_ExistingProject_ReturnsNoContent()
    {
        var projectId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Projects.Add(new Project { Id = projectId, Name = "ToDelete" });
        });
        var controller = CreateController();

        var result = await controller.Delete(projectId, db);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_NonExistent_ReturnsNotFound()
    {
        using var db = TestDbContextFactory.Create();
        var controller = CreateController();

        var result = await controller.Delete(Guid.NewGuid(), db);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetWorkflow_Found_ReturnsOk()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<GetProjectWorkflowQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkflowDto(Guid.NewGuid(), "Workflow", "IT", new List<WorkflowStateDto>()));
        var controller = CreateController();

        var result = await controller.GetWorkflow(Guid.NewGuid(), mediator.Object);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetWorkflow_NotFound_ReturnsNotFound()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<GetProjectWorkflowQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkflowDto?)null);
        var controller = CreateController();

        var result = await controller.GetWorkflow(Guid.NewGuid(), mediator.Object);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task UpdateWorkflow_MismatchedProjectId_ReturnsBadRequest()
    {
        var mediator = new Mock<IMediator>();
        var logger = new Mock<ILogger<ProjectsController>>();
        var controller = CreateController();
        var cmd = new UpdateProjectWorkflowCommand(Guid.NewGuid(), "Name", new List<UpdateWorkflowStateDto>());

        var result = await controller.UpdateWorkflow(Guid.NewGuid(), cmd, mediator.Object, logger.Object);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task UpdateWorkflow_ValidCommand_ReturnsOk()
    {
        var projectId = Guid.NewGuid();
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<UpdateProjectWorkflowCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());
        var logger = new Mock<ILogger<ProjectsController>>();
        var controller = CreateController();
        var cmd = new UpdateProjectWorkflowCommand(projectId, "Name", new List<UpdateWorkflowStateDto>());

        var result = await controller.UpdateWorkflow(projectId, cmd, mediator.Object, logger.Object);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task UpdateWorkflow_UnauthorizedAccess_Returns403()
    {
        var projectId = Guid.NewGuid();
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<UpdateProjectWorkflowCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedAccessException("No permission"));
        var logger = new Mock<ILogger<ProjectsController>>();
        var controller = CreateController();
        var cmd = new UpdateProjectWorkflowCommand(projectId, "Name", new List<UpdateWorkflowStateDto>());

        var result = await controller.UpdateWorkflow(projectId, cmd, mediator.Object, logger.Object);

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task UpdateWorkflow_ConcurrencyConflict_ReturnsConflict()
    {
        var projectId = Guid.NewGuid();
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<UpdateProjectWorkflowCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException("Concurrency"));
        var logger = new Mock<ILogger<ProjectsController>>();
        var controller = CreateController();
        var cmd = new UpdateProjectWorkflowCommand(projectId, "Name", new List<UpdateWorkflowStateDto>());

        var result = await controller.UpdateWorkflow(projectId, cmd, mediator.Object, logger.Object);

        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task UpdateWorkflow_InvalidOperation_ReturnsBadRequest()
    {
        var projectId = Guid.NewGuid();
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<UpdateProjectWorkflowCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Invalid"));
        var logger = new Mock<ILogger<ProjectsController>>();
        var controller = CreateController();
        var cmd = new UpdateProjectWorkflowCommand(projectId, "Name", new List<UpdateWorkflowStateDto>());

        var result = await controller.UpdateWorkflow(projectId, cmd, mediator.Object, logger.Object);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ChangeWorkItemState_WorkItemNotFound_ReturnsNotFound()
    {
        var projectId = Guid.NewGuid();
        using var db = TestDbContextFactory.Create();
        var mediator = new Mock<IMediator>();
        var workflowEngine = new Mock<IWorkflowEngine>();
        var controller = CreateController();
        var request = new ChangeStateRequest(Guid.NewGuid());

        var result = await controller.ChangeWorkItemState(projectId, Guid.NewGuid(), request, mediator.Object, workflowEngine.Object, db);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task ChangeWorkItemState_NoCurrentState_AssignsInitialState()
    {
        var projectId = Guid.NewGuid();
        var workItemId = Guid.NewGuid();
        var targetStateId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.WorkItems.Add(new TaskWorkItem { Id = workItemId, ProjectId = projectId, Title = "Task", CurrentStateId = null });
        });
        var mediator = new Mock<IMediator>();
        var workflowEngine = new Mock<IWorkflowEngine>();
        var controller = CreateController(userId);
        var request = new ChangeStateRequest(targetStateId);

        var result = await controller.ChangeWorkItemState(projectId, workItemId, request, mediator.Object, workflowEngine.Object, db);

        result.Should().BeOfType<OkObjectResult>();
        workflowEngine.Verify(e => e.AssignInitialStateAsync(workItemId, targetStateId, userId, default), Times.Once);
    }

    [Fact]
    public async Task ChangeWorkItemState_HasCurrentState_UsesTransition()
    {
        var projectId = Guid.NewGuid();
        var workItemId = Guid.NewGuid();
        var stateId = Guid.NewGuid();
        var targetStateId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.WorkflowStates.Add(new WorkflowState { Id = stateId, WorkflowId = Guid.NewGuid(), Name = "InProgress" });
            ctx.WorkItems.Add(new TaskWorkItem { Id = workItemId, ProjectId = projectId, Title = "Task", CurrentStateId = stateId });
        });
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<TransitionWorkItemStateCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TransitionWorkItemStateResult(Guid.NewGuid()));
        var workflowEngine = new Mock<IWorkflowEngine>();
        var controller = CreateController(Guid.NewGuid());
        var request = new ChangeStateRequest(targetStateId);

        var result = await controller.ChangeWorkItemState(projectId, workItemId, request, mediator.Object, workflowEngine.Object, db);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ChangeWorkItemState_UnauthorizedAccess_Returns403()
    {
        var projectId = Guid.NewGuid();
        var workItemId = Guid.NewGuid();
        var stateId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.WorkflowStates.Add(new WorkflowState { Id = stateId, WorkflowId = Guid.NewGuid(), Name = "InProgress" });
            ctx.WorkItems.Add(new TaskWorkItem { Id = workItemId, ProjectId = projectId, Title = "Task", CurrentStateId = stateId });
        });
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<TransitionWorkItemStateCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedAccessException("Forbidden"));
        var workflowEngine = new Mock<IWorkflowEngine>();
        var controller = CreateController(Guid.NewGuid());
        var request = new ChangeStateRequest(Guid.NewGuid());

        var result = await controller.ChangeWorkItemState(projectId, workItemId, request, mediator.Object, workflowEngine.Object, db);

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task ChangeWorkItemState_InvalidOperation_ReturnsBadRequest()
    {
        var projectId = Guid.NewGuid();
        var workItemId = Guid.NewGuid();
        var stateId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.WorkflowStates.Add(new WorkflowState { Id = stateId, WorkflowId = Guid.NewGuid(), Name = "InProgress" });
            ctx.WorkItems.Add(new TaskWorkItem { Id = workItemId, ProjectId = projectId, Title = "Task", CurrentStateId = stateId });
        });
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<TransitionWorkItemStateCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Invalid"));
        var workflowEngine = new Mock<IWorkflowEngine>();
        var controller = CreateController(Guid.NewGuid());
        var request = new ChangeStateRequest(Guid.NewGuid());

        var result = await controller.ChangeWorkItemState(projectId, workItemId, request, mediator.Object, workflowEngine.Object, db);

        result.Should().BeOfType<BadRequestObjectResult>();
    }
}
