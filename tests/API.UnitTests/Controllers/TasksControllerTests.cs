using API.Controllers;
using Application.Tasks.Commands;
using API.UnitTests.TestHelpers;
using Domain.Entities;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Moq;
using API.Hubs;
using Xunit;

namespace API.UnitTests.Controllers;

public class TasksControllerTests
{
    private readonly TasksController _controller = new();

    [Fact]
    public async Task GetAll_ReturnsTasks()
    {
        var projectId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.WorkItems.Add(new TaskWorkItem { Id = Guid.NewGuid(), ProjectId = projectId, Title = "Task1" });
            ctx.WorkItems.Add(new TaskWorkItem { Id = Guid.NewGuid(), ProjectId = projectId, Title = "Task2", ParentId = Guid.NewGuid() });
        });

        var result = await _controller.GetAll(projectId, false, db);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAll_OrphansOnly_ReturnsOrphanTasks()
    {
        var projectId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.WorkItems.Add(new TaskWorkItem { Id = Guid.NewGuid(), ProjectId = projectId, Title = "Orphan", ParentId = null });
            ctx.WorkItems.Add(new TaskWorkItem { Id = Guid.NewGuid(), ProjectId = projectId, Title = "Child", ParentId = Guid.NewGuid() });
        });

        var result = await _controller.GetAll(projectId, true, db);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Create_ValidCommand_ReturnsOk()
    {
        var projectId = Guid.NewGuid();
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<CreateTaskCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateTaskResult(Guid.NewGuid()));
        var cmd = new CreateTaskCommand(Guid.Empty, "Task", "Desc");

        var result = await _controller.Create(projectId, cmd, mediator.Object);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Complete_ExistingTask_ReturnsNoContent()
    {
        var projectId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.WorkItems.Add(new TaskWorkItem { Id = taskId, ProjectId = projectId, Title = "Task", IsCompleted = false });
        });
        var hubContext = CreateMockHubContext();

        var result = await _controller.Complete(projectId, taskId, db, hubContext);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Complete_AlreadyCompleted_ReturnsNoContent()
    {
        var projectId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.WorkItems.Add(new TaskWorkItem { Id = taskId, ProjectId = projectId, Title = "Task", IsCompleted = true, CompletedAt = DateTime.UtcNow });
        });
        var hubContext = CreateMockHubContext();

        var result = await _controller.Complete(projectId, taskId, db, hubContext);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Complete_NotFound_ReturnsNotFound()
    {
        var projectId = Guid.NewGuid();
        using var db = TestDbContextFactory.Create();
        var hubContext = CreateMockHubContext();

        var result = await _controller.Complete(projectId, Guid.NewGuid(), db, hubContext);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Delete_ExistingTask_ReturnsNoContent()
    {
        var projectId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.WorkItems.Add(new TaskWorkItem { Id = taskId, ProjectId = projectId, Title = "Task" });
        });

        var result = await _controller.Delete(projectId, taskId, db);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_NotFound_ReturnsNotFound()
    {
        var projectId = Guid.NewGuid();
        using var db = TestDbContextFactory.Create();

        var result = await _controller.Delete(projectId, Guid.NewGuid(), db);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Delete_DifferentProject_ReturnsNotFound()
    {
        var taskId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.WorkItems.Add(new TaskWorkItem { Id = taskId, ProjectId = Guid.NewGuid(), Title = "Task" });
        });

        var result = await _controller.Delete(Guid.NewGuid(), taskId, db);

        result.Should().BeOfType<NotFoundResult>();
    }

    private static IHubContext<ProjectHub> CreateMockHubContext()
    {
        var mockClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        mockClients.Setup(c => c.All).Returns(mockClientProxy.Object);
        var mock = new Mock<IHubContext<ProjectHub>>();
        mock.Setup(h => h.Clients).Returns(mockClients.Object);
        return mock.Object;
    }
}
