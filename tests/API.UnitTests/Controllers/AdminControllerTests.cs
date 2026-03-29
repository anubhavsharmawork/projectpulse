using API.Controllers;
using Application.Admin.Queries;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace API.UnitTests.Controllers;

public class AdminControllerTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly AdminController _controller;

    public AdminControllerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _controller = new AdminController(_mediatorMock.Object);
    }

    [Fact]
    public async Task GetRoles_ShouldReturnOk()
    {
        var roles = new List<RoleDto>();
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetRolesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(roles);

        var result = await _controller.GetRoles();

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetRoles_MediatorThrows_PropagatesException()
    {
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetRolesQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DB down"));

        await Assert.ThrowsAsync<Exception>(() => _controller.GetRoles());
    }
}
