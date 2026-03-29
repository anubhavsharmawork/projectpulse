using API.Controllers;
using Application.Notifications.Commands;
using Application.Notifications.Queries;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace API.UnitTests.Controllers;

public class NotificationsControllerTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly NotificationsController _controller;

    public NotificationsControllerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _controller = new NotificationsController(_mediatorMock.Object);
    }

    [Fact]
    public async Task GetUnread_ShouldReturnOk()
    {
        var notifications = new List<NotificationDto>();
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetUnreadNotificationsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(notifications);

        var result = await _controller.GetUnread();

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task MarkRead_ValidId_ShouldReturnNoContent()
    {
        var id = Guid.NewGuid();
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<MarkNotificationReadCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(Unit.Value));

        var result = await _controller.MarkRead(id);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task MarkAllRead_ShouldReturnOkWithCount()
    {
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<MarkAllNotificationsReadCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        var result = await _controller.MarkAllRead();

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task MarkAllRead_MediatorThrows_PropagatesException()
    {
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<MarkAllNotificationsReadCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DB error"));

        await Assert.ThrowsAsync<Exception>(() => _controller.MarkAllRead());
    }
}
