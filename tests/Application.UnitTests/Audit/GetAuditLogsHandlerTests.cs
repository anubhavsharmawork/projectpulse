using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.Audit.Queries;
using Application.UnitTests.TestHelpers;
using Domain.Entities;
using FluentAssertions;
using Xunit;

namespace Application.UnitTests.Audit;

public class GetAuditLogsHandlerTests
{
    [Fact]
    public async Task Handle_NoFilters_ReturnsAllLogs()
    {
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.AuditLogs.Add(new AuditLog { Id = Guid.NewGuid(), EntityType = "Project", EntityId = Guid.NewGuid(), Action = "Created", Timestamp = DateTime.UtcNow });
            ctx.AuditLogs.Add(new AuditLog { Id = Guid.NewGuid(), EntityType = "WorkItem", EntityId = Guid.NewGuid(), Action = "Updated", Timestamp = DateTime.UtcNow });
        });
        var handler = new GetAuditLogsHandler(db);

        var result = await handler.Handle(new GetAuditLogsQuery(), CancellationToken.None);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_FilterByEntityType_ReturnsFiltered()
    {
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.AuditLogs.Add(new AuditLog { Id = Guid.NewGuid(), EntityType = "Project", EntityId = Guid.NewGuid(), Action = "Created", Timestamp = DateTime.UtcNow });
            ctx.AuditLogs.Add(new AuditLog { Id = Guid.NewGuid(), EntityType = "WorkItem", EntityId = Guid.NewGuid(), Action = "Updated", Timestamp = DateTime.UtcNow });
        });
        var handler = new GetAuditLogsHandler(db);

        var result = await handler.Handle(new GetAuditLogsQuery(EntityType: "Project"), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].EntityType.Should().Be("Project");
    }

    [Fact]
    public async Task Handle_FilterByEntityId_ReturnsFiltered()
    {
        var entityId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.AuditLogs.Add(new AuditLog { Id = Guid.NewGuid(), EntityType = "Project", EntityId = entityId, Action = "Created", Timestamp = DateTime.UtcNow });
            ctx.AuditLogs.Add(new AuditLog { Id = Guid.NewGuid(), EntityType = "Project", EntityId = Guid.NewGuid(), Action = "Updated", Timestamp = DateTime.UtcNow });
        });
        var handler = new GetAuditLogsHandler(db);

        var result = await handler.Handle(new GetAuditLogsQuery(EntityId: entityId), CancellationToken.None);

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_FilterByUserId_ReturnsFiltered()
    {
        var userId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.AuditLogs.Add(new AuditLog { Id = Guid.NewGuid(), EntityType = "Project", EntityId = Guid.NewGuid(), Action = "Created", Timestamp = DateTime.UtcNow, UserId = userId });
            ctx.AuditLogs.Add(new AuditLog { Id = Guid.NewGuid(), EntityType = "Project", EntityId = Guid.NewGuid(), Action = "Updated", Timestamp = DateTime.UtcNow, UserId = Guid.NewGuid() });
        });
        var handler = new GetAuditLogsHandler(db);

        var result = await handler.Handle(new GetAuditLogsQuery(UserId: userId), CancellationToken.None);

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_FilterByDateRange_ReturnsFiltered()
    {
        var now = DateTime.UtcNow;
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.AuditLogs.Add(new AuditLog { Id = Guid.NewGuid(), EntityType = "Project", EntityId = Guid.NewGuid(), Action = "Old", Timestamp = now.AddDays(-10) });
            ctx.AuditLogs.Add(new AuditLog { Id = Guid.NewGuid(), EntityType = "Project", EntityId = Guid.NewGuid(), Action = "Recent", Timestamp = now.AddDays(-1) });
        });
        var handler = new GetAuditLogsHandler(db);

        var result = await handler.Handle(new GetAuditLogsQuery(From: now.AddDays(-5), To: now), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Action.Should().Be("Recent");
    }

    [Fact]
    public async Task Handle_LimitApplied_ReturnsLimitedResults()
    {
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            for (int i = 0; i < 10; i++)
                ctx.AuditLogs.Add(new AuditLog { Id = Guid.NewGuid(), EntityType = "Project", EntityId = Guid.NewGuid(), Action = $"Action{i}", Timestamp = DateTime.UtcNow.AddMinutes(-i) });
        });
        var handler = new GetAuditLogsHandler(db);

        var result = await handler.Handle(new GetAuditLogsQuery(Limit: 3), CancellationToken.None);

        result.Should().HaveCount(3);
    }
}
