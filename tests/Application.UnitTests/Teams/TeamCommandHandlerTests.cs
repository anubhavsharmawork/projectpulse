using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Teams.Commands;
using Application.Teams.Queries;
using Application.UnitTests.TestHelpers;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Moq;
using System.Security.Claims;
using Xunit;

namespace Application.UnitTests.Teams;

public class CreateTeamMemberHandlerTests
{
    private static Mock<IHttpContextAccessor> CreateHttpAccessor(Guid? userId = null)
    {
        var mock = new Mock<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext();
        if (userId.HasValue)
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()) }, "Test"));
        mock.Setup(x => x.HttpContext).Returns(httpContext);
        return mock;
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesMember()
    {
        var teamId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Teams.Add(new Team { Id = teamId, Name = "Team", ProjectId = Guid.NewGuid() });
            ctx.Users.Add(new User { Id = userId, Email = "user@test.com", UserName = "user" });
        });
        var handler = new CreateTeamMemberHandler(db, CreateHttpAccessor(Guid.NewGuid()).Object);

        var result = await handler.Handle(new CreateTeamMemberCommand(teamId, userId, "Developer", null, null, 40, 50), CancellationToken.None);

        result.TeamMemberId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Handle_TeamNotFound_Throws()
    {
        using var db = TestDbContextFactory.Create();
        var handler = new CreateTeamMemberHandler(db, CreateHttpAccessor().Object);

        var act = async () => await handler.Handle(new CreateTeamMemberCommand(Guid.NewGuid(), Guid.NewGuid(), "Dev", null, null, 40, 0), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Team not found*");
    }

    [Fact]
    public async Task Handle_UserNotFound_Throws()
    {
        var teamId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Teams.Add(new Team { Id = teamId, Name = "Team", ProjectId = Guid.NewGuid() });
        });
        var handler = new CreateTeamMemberHandler(db, CreateHttpAccessor().Object);

        var act = async () => await handler.Handle(new CreateTeamMemberCommand(teamId, Guid.NewGuid(), "Dev", null, null, 40, 0), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*User not found*");
    }

    [Fact]
    public async Task Handle_DuplicateMember_Throws()
    {
        var teamId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Teams.Add(new Team { Id = teamId, Name = "Team", ProjectId = Guid.NewGuid() });
            ctx.Users.Add(new User { Id = userId, Email = "user@test.com", UserName = "user" });
            ctx.TeamMembers.Add(new TeamMember { Id = Guid.NewGuid(), TeamId = teamId, UserId = userId, Role = "Dev" });
        });
        var handler = new CreateTeamMemberHandler(db, CreateHttpAccessor().Object);

        var act = async () => await handler.Handle(new CreateTeamMemberCommand(teamId, userId, "Dev", null, null, 40, 0), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*already a member*");
    }
}

public class UpdateTeamMemberHandlerTests
{
    [Fact]
    public async Task Handle_ValidCommand_UpdatesMember()
    {
        var memberId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.TeamMembers.Add(new TeamMember { Id = memberId, TeamId = Guid.NewGuid(), UserId = Guid.NewGuid(), Role = "Dev" });
        });
        var handler = new UpdateTeamMemberHandler(db);

        await handler.Handle(new UpdateTeamMemberCommand(memberId, "QA", "Healthcare", "Testing", 30, 75), CancellationToken.None);

        var member = db.TeamMembers.First(m => m.Id == memberId);
        member.Role.Should().Be("QA");
        member.DomainExpertise.Should().Be("Healthcare");
        member.AvailabilityHoursPerWeek.Should().Be(30);
    }

    [Fact]
    public async Task Handle_MemberNotFound_Throws()
    {
        using var db = TestDbContextFactory.Create();
        var handler = new UpdateTeamMemberHandler(db);

        var act = async () => await handler.Handle(new UpdateTeamMemberCommand(Guid.NewGuid(), "QA", null, null, 40, 0), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Team member not found*");
    }
}

public class RemoveTeamMemberHandlerTests
{
    [Fact]
    public async Task Handle_ValidMember_RemovesMember()
    {
        var memberId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.TeamMembers.Add(new TeamMember { Id = memberId, TeamId = Guid.NewGuid(), UserId = Guid.NewGuid(), Role = "Dev" });
        });
        var handler = new RemoveTeamMemberHandler(db);

        await handler.Handle(new RemoveTeamMemberCommand(memberId), CancellationToken.None);

        db.TeamMembers.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_MemberNotFound_Throws()
    {
        using var db = TestDbContextFactory.Create();
        var handler = new RemoveTeamMemberHandler(db);

        var act = async () => await handler.Handle(new RemoveTeamMemberCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Team member not found*");
    }
}

