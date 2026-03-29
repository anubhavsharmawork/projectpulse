using Application.Notifications;
using Application.UnitTests.TestHelpers;
using Domain.Entities;
using FluentAssertions;
using System;
using System.Linq;
using Xunit;

namespace Application.UnitTests.Notifications;

public class NotificationHelpersTests
{
    [Fact]
    public void AddNotification_ShouldAddNotificationToDb()
    {
        using var db = TestDbContextFactory.Create();

        var userId = Guid.NewGuid();
        db.AddNotification(userId, NotificationType.Mention, "Hello world", Guid.NewGuid());

        db.SaveChanges();

        db.Notifications.Should().HaveCount(1);
        var n = db.Notifications.ToList().Single();
        n.Should().NotBeNull();
        n.UserId.Should().Be(userId);
        n.Message.Should().Be("Hello world");
        n.IsRead.Should().BeFalse();
    }
}
