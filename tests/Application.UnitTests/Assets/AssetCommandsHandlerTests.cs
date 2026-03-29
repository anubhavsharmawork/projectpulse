using System;
using System.Linq;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Application.Assets.Commands;
using Application.UnitTests.TestHelpers;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Application.UnitTests.Assets
{
    public class AssetCommandsHandlerTests
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
        public async Task RecordMaintenance_UpdatesRecordAndAssetAndAddsHistory()
        {
            var userId = Guid.NewGuid();
            var assetId = Guid.NewGuid();
            var recId = Guid.NewGuid();

            using var db = TestDbContextFactory.CreateWithData(ctx =>
            {
                ctx.Assets.Add(new Asset { Id = assetId, Name = "A", IsActive = true, MaintenanceIntervalDays = 30 });
                ctx.MaintenanceRecords.Add(new MaintenanceRecord { Id = recId, AssetId = assetId, MaintenanceType = MaintenanceType.Preventive, Description = "sched" });
            });

            var handler = new RecordMaintenanceHandler(db, CreateHttpAccessor(userId).Object);
            var completed = DateTime.UtcNow.Date;
            await handler.Handle(new RecordMaintenanceCommand(recId, completed, "tech", 123.45m, "done"), CancellationToken.None);

            var saved = await db.MaintenanceRecords.FindAsync(recId);
            saved.PerformedBy.Should().Be("tech");
            saved.CompletedDate.Should().BeCloseTo(completed, precision: TimeSpan.FromSeconds(1));

            var asset = await db.Assets.FindAsync(assetId);
            asset.LastMaintenanceDate.Should().Be(completed);
            asset.NextMaintenanceDate.Should().Be(completed.AddDays(30));

            var history = await db.AssetHistoryEntries.Where(h => h.AssetId == assetId).ToListAsync();
            history.Should().NotBeEmpty();
            history[0].ChangeType.Should().Be(AssetChangeType.MaintenancePerformed);
        }

        [Fact]
        public async Task RecordMaintenance_RecordNotFound_ThrowsKeyNotFound()
        {
            using var db = TestDbContextFactory.Create();
            var handler = new RecordMaintenanceHandler(db, CreateHttpAccessor().Object);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(new RecordMaintenanceCommand(Guid.NewGuid(), DateTime.UtcNow, "x", 0m, null), CancellationToken.None));
        }

        [Fact]
        public async Task ReturnAsset_SetsCheckoutAndAssetStatus_AddsHistory()
        {
            var userId = Guid.NewGuid();
            var assetId = Guid.NewGuid();
            var checkoutId = Guid.NewGuid();

            using var db = TestDbContextFactory.CreateWithData(ctx =>
            {
                ctx.Assets.Add(new Asset { Id = assetId, Name = "A", IsActive = true, AssignedToUserId = Guid.NewGuid(), Status = AssetStatus.InUse });
                ctx.AssetCheckouts.Add(new AssetCheckout { Id = checkoutId, AssetId = assetId, CheckedOutToUserId = Guid.NewGuid(), CheckedOutAt = DateTime.UtcNow.AddDays(-1) });
            });

            var handler = new ReturnAssetHandler(db, CreateHttpAccessor(userId).Object);
            await handler.Handle(new ReturnAssetCommand(assetId, "Good", "ok"), CancellationToken.None);

            var co = await db.AssetCheckouts.FindAsync(checkoutId);
            co.ActualReturnDate.Should().NotBeNull();
            co.CheckedInBy.Should().Be(userId.ToString());
            co.Condition.Should().Be("Good");

            var asset = await db.Assets.FindAsync(assetId);
            asset.AssignedToUserId.Should().BeNull();
            asset.Status.Should().Be(AssetStatus.Available);

            var hist = await db.AssetHistoryEntries.Where(h => h.AssetId == assetId).ToListAsync();
            hist.Should().ContainSingle();
            hist[0].ChangeType.Should().Be(AssetChangeType.AssignmentChanged);
        }

        [Fact]
        public async Task ReturnAsset_AssetNotFound_Throws()
        {
            using var db = TestDbContextFactory.Create();
            var handler = new ReturnAssetHandler(db, CreateHttpAccessor().Object);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(new ReturnAssetCommand(Guid.NewGuid(), "Good", null), CancellationToken.None));
        }

        [Fact]
        public async Task ReturnAsset_NoActiveCheckout_ThrowsInvalidOperation()
        {
            var assetId = Guid.NewGuid();
            using var db = TestDbContextFactory.CreateWithData(ctx =>
            {
                ctx.Assets.Add(new Asset { Id = assetId, Name = "A", IsActive = true });
            });
            var handler = new ReturnAssetHandler(db, CreateHttpAccessor().Object);

            await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(new ReturnAssetCommand(assetId, "Good", null), CancellationToken.None));
        }

        [Fact]
        public async Task ScheduleMaintenance_CreatesRecord_WithCreatedBy()
        {
            var userId = Guid.NewGuid();
            var assetId = Guid.NewGuid();
            using var db = TestDbContextFactory.CreateWithData(ctx =>
            {
                ctx.Assets.Add(new Asset { Id = assetId, Name = "A", IsActive = true });
            });

            var handler = new ScheduleMaintenanceHandler(db, CreateHttpAccessor(userId).Object);
            var scheduled = DateTime.UtcNow.AddDays(7);
            var res = await handler.Handle(new ScheduleMaintenanceCommand(assetId, MaintenanceType.Corrective, scheduled, "desc", 10m, "n"), CancellationToken.None);

            var rec = await db.MaintenanceRecords.FindAsync(res.MaintenanceRecordId);
            rec.Should().NotBeNull();
            rec.CreatedBy.Should().Be(userId.ToString());
            rec.ScheduledDate.Should().Be(scheduled);
        }

        [Fact]
        public async Task ScheduleMaintenance_AssetNotFound_Throws()
        {
            using var db = TestDbContextFactory.Create();
            var handler = new ScheduleMaintenanceHandler(db, CreateHttpAccessor().Object);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(new ScheduleMaintenanceCommand(Guid.NewGuid(), MaintenanceType.Corrective, DateTime.UtcNow, "x", 0m, null), CancellationToken.None));
        }

        [Fact]
        public async Task UpdateAsset_Changes_CreateHistoryEntries()
        {
            var userId = Guid.NewGuid();
            var assetId = Guid.NewGuid();
            using var db = TestDbContextFactory.CreateWithData(ctx =>
            {
                ctx.Assets.Add(new Asset { Id = assetId, Name = "Old", Location = "L1", CurrentValue = 100m, IsActive = true, Status = AssetStatus.Available });
            });

            var handler = new UpdateAssetHandler(db, CreateHttpAccessor(userId).Object);
            var cmd = new UpdateAssetCommand(
                AssetId: assetId,
                Name: "NewName",
                Description: "d",
                Status: AssetStatus.InUse,
                Location: "L2",
                AssignedToUserId: null,
                SerialNumber: "sn",
                Manufacturer: "man",
                Model: "mod",
                WarrantyExpiryDate: null,
                Notes: "notes",
                CurrentValue: 150m,
                DepreciationMethod: DepreciationMethod.StraightLine,
                UsefulLifeYears: 5,
                Weight: null,
                Dimensions: null,
                BarcodeValue: null,
                MaintenanceIntervalDays: 90,
                LicenseKey: null,
                LicensedSeats: null,
                LicenseExpiryDate: null,
                Vendor: null,
                GridReference: null,
                Capacity: null,
                RegulatoryId: null);

            await handler.Handle(cmd, CancellationToken.None);

            var asset = await db.Assets.FindAsync(assetId);
            asset.Name.Should().Be("NewName");
            asset.Location.Should().Be("L2");
            asset.CurrentValue.Should().Be(150m);

            var history = await db.AssetHistoryEntries.Where(h => h.AssetId == assetId).ToListAsync();
            history.Select(h => h.ChangeType).Should().Contain(new[] { AssetChangeType.StatusChanged, AssetChangeType.LocationMoved, AssetChangeType.ValueAdjusted });
        }

        [Fact]
        public async Task UpdateAsset_NotFound_Throws()
        {
            using var db = TestDbContextFactory.Create();
            var handler = new UpdateAssetHandler(db, CreateHttpAccessor().Object);

            var cmd = new UpdateAssetCommand(
                AssetId: Guid.NewGuid(),
                Name: "n",
                Description: null,
                Status: AssetStatus.Available,
                Location: "l",
                AssignedToUserId: null,
                SerialNumber: null,
                Manufacturer: null,
                Model: null,
                WarrantyExpiryDate: null,
                Notes: null,
                CurrentValue: 0m,
                DepreciationMethod: DepreciationMethod.StraightLine,
                UsefulLifeYears: 5,
                Weight: null,
                Dimensions: null,
                BarcodeValue: null,
                MaintenanceIntervalDays: null,
                LicenseKey: null,
                LicensedSeats: null,
                LicenseExpiryDate: null,
                Vendor: null,
                GridReference: null,
                Capacity: null,
                RegulatoryId: null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(cmd, CancellationToken.None));
        }
    }
}
