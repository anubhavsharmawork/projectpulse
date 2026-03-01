using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.Assets.Queries;
using Application.UnitTests.TestHelpers;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Xunit;

namespace Application.UnitTests.Assets;

public class GetAssetHistoryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsHistoryForAsset()
    {
        var assetId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.AssetHistoryEntries.Add(new AssetHistoryEntry { Id = Guid.NewGuid(), AssetId = assetId, ChangeType = AssetChangeType.Created, ChangedBy = "user1", ChangedAt = DateTime.UtcNow });
            ctx.AssetHistoryEntries.Add(new AssetHistoryEntry { Id = Guid.NewGuid(), AssetId = assetId, ChangeType = AssetChangeType.StatusChanged, ChangedBy = "user2", ChangedAt = DateTime.UtcNow.AddHours(1) });
            ctx.AssetHistoryEntries.Add(new AssetHistoryEntry { Id = Guid.NewGuid(), AssetId = Guid.NewGuid(), ChangeType = AssetChangeType.Created, ChangedBy = "user3", ChangedAt = DateTime.UtcNow });
        });
        var handler = new GetAssetHistoryHandler(db);

        var result = await handler.Handle(new GetAssetHistoryQuery(assetId), CancellationToken.None);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_NoHistory_ReturnsEmptyList()
    {
        using var db = TestDbContextFactory.Create();
        var handler = new GetAssetHistoryHandler(db);

        var result = await handler.Handle(new GetAssetHistoryQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeEmpty();
    }
}

public class GetAssetMaintenanceHistoryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsMaintenanceRecords()
    {
        var assetId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.MaintenanceRecords.Add(new MaintenanceRecord { Id = Guid.NewGuid(), AssetId = assetId, ScheduledDate = DateTime.UtcNow, MaintenanceType = MaintenanceType.Preventive, Description = "Check", Cost = 100 });
        });
        var handler = new GetAssetMaintenanceHistoryHandler(db);

        var result = await handler.Handle(new GetAssetMaintenanceHistoryQuery(assetId), CancellationToken.None);

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_NoRecords_ReturnsEmptyList()
    {
        using var db = TestDbContextFactory.Create();
        var handler = new GetAssetMaintenanceHistoryHandler(db);

        var result = await handler.Handle(new GetAssetMaintenanceHistoryQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeEmpty();
    }
}

public class GetAssetCheckoutHistoryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsCheckoutHistory()
    {
        var assetId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Users.Add(new User { Id = userId, Email = "user@test.com", DisplayName = "User", UserName = "user" });
            ctx.AssetCheckouts.Add(new AssetCheckout { Id = Guid.NewGuid(), AssetId = assetId, CheckedOutToUserId = userId, CheckedOutAt = DateTime.UtcNow, CheckedOutBy = "user1", Condition = "Good" });
        });
        var handler = new GetAssetCheckoutHistoryHandler(db);

        var result = await handler.Handle(new GetAssetCheckoutHistoryQuery(assetId), CancellationToken.None);

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_NoCheckouts_ReturnsEmptyList()
    {
        using var db = TestDbContextFactory.Create();
        var handler = new GetAssetCheckoutHistoryHandler(db);

        var result = await handler.Handle(new GetAssetCheckoutHistoryQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeEmpty();
    }
}

public class GetDomainAssetConfigHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsActiveConfigsForDomain()
    {
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.DomainAssetConfigs.Add(new DomainAssetConfig { Id = Guid.NewGuid(), DomainType = DomainType.IT, AssetType = AssetType.Equipment, Category = AssetCategory.Physical, DisplayLabel = "Laptop", IsActive = true, SortOrder = 1 });
            ctx.DomainAssetConfigs.Add(new DomainAssetConfig { Id = Guid.NewGuid(), DomainType = DomainType.IT, AssetType = AssetType.SoftwareLicense, Category = AssetCategory.Digital, DisplayLabel = "License", IsActive = false, SortOrder = 2 });
            ctx.DomainAssetConfigs.Add(new DomainAssetConfig { Id = Guid.NewGuid(), DomainType = DomainType.Healthcare, AssetType = AssetType.Equipment, Category = AssetCategory.Physical, DisplayLabel = "MRI", IsActive = true, SortOrder = 1 });
        });
        var handler = new GetDomainAssetConfigHandler(db);

        var result = await handler.Handle(new GetDomainAssetConfigQuery(DomainType.IT), CancellationToken.None);

        result.DomainType.Should().Be(DomainType.IT);
        result.AssetTypes.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_NoneFound_ReturnsEmptyList()
    {
        using var db = TestDbContextFactory.Create();
        var handler = new GetDomainAssetConfigHandler(db);

        var result = await handler.Handle(new GetDomainAssetConfigQuery(DomainType.IT), CancellationToken.None);

        result.AssetTypes.Should().BeEmpty();
    }
}
