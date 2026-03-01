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

public class AssignAssetHandlerTests
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
    public async Task Handle_ValidAssignment_CreatesCheckoutAndUpdatesAsset()
    {
        var userId = Guid.NewGuid();
        var assetId = Guid.NewGuid();
        var assigneeId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Assets.Add(new Asset { Id = assetId, ProjectId = Guid.NewGuid(), AssetTag = "A1", Name = "Laptop", Status = AssetStatus.Available, Location = "Office", PurchaseDate = DateTime.UtcNow, IsActive = true });
        });
        var handler = new AssignAssetHandler(db, CreateHttpAccessor(userId).Object);

        var result = await handler.Handle(new AssignAssetCommand(assetId, assigneeId, DateTime.UtcNow.AddDays(7), "Test"), CancellationToken.None);

        result.CheckoutId.Should().NotBe(Guid.Empty);
        var asset = await db.Assets.FirstAsync(a => a.Id == assetId);
        asset.Status.Should().Be(AssetStatus.InUse);
        asset.AssignedToUserId.Should().Be(assigneeId);
    }

    [Fact]
    public async Task Handle_AssetNotFound_ThrowsKeyNotFound()
    {
        using var db = TestDbContextFactory.Create();
        var handler = new AssignAssetHandler(db, CreateHttpAccessor(Guid.NewGuid()).Object);

        var act = async () => await handler.Handle(new AssignAssetCommand(Guid.NewGuid(), Guid.NewGuid(), null, null), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Handle_AssetAlreadyInUse_ThrowsInvalidOperation()
    {
        var assetId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Assets.Add(new Asset { Id = assetId, ProjectId = Guid.NewGuid(), AssetTag = "A1", Name = "Laptop", Status = AssetStatus.InUse, Location = "Office", PurchaseDate = DateTime.UtcNow, IsActive = true });
        });
        var handler = new AssignAssetHandler(db, CreateHttpAccessor(Guid.NewGuid()).Object);

        var act = async () => await handler.Handle(new AssignAssetCommand(assetId, Guid.NewGuid(), null, null), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*already assigned*");
    }
}
