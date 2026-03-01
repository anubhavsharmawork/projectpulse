using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Assets.Queries;
using Application.UnitTests.TestHelpers;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Xunit;

namespace Application.UnitTests.Assets;

public class GetAssetByIdHandlerTests
{
    [Fact]
    public async Task Handle_ExistingActiveAsset_ReturnsDto()
    {
        var assetId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Assets.Add(new Asset { Id = assetId, ProjectId = Guid.NewGuid(), AssetTag = "A1", Name = "Test", Status = AssetStatus.Available, Location = "Office", PurchaseDate = DateTime.UtcNow, IsActive = true, PurchasePrice = 1000, CurrentValue = 800, UsefulLifeYears = 5 });
        });
        var handler = new GetAssetByIdHandler(db);

        var result = await handler.Handle(new GetAssetByIdQuery(assetId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Test");
        result.PurchasePrice.Should().Be(1000);
    }

    [Fact]
    public async Task Handle_NonExistentAsset_ReturnsNull()
    {
        using var db = TestDbContextFactory.Create();
        var handler = new GetAssetByIdHandler(db);

        var result = await handler.Handle(new GetAssetByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_InactiveAsset_ReturnsNull()
    {
        var assetId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Assets.Add(new Asset { Id = assetId, ProjectId = Guid.NewGuid(), AssetTag = "A1", Name = "Inactive", Status = AssetStatus.Available, Location = "Office", PurchaseDate = DateTime.UtcNow, IsActive = false });
        });
        var handler = new GetAssetByIdHandler(db);

        var result = await handler.Handle(new GetAssetByIdQuery(assetId), CancellationToken.None);

        result.Should().BeNull();
    }
}
