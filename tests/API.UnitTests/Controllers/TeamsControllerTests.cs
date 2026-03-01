using API.Controllers;
using Application.Teams.Commands;
using Application.Teams.Queries;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace API.UnitTests.Controllers;

public class TeamsControllerTests
{
    private readonly Mock<IMediator> _mediator = new();
    private readonly TeamsController _controller;

    public TeamsControllerTests()
    {
        _controller = new TeamsController(_mediator.Object);
    }

    [Fact]
    public async Task GetMembers_ReturnsOk()
    {
        _mediator.Setup(m => m.Send(It.IsAny<GetTeamMembersQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamMemberDto>());

        var result = await _controller.GetMembers(Guid.NewGuid());

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetCapacity_ReturnsOk()
    {
        _mediator.Setup(m => m.Send(It.IsAny<GetTeamCapacityQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TeamCapacityDto(Guid.NewGuid(), "Team", 0, 0, 0, 0, new List<MemberCapacityDto>()));

        var result = await _controller.GetCapacity(Guid.NewGuid());

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task AddMember_ReturnsCreatedAtAction()
    {
        var teamId = Guid.NewGuid();
        _mediator.Setup(m => m.Send(It.IsAny<CreateTeamMemberCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateTeamMemberResult(Guid.NewGuid()));
        var request = new AddTeamMemberRequest(Guid.NewGuid(), "Developer", null, null, 40, 0);

        var result = await _controller.AddMember(teamId, request);

        result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task UpdateMember_ReturnsNoContent()
    {
        _mediator.Setup(m => m.Send(It.IsAny<UpdateTeamMemberCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Unit.Value);
        var request = new UpdateTeamMemberRequest("QA", null, null, 40, 0);

        var result = await _controller.UpdateMember(Guid.NewGuid(), request);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task RemoveMember_ReturnsNoContent()
    {
        _mediator.Setup(m => m.Send(It.IsAny<RemoveTeamMemberCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Unit.Value);

        var result = await _controller.RemoveMember(Guid.NewGuid());

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task AssignToProject_Success_ReturnsOk()
    {
        _mediator.Setup(m => m.Send(It.IsAny<AssignToProjectCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssignToProjectResult(Guid.NewGuid(), Guid.NewGuid()));
        var request = new AssignToProjectRequest("testuser", "Developer", null, null, 40, 0);

        var result = await _controller.AssignToProject(Guid.NewGuid(), request);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task AssignToProject_InvalidOperation_ReturnsBadRequest()
    {
        _mediator.Setup(m => m.Send(It.IsAny<AssignToProjectCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("User not found"));
        var request = new AssignToProjectRequest("unknown", "Developer", null, null, 40, 0);

        var result = await _controller.AssignToProject(Guid.NewGuid(), request);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetMembersByProject_ReturnsOk()
    {
        _mediator.Setup(m => m.Send(It.IsAny<GetTeamMembersByProjectQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamMemberDto>());

        var result = await _controller.GetMembersByProject(Guid.NewGuid());

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetProjectRoles_ReturnsOk()
    {
        _mediator.Setup(m => m.Send(It.IsAny<GetProjectRolesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProjectRoleDto>());

        var result = await _controller.GetProjectRoles(Guid.NewGuid());

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task UnassignFromProject_ReturnsNoContent()
    {
        _mediator.Setup(m => m.Send(It.IsAny<UnassignFromProjectCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Unit.Value);
        var request = new UnassignFromProjectRequest(Guid.NewGuid());

        var result = await _controller.UnassignFromProject(Guid.NewGuid(), request);

        result.Should().BeOfType<NoContentResult>();
    }
}
