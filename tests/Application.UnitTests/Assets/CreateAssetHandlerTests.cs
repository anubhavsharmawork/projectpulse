using System;
using System.Collections.Generic;
using System.Linq;
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
    public class CreateAssetHandlerTests
    {
        private Mock<IHttpContextAccessor> CreateHttpContextAccessor(Guid? userId = null)
        {
            var mock = new Mock<IHttpContextAccessor>();
            var httpContext = new DefaultHttpContext();

            if (userId.HasValue)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString())
                };
                var identity = new ClaimsIdentity(claims, "Test");
                httpContext.User = new ClaimsPrincipal(identity);
            }

            mock.Setup(x => x.HttpContext).Returns(httpContext);
            return mock;
        }

        [Fact]
        public async Task Handle_ValidCommand_ShouldCreateAsset()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            using var db = TestDbContextFactory.CreateWithData(ctx =>
            {
                ctx.Projects.Add(new Project
                {
                    Id = projectId,
                    Name = "Test Project",
                    OwnerId = userId,
                    IsActive = true
                });
            });

            var httpMock = CreateHttpContextAccessor(userId);
            var handler = new CreateAssetHandler(db, httpMock.Object);

            var command = new CreateAssetCommand(
                ProjectId: projectId,
                AssetTag: "AST-001",
                Name: "Test Equipment",
                Description: "A test asset",
                PurchaseDate: DateTime.UtcNow.AddMonths(-6),
                PurchasePrice: 5000m,
                CurrentValue: 4500m,
                Status: AssetStatus.Available,
                Location: "Building A, Room 101",
                AssignedToUserId: null,
                SerialNumber: "SN-123456",
                Manufacturer: "TestMfg",
                Model: "Model X",
                WarrantyExpiryDate: DateTime.UtcNow.AddYears(2),
                Notes: "Test notes",
                DepreciationMethod: DepreciationMethod.StraightLine,
                UsefulLifeYears: 5,
                AssetType: AssetType.Equipment,
                Category: AssetCategory.Physical,
                Weight: 25.5m,
                Dimensions: "100x50x30 cm",
                BarcodeValue: "BC-001",
                MaintenanceIntervalDays: 90,
                LicenseKey: null,
                LicensedSeats: null,
                LicenseExpiryDate: null,
                Vendor: null,
                GridReference: null,
                Capacity: null,
                RegulatoryId: null,
                DomainAssetConfigId: null);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.AssetId.Should().NotBe(Guid.Empty);

            var asset = await db.Assets.FirstOrDefaultAsync(a => a.Id == result.AssetId);
            asset.Should().NotBeNull();
            asset!.Name.Should().Be("Test Equipment");
            asset.AssetTag.Should().Be("AST-001");
            asset.PurchasePrice.Should().Be(5000m);
            asset.Status.Should().Be(AssetStatus.Available);
            asset.Location.Should().Be("Building A, Room 101");

            var history = await db.AssetHistoryEntries.FirstOrDefaultAsync(h => h.AssetId == result.AssetId);
            history.Should().NotBeNull();
            history!.ChangeType.Should().Be(AssetChangeType.Created);
        }

        [Fact]
        public async Task Handle_DuplicateAssetTag_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            using var db = TestDbContextFactory.CreateWithData(ctx =>
            {
                ctx.Projects.Add(new Project
                {
                    Id = projectId,
                    Name = "Test Project",
                    OwnerId = userId,
                    IsActive = true
                });
                ctx.Assets.Add(new Asset
                {
                    Id = Guid.NewGuid(),
                    ProjectId = projectId,
                    AssetTag = "AST-001",
                    Name = "Existing Asset",
                    Location = "Location",
                    PurchaseDate = DateTime.UtcNow,
                    Status = AssetStatus.Available
                });
            });

            var httpMock = CreateHttpContextAccessor(userId);
            var handler = new CreateAssetHandler(db, httpMock.Object);

            var command = new CreateAssetCommand(
                ProjectId: projectId,
                AssetTag: "AST-001",
                Name: "Duplicate Tag Asset",
                Description: null,
                PurchaseDate: DateTime.UtcNow,
                PurchasePrice: 1000m,
                CurrentValue: 1000m,
                Status: AssetStatus.Available,
                Location: "Location",
                AssignedToUserId: null,
                SerialNumber: null,
                Manufacturer: null,
                Model: null,
                WarrantyExpiryDate: null,
                Notes: null,
                DepreciationMethod: DepreciationMethod.NoDepreciation,
                UsefulLifeYears: 5,
                AssetType: AssetType.Equipment,
                Category: AssetCategory.Physical,
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
                RegulatoryId: null,
                DomainAssetConfigId: null);

            // Act
            var act = async () => await handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*already exists*");
        }

        [Fact]
        public async Task Handle_EmptyName_ShouldThrowArgumentException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            using var db = TestDbContextFactory.Create();
            var httpMock = CreateHttpContextAccessor(userId);
            var handler = new CreateAssetHandler(db, httpMock.Object);

            var command = new CreateAssetCommand(
                ProjectId: Guid.NewGuid(),
                AssetTag: "AST-002",
                Name: "",
                Description: null,
                PurchaseDate: DateTime.UtcNow,
                PurchasePrice: 1000m,
                CurrentValue: 1000m,
                Status: AssetStatus.Available,
                Location: "Location",
                AssignedToUserId: null,
                SerialNumber: null,
                Manufacturer: null,
                Model: null,
                WarrantyExpiryDate: null,
                Notes: null,
                DepreciationMethod: DepreciationMethod.NoDepreciation,
                UsefulLifeYears: 5,
                AssetType: AssetType.Equipment,
                Category: AssetCategory.Physical,
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
                RegulatoryId: null,
                DomainAssetConfigId: null);

            // Act
            var act = async () => await handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*name is required*");
        }

        [Fact]
        public async Task Handle_NegativePurchasePrice_ShouldThrowArgumentException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            using var db = TestDbContextFactory.CreateWithData(ctx =>
            {
                ctx.Projects.Add(new Project
                {
                    Id = projectId,
                    Name = "Test Project",
                    OwnerId = userId,
                    IsActive = true
                });
            });

            var httpMock = CreateHttpContextAccessor(userId);
            var handler = new CreateAssetHandler(db, httpMock.Object);

            var command = new CreateAssetCommand(
                ProjectId: projectId,
                AssetTag: "AST-003",
                Name: "Test Asset",
                Description: null,
                PurchaseDate: DateTime.UtcNow,
                PurchasePrice: -100m,
                CurrentValue: 0m,
                Status: AssetStatus.Available,
                Location: "Location",
                AssignedToUserId: null,
                SerialNumber: null,
                Manufacturer: null,
                Model: null,
                WarrantyExpiryDate: null,
                Notes: null,
                DepreciationMethod: DepreciationMethod.NoDepreciation,
                UsefulLifeYears: 5,
                AssetType: AssetType.Equipment,
                Category: AssetCategory.Physical,
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
                RegulatoryId: null,
                DomainAssetConfigId: null);

            // Act
            var act = async () => await handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*Purchase price*");
        }
    }
}
