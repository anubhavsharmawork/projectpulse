using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Claims;
using Application.Assets.Commands;
using Application.UnitTests.TestHelpers;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Application.UnitTests.Assets;

public class DeleteAssetHandlerTests
{
    private static Mock<IHttpContextAccessor> CreateHttpAccessor(Guid? userId = null)
    {
        var mock = new Mock<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext();
        if (userId.HasValue)
        {
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()) }, "Test"));
        }
        mock.Setup(x => x.HttpContext).Returns(httpContext);
        return mock;
    }

    [Fact]
    public async Task Handle_ValidAsset_SoftDeletes()
    {
        var assetId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Assets.Add(new Asset { Id = assetId, ProjectId = Guid.NewGuid(), AssetTag = "A1", Name = "Asset", Status = AssetStatus.Available, Location = "Office", PurchaseDate = DateTime.UtcNow, IsActive = true });
        });
        var handler = new DeleteAssetHandler(db, CreateHttpAccessor(Guid.NewGuid()).Object);

        await handler.Handle(new DeleteAssetCommand(assetId), CancellationToken.None);

        var asset = await db.Assets.FirstAsync(a => a.Id == assetId);
        asset.IsActive.Should().BeFalse();
        (await db.AssetHistoryEntries.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Handle_AssetNotFound_ThrowsKeyNotFound()
    {
        using var db = TestDbContextFactory.Create();
        var handler = new DeleteAssetHandler(db, CreateHttpAccessor(Guid.NewGuid()).Object);

        var act = async () => await handler.Handle(new DeleteAssetCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Handle_InactiveAsset_ThrowsKeyNotFound()
    {
        var assetId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Assets.Add(new Asset { Id = assetId, ProjectId = Guid.NewGuid(), AssetTag = "A1", Name = "Asset", Status = AssetStatus.Available, Location = "Office", PurchaseDate = DateTime.UtcNow, IsActive = false });
        });
        var handler = new DeleteAssetHandler(db, CreateHttpAccessor(Guid.NewGuid()).Object);

        var act = async () => await handler.Handle(new DeleteAssetCommand(assetId), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
