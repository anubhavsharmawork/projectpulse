using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Application.Notifications.Queries;
using Application.UnitTests.TestHelpers;
using Domain.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace Application.UnitTests.Notifications;

public class GetUnreadNotificationsHandlerTests
{
    private static Mock<IHttpContextAccessor> CreateHttpAccessor(Guid? userId = null)
    {
        var mock = new Mock<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext();
        if (userId.HasValue)
        {
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()) }, "Test"));
        }
        mock.Setup(x => x.HttpContext).Returns(httpContext);
        return mock;
    }

    [Fact]
    public async Task Handle_ReturnsUnreadNotificationsForUser()
    {
        var userId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Notifications.Add(new Notification { Id = Guid.NewGuid(), UserId = userId, Type = NotificationType.General, Message = "Unread", IsRead = false });
            ctx.Notifications.Add(new Notification { Id = Guid.NewGuid(), UserId = userId, Type = NotificationType.Comment, Message = "Read", IsRead = true });
            ctx.Notifications.Add(new Notification { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Type = NotificationType.General, Message = "Other user", IsRead = false });
        });
        var handler = new GetUnreadNotificationsHandler(db, CreateHttpAccessor(userId).Object);

        var result = await handler.Handle(new GetUnreadNotificationsQuery(), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Message.Should().Be("Unread");
    }

    [Fact]
    public async Task Handle_NoUnread_ReturnsEmptyList()
    {
        var userId = Guid.NewGuid();
        using var db = TestDbContextFactory.Create();
        var handler = new GetUnreadNotificationsHandler(db, CreateHttpAccessor(userId).Object);

        var result = await handler.Handle(new GetUnreadNotificationsQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_NoUserClaim_ReturnsEmptyGuidUser()
    {
        using var db = TestDbContextFactory.Create();
        var handler = new GetUnreadNotificationsHandler(db, CreateHttpAccessor().Object);

        var result = await handler.Handle(new GetUnreadNotificationsQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }
}
