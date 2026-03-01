using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Teams.Queries;
using Application.UnitTests.TestHelpers;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Xunit;

namespace Application.UnitTests.Teams;

public class GetTeamMembersHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsActiveMembers()
    {
        var teamId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Users.Add(new User { Id = userId, Email = "user@test.com", DisplayName = "User", UserName = "user" });
            ctx.TeamMembers.Add(new TeamMember { Id = Guid.NewGuid(), TeamId = teamId, UserId = userId, Role = "Dev", IsActive = true });
            ctx.TeamMembers.Add(new TeamMember { Id = Guid.NewGuid(), TeamId = teamId, UserId = Guid.NewGuid(), Role = "QA", IsActive = false });
        });
        var handler = new GetTeamMembersHandler(db);

        var result = await handler.Handle(new GetTeamMembersQuery(teamId), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Role.Should().Be("Dev");
    }

    [Fact]
    public async Task Handle_NoMembers_ReturnsEmptyList()
    {
        using var db = TestDbContextFactory.Create();
        var handler = new GetTeamMembersHandler(db);

        var result = await handler.Handle(new GetTeamMembersQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeEmpty();
    }
}

public class GetTeamMembersByProjectHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsTeamMembersForProject()
    {
        var projectId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Teams.Add(new Team { Id = teamId, Name = "Team", ProjectId = projectId });
            ctx.Users.Add(new User { Id = userId, Email = "user@test.com", DisplayName = "User", UserName = "user" });
            ctx.TeamMembers.Add(new TeamMember { Id = Guid.NewGuid(), TeamId = teamId, UserId = userId, Role = "Dev", IsActive = true });
        });
        var handler = new GetTeamMembersByProjectHandler(db);

        var result = await handler.Handle(new GetTeamMembersByProjectQuery(projectId), CancellationToken.None);

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_NoTeams_ReturnsEmptyList()
    {
        using var db = TestDbContextFactory.Create();
        var handler = new GetTeamMembersByProjectHandler(db);

        var result = await handler.Handle(new GetTeamMembersByProjectQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeEmpty();
    }
}

public class GetProjectRolesHandlerTests
{
    [Fact]
    public async Task Handle_ProjectHasRoles_ReturnsThem()
    {
        var projectId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.ProjectRoles.Add(new ProjectRole { Id = Guid.NewGuid(), ProjectId = projectId, RoleName = "Developer" });
            ctx.ProjectRoles.Add(new ProjectRole { Id = Guid.NewGuid(), ProjectId = projectId, RoleName = "QA" });
        });
        var handler = new GetProjectRolesHandler(db);

        var result = await handler.Handle(new GetProjectRolesQuery(projectId), CancellationToken.None);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_NoRoles_ReturnsDefaults()
    {
        var projectId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Projects.Add(new Project { Id = projectId, Name = "IT Project", DomainType = DomainType.IT });
        });
        var handler = new GetProjectRolesHandler(db);

        var result = await handler.Handle(new GetProjectRolesQuery(projectId), CancellationToken.None);

        result.Should().NotBeEmpty();
        result.Should().Contain(r => r.RoleName == "Developer");
    }

    [Fact]
    public async Task Handle_NoRolesNoProject_ReturnsEmptyList()
    {
        using var db = TestDbContextFactory.Create();
        var handler = new GetProjectRolesHandler(db);

        var result = await handler.Handle(new GetProjectRolesQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData(DomainType.Construction)]
    [InlineData(DomainType.Healthcare)]
    [InlineData(DomainType.IT)]
    [InlineData(DomainType.Technology)]
    [InlineData(DomainType.Infrastructure)]
    [InlineData(DomainType.EconomicDevelopment)]
    [InlineData(DomainType.PublicSafety)]
    public async Task Handle_DifferentDomains_ReturnsDomainSpecificRoles(DomainType domainType)
    {
        var projectId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Projects.Add(new Project { Id = projectId, Name = "Project", DomainType = domainType });
        });
        var handler = new GetProjectRolesHandler(db);

        var result = await handler.Handle(new GetProjectRolesQuery(projectId), CancellationToken.None);

        result.Should().NotBeEmpty();
    }
}

public class GetTeamCapacityHandlerTests
{
    [Fact]
    public async Task Handle_TeamNotFound_Throws()
    {
        using var db = TestDbContextFactory.Create();
        var handler = new GetTeamCapacityHandler(db);

        var act = async () => await handler.Handle(new GetTeamCapacityQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Team not found*");
    }

    [Fact]
    public async Task Handle_TeamWithMembers_ReturnsCapacity()
    {
        var teamId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Teams.Add(new Team { Id = teamId, Name = "Team", ProjectId = projectId });
            ctx.Users.Add(new User { Id = userId, Email = "user@test.com", DisplayName = "User", UserName = "user" });
            ctx.TeamMembers.Add(new TeamMember { Id = Guid.NewGuid(), TeamId = teamId, UserId = userId, Role = "Dev", IsActive = true, AvailabilityHoursPerWeek = 40 });
        });
        var handler = new GetTeamCapacityHandler(db);

        var result = await handler.Handle(new GetTeamCapacityQuery(teamId), CancellationToken.None);

        result.TeamId.Should().Be(teamId);
        result.TotalMembers.Should().Be(1);
        result.TotalAvailableHours.Should().Be(40);
    }

    [Fact]
    public async Task Handle_EmptyTeam_ReturnsZeroCapacity()
    {
        var teamId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Teams.Add(new Team { Id = teamId, Name = "Team", ProjectId = Guid.NewGuid() });
        });
        var handler = new GetTeamCapacityHandler(db);

        var result = await handler.Handle(new GetTeamCapacityQuery(teamId), CancellationToken.None);

        result.TotalMembers.Should().Be(0);
        result.TotalAvailableHours.Should().Be(0);
    }
}
