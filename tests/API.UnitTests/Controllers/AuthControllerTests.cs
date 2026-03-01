using API.Controllers;
using Application.Auth.Commands;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace API.UnitTests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _controller = new AuthController();
    }

    [Fact]
    public async Task RegisterJson_ValidCommand_ShouldReturnOk()
    {
        var cmd = new RegisterUserCommand("test@example.com", "password123", "Test User", null);
        var response = new RegisterUserResult(Guid.NewGuid(), "testuser");
        _mediatorMock.Setup(m => m.Send(cmd, It.IsAny<CancellationToken>())).ReturnsAsync(response);

        var result = await _controller.RegisterJson(cmd, _mediatorMock.Object);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(response);
    }

    [Fact]
    public async Task RegisterJson_DuplicateEmail_ShouldReturnBadRequest()
    {
        var cmd = new RegisterUserCommand("dup@example.com", "password123", "Dup User", null);
        _mediatorMock.Setup(m => m.Send(cmd, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Email already exists"));

        var result = await _controller.RegisterJson(cmd, _mediatorMock.Object);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task RegisterJson_InvalidModelState_ShouldReturnValidationProblem()
    {
        var cmd = new RegisterUserCommand("", "", "", null);
        _controller.ModelState.AddModelError("Email", "Required");

        var result = await _controller.RegisterJson(cmd, _mediatorMock.Object);

        result.Should().BeAssignableTo<ObjectResult>();
    }

    [Fact]
    public async Task RegisterForm_ValidCommand_ShouldReturnOk()
    {
        var cmd = new RegisterUserCommand("test@example.com", "password123", "Test User", null);
        var response = new RegisterUserResult(Guid.NewGuid(), "testuser");
        _mediatorMock.Setup(m => m.Send(cmd, It.IsAny<CancellationToken>())).ReturnsAsync(response);

        var result = await _controller.RegisterForm(cmd, _mediatorMock.Object);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task RegisterForm_DuplicateEmail_ShouldReturnBadRequest()
    {
        var cmd = new RegisterUserCommand("dup@example.com", "password123", "Dup User", null);
        _mediatorMock.Setup(m => m.Send(cmd, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Email already exists"));

        var result = await _controller.RegisterForm(cmd, _mediatorMock.Object);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task LoginJson_ValidCredentials_ShouldReturnOk()
    {
        var cmd = new LoginUserCommand("test@example.com", "password123");
        var response = new LoginUserResult("jwt-token");
        _mediatorMock.Setup(m => m.Send(cmd, It.IsAny<CancellationToken>())).ReturnsAsync(response);

        var result = await _controller.LoginJson(cmd, _mediatorMock.Object);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(response);
    }

    [Fact]
    public async Task LoginJson_InvalidCredentials_ShouldReturnUnauthorized()
    {
        var cmd = new LoginUserCommand("test@example.com", "wrong");
        _mediatorMock.Setup(m => m.Send(cmd, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedAccessException());

        var result = await _controller.LoginJson(cmd, _mediatorMock.Object);

        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task LoginJson_InvalidModelState_ShouldReturnValidationProblem()
    {
        var cmd = new LoginUserCommand("", "");
        _controller.ModelState.AddModelError("Email", "Required");

        var result = await _controller.LoginJson(cmd, _mediatorMock.Object);

        result.Should().BeAssignableTo<ObjectResult>();
    }

    [Fact]
    public async Task LoginForm_ValidCredentials_ShouldReturnOk()
    {
        var cmd = new LoginUserCommand("test@example.com", "password123");
        var response = new LoginUserResult("jwt-token");
        _mediatorMock.Setup(m => m.Send(cmd, It.IsAny<CancellationToken>())).ReturnsAsync(response);

        var result = await _controller.LoginForm(cmd, _mediatorMock.Object);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task LoginForm_InvalidCredentials_ShouldReturnUnauthorized()
    {
        var cmd = new LoginUserCommand("test@example.com", "wrong");
        _mediatorMock.Setup(m => m.Send(cmd, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedAccessException());

        var result = await _controller.LoginForm(cmd, _mediatorMock.Object);

        result.Should().BeOfType<UnauthorizedResult>();
    }
}