public class UnassignFromProjectHandlerTests
{
    [Fact]
    public async Task Handle_ValidUnassign_RemovesMember()
    {
        var projectId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Teams.Add(new Team { Id = teamId, Name = "Team", ProjectId = projectId });
            ctx.TeamMembers.Add(new TeamMember { Id = Guid.NewGuid(), TeamId = teamId, UserId = userId, Role = "Dev" });
        });
        var handler = new UnassignFromProjectHandler(db);

        await handler.Handle(new UnassignFromProjectCommand(projectId, userId), CancellationToken.None);

        db.TeamMembers.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_NoTeamsForProject_Throws()
    {
        using var db = TestDbContextFactory.Create();
        var handler = new UnassignFromProjectHandler(db);

        var act = async () => await handler.Handle(new UnassignFromProjectCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*No teams found*");
    }

    [Fact]
    public async Task Handle_UserNotInTeam_Throws()
    {
        var projectId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Teams.Add(new Team { Id = teamId, Name = "Team", ProjectId = projectId });
        });
        var handler = new UnassignFromProjectHandler(db);

        var act = async () => await handler.Handle(new UnassignFromProjectCommand(projectId, Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*not assigned*");
    }
}

public class AssignToProjectHandlerTests
{
    private static Mock<IHttpContextAccessor> CreateHttpAccessor(Guid? userId = null)
    {
        var mock = new Mock<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext();
        if (userId.HasValue)
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()) }, "Test"));
        mock.Setup(x => x.HttpContext).Returns(httpContext);
        return mock;
    }

    [Fact]
    public async Task Handle_ValidAssignment_CreatesTeamAndMember()
    {
        var projectId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Projects.Add(new Project { Id = projectId, Name = "Project" });
            ctx.Users.Add(new User { Id = userId, Email = "user@test.com", UserName = "testuser" });
        });
        var handler = new AssignToProjectHandler(db, CreateHttpAccessor(Guid.NewGuid()).Object);

        var result = await handler.Handle(new AssignToProjectCommand(projectId, "testuser", "Dev", null, null, 40, 50), CancellationToken.None);

        result.TeamMemberId.Should().NotBe(Guid.Empty);
        result.TeamId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Handle_ExistingTeam_ReusesTeam()
    {
        var projectId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Projects.Add(new Project { Id = projectId, Name = "Project" });
            ctx.Teams.Add(new Team { Id = teamId, Name = "Team", ProjectId = projectId });
            ctx.Users.Add(new User { Id = userId, Email = "user@test.com", UserName = "testuser" });
        });
        var handler = new AssignToProjectHandler(db, CreateHttpAccessor(Guid.NewGuid()).Object);

        var result = await handler.Handle(new AssignToProjectCommand(projectId, "testuser", "Dev", null, null, 40, 50), CancellationToken.None);

        result.TeamId.Should().Be(teamId);
    }

    [Fact]
    public async Task Handle_ProjectNotFound_Throws()
    {
        using var db = TestDbContextFactory.Create();
        var handler = new AssignToProjectHandler(db, CreateHttpAccessor().Object);

        var act = async () => await handler.Handle(new AssignToProjectCommand(Guid.NewGuid(), "user", "Dev", null, null, 40, 0), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Project not found*");
    }

    [Fact]
    public async Task Handle_UserNotFound_Throws()
    {
        var projectId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Projects.Add(new Project { Id = projectId, Name = "Project" });
        });
        var handler = new AssignToProjectHandler(db, CreateHttpAccessor().Object);

        var act = async () => await handler.Handle(new AssignToProjectCommand(projectId, "unknown", "Dev", null, null, 40, 0), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*No user found*");
    }

    [Fact]
    public async Task Handle_AlreadyAssigned_Throws()
    {
        var projectId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Projects.Add(new Project { Id = projectId, Name = "Project" });
            ctx.Teams.Add(new Team { Id = teamId, Name = "Team", ProjectId = projectId });
            ctx.Users.Add(new User { Id = userId, Email = "user@test.com", UserName = "testuser" });
            ctx.TeamMembers.Add(new TeamMember { Id = Guid.NewGuid(), TeamId = teamId, UserId = userId, Role = "Dev" });
        });
        var handler = new AssignToProjectHandler(db, CreateHttpAccessor().Object);

        var act = async () => await handler.Handle(new AssignToProjectCommand(projectId, "testuser", "Dev", null, null, 40, 0), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*already assigned*");
    }
}
