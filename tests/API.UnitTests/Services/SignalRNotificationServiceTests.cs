using API.Services;
using API.Hubs;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Moq;
using Xunit;

namespace API.UnitTests.Services;

public class SignalRNotificationServiceTests
{
    private readonly Mock<IHubContext<ProjectHub>> _hubContextMock;
    private readonly Mock<IHubClients> _clientsMock;
    private readonly Mock<IClientProxy> _clientProxyMock;
    private readonly SignalRNotificationService _service;

    public SignalRNotificationServiceTests()
    {
        _hubContextMock = new Mock<IHubContext<ProjectHub>>();
        _clientsMock = new Mock<IHubClients>();
        _clientProxyMock = new Mock<IClientProxy>();

        _hubContextMock.Setup(h => h.Clients).Returns(_clientsMock.Object);
        _clientsMock.Setup(c => c.User(It.IsAny<string>())).Returns(_clientProxyMock.Object);

        _service = new SignalRNotificationService(_hubContextMock.Object);
    }

    [Fact]
    public async Task SendMentionNotificationAsync_ShouldSendToCorrectUser()
    {
        var userId = Guid.NewGuid();
        var workItemId = Guid.NewGuid();

        await _service.SendMentionNotificationAsync(
            userId, workItemId, "Test Work Item", "John Doe");

        _clientsMock.Verify(c => c.User(userId.ToString()), Times.Once);
        _clientProxyMock.Verify(
            c => c.SendCoreAsync("Notification", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SendMentionNotificationAsync_WithCancellationToken_ShouldPassToken()
    {
        var userId = Guid.NewGuid();
        var cts = new CancellationTokenSource();

        await _service.SendMentionNotificationAsync(
            userId, Guid.NewGuid(), "Item", "User", cts.Token);

        _clientProxyMock.Verify(
            c => c.SendCoreAsync("Notification", It.IsAny<object?[]>(), cts.Token),
            Times.Once);
    }
}
