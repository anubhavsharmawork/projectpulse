using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Application.Notifications.Commands;
using Application.UnitTests.TestHelpers;
using Domain.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Application.UnitTests.Notifications
{
    public class MarkNotificationReadHandlerTests
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
        public async Task Handle_MarkNotificationRead_Succeeds()
        {
            var userId = Guid.NewGuid();
            using var db = TestDbContextFactory.CreateWithData(ctx =>
            {
                ctx.Notifications.Add(new Notification { Id = Guid.NewGuid(), UserId = userId, Message = "X", IsRead = false });
            });

            var handler = new MarkNotificationReadHandler(db, CreateHttpAccessor(userId).Object);
            var notif = await db.Notifications.FirstAsync();

            await handler.Handle(new MarkNotificationReadCommand(notif.Id), CancellationToken.None);

            var saved = await db.Notifications.FindAsync(notif.Id);
            saved.IsRead.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_MarkNotificationRead_NotFound_Throws()
        {
            var userId = Guid.NewGuid();
            using var db = TestDbContextFactory.Create();

            var handler = new MarkNotificationReadHandler(db, CreateHttpAccessor(userId).Object);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                handler.Handle(new MarkNotificationReadCommand(Guid.NewGuid()), CancellationToken.None));
        }

        [Fact]
        public async Task Handle_MarkAllNotificationsRead_ReturnsCount_AndFlagsRead()
        {
            var userId = Guid.NewGuid();
            using var db = TestDbContextFactory.CreateWithData(ctx =>
            {
                ctx.Notifications.Add(new Notification { Id = Guid.NewGuid(), UserId = userId, Message = "1", IsRead = false });
                ctx.Notifications.Add(new Notification { Id = Guid.NewGuid(), UserId = userId, Message = "2", IsRead = false });
                ctx.Notifications.Add(new Notification { Id = Guid.NewGuid(), UserId = userId, Message = "3", IsRead = true });
                ctx.Notifications.Add(new Notification { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Message = "other", IsRead = false });
            });

            var handler = new MarkAllNotificationsReadHandler(db, CreateHttpAccessor(userId).Object);

            var count = await handler.Handle(new MarkAllNotificationsReadCommand(), CancellationToken.None);

            count.Should().Be(2);
            (await db.Notifications.CountAsync(n => n.UserId == userId && n.IsRead)).Should().Be(3);
        }
    }
}
