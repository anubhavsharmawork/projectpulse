using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.WorkItems.Commands;
using Application.UnitTests.TestHelpers;
using Domain.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Application.UnitTests.WorkItems;

public class CreateBugHandlerTests
{
    [Fact]
    public async Task Handle_ValidCommand_CreatesBug()
    {
        var projectId = Guid.NewGuid();
        using var db = TestDbContextFactory.Create();
        var handler = new CreateBugHandler(db);
        var cmd = new CreateBugCommand(projectId, "Login Bug", "Desc", null, BugSeverity.High, "Click login", "Should redirect", "Shows error", "Chrome");

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.BugId.Should().NotBe(Guid.Empty);
        var bug = await db.WorkItems.OfType<BugWorkItem>().FirstAsync(b => b.Id == result.BugId);
        bug.Title.Should().Be("Login Bug");
        bug.Severity.Should().Be(BugSeverity.High);
        bug.StepsToReproduce.Should().Be("Click login");
        bug.Type.Should().Be(WorkItemType.Bug);
    }

    [Fact]
    public async Task Handle_MinimalCommand_CreatesBugWithDefaults()
    {
        using var db = TestDbContextFactory.Create();
        var handler = new CreateBugHandler(db);
        var cmd = new CreateBugCommand(Guid.NewGuid(), "Simple Bug");

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.BugId.Should().NotBe(Guid.Empty);
        var bug = await db.WorkItems.OfType<BugWorkItem>().FirstAsync(b => b.Id == result.BugId);
        bug.Severity.Should().Be(BugSeverity.Medium);
        bug.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithParentId_SetsParent()
    {
        var parentId = Guid.NewGuid();
        using var db = TestDbContextFactory.Create();
        var handler = new CreateBugHandler(db);
        var cmd = new CreateBugCommand(Guid.NewGuid(), "Child Bug", ParentId: parentId);

        var result = await handler.Handle(cmd, CancellationToken.None);

        var bug = await db.WorkItems.OfType<BugWorkItem>().FirstAsync(b => b.Id == result.BugId);
        bug.ParentId.Should().Be(parentId);
    }
}
