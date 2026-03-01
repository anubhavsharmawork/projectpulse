using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.Projects.Queries;
using Application.UnitTests.TestHelpers;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Xunit;

namespace Application.UnitTests.Projects;

public class GetProjectConfigHandlerTests
{
    [Fact]
    public async Task Handle_ProjectWithoutTemplate_ReturnsDomainDefaults()
    {
        var projectId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Projects.Add(new Project { Id = projectId, Name = "IT Project", DomainType = DomainType.IT });
        });
        var handler = new GetProjectConfigHandler(db);

        var result = await handler.Handle(new GetProjectConfigQuery(projectId), CancellationToken.None);

        result.ProjectId.Should().Be(projectId);
        result.DomainType.Should().Be("IT");
        result.WorkItemTypeLabels.Should().ContainKey("1");
        result.WorkItemTypeLabels["1"].Should().Be("Epic");
    }

    [Fact]
    public async Task Handle_ProjectNotFound_ThrowsInvalidOperation()
    {
        using var db = TestDbContextFactory.Create();
        var handler = new GetProjectConfigHandler(db);

        var act = async () => await handler.Handle(new GetProjectConfigQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Project not found*");
    }

    [Fact]
    public async Task Handle_HealthcareDomain_ReturnsCorrectLabels()
    {
        var projectId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Projects.Add(new Project { Id = projectId, Name = "HC Project", DomainType = DomainType.Healthcare });
        });
        var handler = new GetProjectConfigHandler(db);

        var result = await handler.Handle(new GetProjectConfigQuery(projectId), CancellationToken.None);

        result.WorkItemTypeLabels["1"].Should().Be("Initiative");
    }

    [Fact]
    public async Task Handle_ConstructionDomain_ReturnsCorrectLabels()
    {
        var projectId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Projects.Add(new Project { Id = projectId, Name = "Construction", DomainType = DomainType.Construction });
        });
        var handler = new GetProjectConfigHandler(db);

        var result = await handler.Handle(new GetProjectConfigQuery(projectId), CancellationToken.None);

        result.WorkItemTypeLabels["1"].Should().Be("Phase");
        result.WorkItemTypeLabels["3"].Should().Be("Punch Item");
    }

    [Theory]
    [InlineData(DomainType.IT, "Epic")]
    [InlineData(DomainType.Healthcare, "Initiative")]
    [InlineData(DomainType.PublicSafety, "Operation")]
    [InlineData(DomainType.Construction, "Phase")]
    [InlineData(DomainType.Infrastructure, "Program")]
    [InlineData(DomainType.EconomicDevelopment, "Program")]
    [InlineData(DomainType.Technology, "Epic")]
    public async System.Threading.Tasks.Task GetLabelsByDomainType_AllDomains_ReturnCorrectLevel1(DomainType domain, string expectedLevel1)
    {
        var projectId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Projects.Add(new Project { Id = projectId, Name = "Project", DomainType = domain });
        });
        var handler = new GetProjectConfigHandler(db);

        var result = await handler.Handle(new GetProjectConfigQuery(projectId), CancellationToken.None);

        result.WorkItemTypeLabels["1"].Should().Be(expectedLevel1);
        result.WorkItemTypeLabels.Should().ContainKey("2");
        result.WorkItemTypeLabels.Should().ContainKey("3");
        result.WorkItemTypeLabels.Should().ContainKey("4");
    }
}
