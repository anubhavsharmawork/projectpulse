using API.Controllers;
using Application.Comments.Commands;
using API.UnitTests.TestHelpers;
using Domain.Entities;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace API.UnitTests.Controllers;

public class CommentsControllerTests
{
    private readonly CommentsController _controller;

    public CommentsControllerTests()
    {
        _controller = new CommentsController();
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithComments()
    {
        var workItemId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Comments.Add(new Comment { Id = Guid.NewGuid(), WorkItemId = workItemId, Body = "Comment 1" });
            ctx.Comments.Add(new Comment { Id = Guid.NewGuid(), WorkItemId = workItemId, Body = "Comment 2" });
            ctx.Comments.Add(new Comment { Id = Guid.NewGuid(), WorkItemId = Guid.NewGuid(), Body = "Other" });
        });

        var result = await _controller.GetAll(workItemId, db);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var items = ok.Value as IEnumerable<object>;
        items.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAll_NoComments_ReturnsEmptyList()
    {
        var workItemId = Guid.NewGuid();
        using var db = TestDbContextFactory.Create();

        var result = await _controller.GetAll(workItemId, db);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Create_ValidCommand_ReturnsOk()
    {
        var workItemId = Guid.NewGuid();
        var cmd = new CreateCommentCommand(Guid.Empty, "Test body");
        var mediatorMock = new Mock<IMediator>();
        mediatorMock.Setup(m => m.Send(It.IsAny<CreateCommentCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateCommentResult(Guid.NewGuid()));

        var result = await _controller.Create(workItemId, cmd, mediatorMock.Object);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Delete_ExistingComment_ReturnsNoContent()
    {
        var workItemId = Guid.NewGuid();
        var commentId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Comments.Add(new Comment { Id = commentId, WorkItemId = workItemId, Body = "To delete" });
        });

        var result = await _controller.Delete(workItemId, commentId, db);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_NonExistentComment_ReturnsNotFound()
    {
        var workItemId = Guid.NewGuid();
        using var db = TestDbContextFactory.Create();

        var result = await _controller.Delete(workItemId, Guid.NewGuid(), db);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Delete_CommentWithDifferentWorkItemId_ReturnsNotFound()
    {
        var workItemId = Guid.NewGuid();
        var commentId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Comments.Add(new Comment { Id = commentId, WorkItemId = Guid.NewGuid(), Body = "Different WI" });
        });

        var result = await _controller.Delete(workItemId, commentId, db);

        result.Should().BeOfType<NotFoundResult>();
    }
}
