using API.Controllers;
using Application.WorkItems.Commands;
using Application.Tasks.Commands;
using API.UnitTests.TestHelpers;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace API.UnitTests.Controllers;

public class WorkItemsControllerTests
{
    private readonly WorkItemsController _controller = new();

    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        var projectId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.WorkItems.Add(new EpicWorkItem { Id = Guid.NewGuid(), ProjectId = projectId, Title = "Epic1" });
            ctx.WorkItems.Add(new TaskWorkItem { Id = Guid.NewGuid(), ProjectId = projectId, Title = "Task1" });
        });

        var result = await _controller.GetAll(projectId, db);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetEpics_ReturnsOk()
    {
        var projectId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.WorkItems.Add(new EpicWorkItem { Id = Guid.NewGuid(), ProjectId = projectId, Title = "Epic" });
            ctx.WorkItems.Add(new TaskWorkItem { Id = Guid.NewGuid(), ProjectId = projectId, Title = "Task" });
        });

        var result = await _controller.GetEpics(projectId, db);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetUserStories_ReturnsOk()
    {
        var projectId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.WorkItems.Add(new UserStoryWorkItem { Id = Guid.NewGuid(), ProjectId = projectId, Title = "Story" });
        });

        var result = await _controller.GetUserStories(projectId, db);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetTasksForUserStory_ReturnsOk()
    {
        var projectId = Guid.NewGuid();
        var userStoryId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.WorkItems.Add(new TaskWorkItem { Id = Guid.NewGuid(), ProjectId = projectId, ParentId = userStoryId, Title = "Task" });
        });

        var result = await _controller.GetTasksForUserStory(projectId, userStoryId, db);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task CreateEpic_ReturnsOk()
    {
        var projectId = Guid.NewGuid();
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<CreateEpicCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateEpicResult(Guid.NewGuid()));
        var cmd = new CreateEpicCommand(Guid.Empty, "Epic", "Desc");

        var result = await _controller.CreateEpic(projectId, cmd, mediator.Object);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task CreateUserStory_ReturnsOk()
    {
        var projectId = Guid.NewGuid();
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<CreateUserStoryCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateUserStoryResult(Guid.NewGuid()));
        var cmd = new CreateUserStoryCommand(Guid.Empty, "Story", "Desc");

        var result = await _controller.CreateUserStory(projectId, cmd, mediator.Object);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task CreateTaskForUserStory_ReturnsOk()
    {
        var projectId = Guid.NewGuid();
        var userStoryId = Guid.NewGuid();
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<CreateTaskCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateTaskResult(Guid.NewGuid()));
        var cmd = new CreateTaskCommand(Guid.Empty, "Task", "Desc");

        var result = await _controller.CreateTaskForUserStory(projectId, userStoryId, cmd, mediator.Object);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_Found_ReturnsOk()
    {
        var projectId = Guid.NewGuid();
        var workItemId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.WorkItems.Add(new TaskWorkItem { Id = workItemId, ProjectId = projectId, Title = "Task" });
        });

        var result = await _controller.GetById(projectId, workItemId, db);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_NotFound_ReturnsNotFound()
    {
        using var db = TestDbContextFactory.Create();

        var result = await _controller.GetById(Guid.NewGuid(), Guid.NewGuid(), db);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetChildren_ReturnsOk()
    {
        var projectId = Guid.NewGuid();
        var parentId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.WorkItems.Add(new TaskWorkItem { Id = Guid.NewGuid(), ProjectId = projectId, ParentId = parentId, Title = "Child" });
        });

        var result = await _controller.GetChildren(projectId, parentId, db);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetBugs_ReturnsOk()
    {
        var projectId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.WorkItems.Add(new BugWorkItem { Id = Guid.NewGuid(), ProjectId = projectId, Title = "Bug", Severity = BugSeverity.High });
        });

        var result = await _controller.GetBugs(projectId, db);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task CreateBug_ValidITProject_ReturnsOk()
    {
        var projectId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Projects.Add(new Project { Id = projectId, Name = "IT Project", DomainType = DomainType.IT });
        });
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<CreateBugCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateBugResult(Guid.NewGuid()));
        var cmd = new CreateBugCommand(Guid.Empty, "Bug Title");

        var result = await _controller.CreateBug(projectId, cmd, mediator.Object, db);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task CreateBug_TechnologyProject_ReturnsOk()
    {
        var projectId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Projects.Add(new Project { Id = projectId, Name = "Tech Project", DomainType = DomainType.Technology });
        });
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<CreateBugCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateBugResult(Guid.NewGuid()));
        var cmd = new CreateBugCommand(Guid.Empty, "Bug Title");

        var result = await _controller.CreateBug(projectId, cmd, mediator.Object, db);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task CreateBug_NonITProject_ReturnsBadRequest()
    {
        var projectId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Projects.Add(new Project { Id = projectId, Name = "Healthcare Project", DomainType = DomainType.Healthcare });
        });
        var mediator = new Mock<IMediator>();
        var cmd = new CreateBugCommand(Guid.Empty, "Bug Title");

        var result = await _controller.CreateBug(projectId, cmd, mediator.Object, db);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CreateBug_ProjectNotFound_ReturnsNotFound()
    {
        using var db = TestDbContextFactory.Create();
        var mediator = new Mock<IMediator>();
        var cmd = new CreateBugCommand(Guid.Empty, "Bug Title");

        var result = await _controller.CreateBug(Guid.NewGuid(), cmd, mediator.Object, db);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Delete_Existing_ReturnsNoContent()
    {
        var projectId = Guid.NewGuid();
        var workItemId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.WorkItems.Add(new TaskWorkItem { Id = workItemId, ProjectId = projectId, Title = "Task" });
        });

        var result = await _controller.Delete(projectId, workItemId, db);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_NotFound_ReturnsNotFound()
    {
        using var db = TestDbContextFactory.Create();

        var result = await _controller.Delete(Guid.NewGuid(), Guid.NewGuid(), db);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Delete_DifferentProject_ReturnsNotFound()
    {
        var workItemId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.WorkItems.Add(new TaskWorkItem { Id = workItemId, ProjectId = Guid.NewGuid(), Title = "Task" });
        });

        var result = await _controller.Delete(Guid.NewGuid(), workItemId, db);

        result.Should().BeOfType<NotFoundResult>();
    }
}
