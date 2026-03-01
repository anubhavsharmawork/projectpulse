using API.Controllers;
using Application.Users.Commands;
using Application.Users.Queries;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace API.UnitTests.Controllers;

public class UsersControllerTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly UsersController _controller;

    public UsersControllerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _controller = new UsersController(_mediatorMock.Object);
    }

    [Fact]
    public async Task GetUsers_WithSearch_ShouldReturnOk()
    {
        var users = new List<UserDto> { new(Guid.NewGuid(), "test@example.com", "Test User") };
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetUsersQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);

        var result = await _controller.GetUsers("test");

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(users);
    }

    [Fact]
    public async Task GetUsers_NullSearch_ShouldReturnOk()
    {
        var users = new List<UserDto>();
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetUsersQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);

        var result = await _controller.GetUsers(null);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ResolveUsername_UserFound_ShouldReturnOk()
    {
        var resolvedUser = new ResolvedUserDto("TestUser", "testuser");
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<ResolveUsernameQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(resolvedUser);
        var request = new ResolveUsernameRequest("testuser");

        var result = await _controller.ResolveUsername(request);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(resolvedUser);
    }

    [Fact]
    public async Task ResolveUsername_UserNotFound_ShouldReturnNotFound()
    {
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<ResolveUsernameQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ResolvedUserDto?)null);
        var request = new ResolveUsernameRequest("nonexistent");

        var result = await _controller.ResolveUsername(request);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task UpdateTimezone_ValidRequest_ShouldReturnOk()
    {
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<UpdateTimezoneCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(Unit.Value));
        var request = new UpdateTimezoneRequest("America/New_York", -300);

        var result = await _controller.UpdateTimezone(request);

        result.Should().BeOfType<OkObjectResult>();
    }
}
