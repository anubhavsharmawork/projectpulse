using API.Controllers;
using API.UnitTests.TestHelpers;
using Domain.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Xunit;

namespace API.UnitTests.Controllers;

public class MentionNotificationsControllerTests
{
    private static MentionNotificationsController CreateController(Guid? userId = null)
    {
        var controller = new MentionNotificationsController();
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
    public async Task GetMyNotifications_ValidUser_ReturnsOk()
    {
        var userId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.MentionNotifications.Add(new MentionNotification
            {
                Id = Guid.NewGuid(), UserId = userId, CommentId = Guid.NewGuid(),
                WorkItemId = Guid.NewGuid(), MentionedByUserId = Guid.NewGuid(),
                CommentBody = "Body", WorkItemTitle = "Title", MentionedByName = "User"
            });
        });
        var controller = CreateController(userId);

        var result = await controller.GetMyNotifications(db);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetMyNotifications_NoUserId_ReturnsUnauthorized()
    {
        using var db = TestDbContextFactory.Create();
        var controller = CreateController();

        var result = await controller.GetMyNotifications(db);

        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task GetUnreadCount_ValidUser_ReturnsOk()
    {
        var userId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.MentionNotifications.Add(new MentionNotification
            {
                Id = Guid.NewGuid(), UserId = userId, CommentId = Guid.NewGuid(),
                WorkItemId = Guid.NewGuid(), MentionedByUserId = Guid.NewGuid(),
                CommentBody = "Body", WorkItemTitle = "Title", MentionedByName = "User",
                IsRead = false
            });
            ctx.MentionNotifications.Add(new MentionNotification
            {
                Id = Guid.NewGuid(), UserId = userId, CommentId = Guid.NewGuid(),
                WorkItemId = Guid.NewGuid(), MentionedByUserId = Guid.NewGuid(),
                CommentBody = "Body2", WorkItemTitle = "Title2", MentionedByName = "User2",
                IsRead = true
            });
        });
        var controller = CreateController(userId);

        var result = await controller.GetUnreadCount(db);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetUnreadCount_NoUserId_ReturnsUnauthorized()
    {
        using var db = TestDbContextFactory.Create();
        var controller = CreateController();

        var result = await controller.GetUnreadCount(db);

        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task MarkAsRead_ExistingNotification_ReturnsNoContent()
    {
        var userId = Guid.NewGuid();
        var notifId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.MentionNotifications.Add(new MentionNotification
            {
                Id = notifId, UserId = userId, CommentId = Guid.NewGuid(),
                WorkItemId = Guid.NewGuid(), MentionedByUserId = Guid.NewGuid(),
                CommentBody = "Body", WorkItemTitle = "Title", MentionedByName = "User",
                IsRead = false
            });
        });
        var controller = CreateController(userId);

        var result = await controller.MarkAsRead(notifId, db);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task MarkAsRead_NotFound_ReturnsNotFound()
    {
        var userId = Guid.NewGuid();
        using var db = TestDbContextFactory.Create();
        var controller = CreateController(userId);

        var result = await controller.MarkAsRead(Guid.NewGuid(), db);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task MarkAsRead_NoUserId_ReturnsUnauthorized()
    {
        using var db = TestDbContextFactory.Create();
        var controller = CreateController();

        var result = await controller.MarkAsRead(Guid.NewGuid(), db);

        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task MarkAsRead_DifferentUser_ReturnsNotFound()
    {
        var userId = Guid.NewGuid();
        var notifId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.MentionNotifications.Add(new MentionNotification
            {
                Id = notifId, UserId = Guid.NewGuid(), CommentId = Guid.NewGuid(),
                WorkItemId = Guid.NewGuid(), MentionedByUserId = Guid.NewGuid(),
                CommentBody = "Body", WorkItemTitle = "Title", MentionedByName = "User"
            });
        });
        var controller = CreateController(userId);

        var result = await controller.MarkAsRead(notifId, db);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task MarkAllAsRead_ValidUser_ReturnsNoContent()
    {
        var userId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.MentionNotifications.Add(new MentionNotification
            {
                Id = Guid.NewGuid(), UserId = userId, CommentId = Guid.NewGuid(),
                WorkItemId = Guid.NewGuid(), MentionedByUserId = Guid.NewGuid(),
                CommentBody = "Body1", WorkItemTitle = "Title1", MentionedByName = "User1",
                IsRead = false
            });
            ctx.MentionNotifications.Add(new MentionNotification
            {
                Id = Guid.NewGuid(), UserId = userId, CommentId = Guid.NewGuid(),
                WorkItemId = Guid.NewGuid(), MentionedByUserId = Guid.NewGuid(),
                CommentBody = "Body2", WorkItemTitle = "Title2", MentionedByName = "User2",
                IsRead = false
            });
        });
        var controller = CreateController(userId);

        var result = await controller.MarkAllAsRead(db);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task MarkAllAsRead_NoUserId_ReturnsUnauthorized()
    {
        using var db = TestDbContextFactory.Create();
        var controller = CreateController();

        var result = await controller.MarkAllAsRead(db);

        result.Should().BeOfType<UnauthorizedResult>();
    }
}
