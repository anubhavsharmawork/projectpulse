using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Assets.Queries;
using Application.UnitTests.TestHelpers;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Application.UnitTests.Assets
{
    public class GetAssetsByProjectHandlerTests
    {
        [Fact]
        public async Task Handle_WithAssets_ShouldReturnPaginatedResult()
        {
            // Arrange
            var projectId = Guid.NewGuid();
            using var db = TestDbContextFactory.CreateWithData(ctx =>
            {
                ctx.Projects.Add(new Project
                {
                    Id = projectId,
                    Name = "Test Project",
                    OwnerId = Guid.NewGuid(),
                    IsActive = true
                });

                for (int i = 1; i <= 5; i++)
                {
                    ctx.Assets.Add(new Asset
                    {
                        Id = Guid.NewGuid(),
                        ProjectId = projectId,
                        AssetTag = $"AST-{i:D3}",
                        Name = $"Asset {i}",
                        Location = "Building A",
                        PurchaseDate = DateTime.UtcNow.AddMonths(-i),
                        PurchasePrice = 1000m * i,
                        CurrentValue = 900m * i,
                        Status = i <= 3 ? AssetStatus.Available : AssetStatus.InUse,
                        IsActive = true
                    });
                }
            });

            var handler = new GetAssetsByProjectHandler(db);
            var query = new GetAssetsByProjectQuery(projectId);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.TotalCount.Should().Be(5);
            result.Items.Should().HaveCount(5);
            result.Page.Should().Be(1);
        }

        [Fact]
        public async Task Handle_WithStatusFilter_ShouldReturnFilteredResults()
        {
            // Arrange
            var projectId = Guid.NewGuid();
            using var db = TestDbContextFactory.CreateWithData(ctx =>
            {
                ctx.Projects.Add(new Project
                {
                    Id = projectId,
                    Name = "Test Project",
                    OwnerId = Guid.NewGuid(),
                    IsActive = true
                });

                ctx.Assets.Add(new Asset
                {
                    Id = Guid.NewGuid(),
                    ProjectId = projectId,
                    AssetTag = "AST-001",
                    Name = "Available Asset",
                    Location = "Building A",
                    PurchaseDate = DateTime.UtcNow,
                    Status = AssetStatus.Available,
                    IsActive = true
                });

                ctx.Assets.Add(new Asset
                {
                    Id = Guid.NewGuid(),
                    ProjectId = projectId,
                    AssetTag = "AST-002",
                    Name = "InUse Asset",
                    Location = "Building B",
                    PurchaseDate = DateTime.UtcNow,
                    Status = AssetStatus.InUse,
                    IsActive = true
                });
            });

            var handler = new GetAssetsByProjectHandler(db);
            var query = new GetAssetsByProjectQuery(projectId, Status: AssetStatus.Available);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.TotalCount.Should().Be(1);
            result.Items.Should().HaveCount(1);
            result.Items[0].Name.Should().Be("Available Asset");
        }

        [Fact]
        public async Task Handle_WithSearchTerm_ShouldReturnMatchingResults()
        {
            // Arrange
            var projectId = Guid.NewGuid();
            using var db = TestDbContextFactory.CreateWithData(ctx =>
            {
                ctx.Projects.Add(new Project
                {
                    Id = projectId,
                    Name = "Test Project",
                    OwnerId = Guid.NewGuid(),
                    IsActive = true
                });

                ctx.Assets.Add(new Asset
                {
                    Id = Guid.NewGuid(),
                    ProjectId = projectId,
                    AssetTag = "AST-001",
                    Name = "Dell Laptop",
                    Manufacturer = "Dell",
                    Location = "IT Room",
                    PurchaseDate = DateTime.UtcNow,
                    Status = AssetStatus.Available,
                    IsActive = true
                });

                ctx.Assets.Add(new Asset
                {
                    Id = Guid.NewGuid(),
                    ProjectId = projectId,
                    AssetTag = "AST-002",
                    Name = "HP Monitor",
                    Manufacturer = "HP",
                    Location = "Office",
                    PurchaseDate = DateTime.UtcNow,
                    Status = AssetStatus.Available,
                    IsActive = true
                });
            });

            var handler = new GetAssetsByProjectHandler(db);
            var query = new GetAssetsByProjectQuery(projectId, Search: "Dell");

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.TotalCount.Should().Be(1);
            result.Items[0].Name.Should().Be("Dell Laptop");
        }

        [Fact]
        public async Task Handle_EmptyProject_ShouldReturnEmptyResult()
        {
            // Arrange
            var projectId = Guid.NewGuid();
            using var db = TestDbContextFactory.Create();

            var handler = new GetAssetsByProjectHandler(db);
            var query = new GetAssetsByProjectQuery(projectId);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.TotalCount.Should().Be(0);
            result.Items.Should().BeEmpty();
        }

        [Fact]
        public async Task Handle_InactiveAssets_ShouldBeExcluded()
        {
            // Arrange
            var projectId = Guid.NewGuid();
            using var db = TestDbContextFactory.CreateWithData(ctx =>
            {
                ctx.Projects.Add(new Project
                {
                    Id = projectId,
                    Name = "Test Project",
                    OwnerId = Guid.NewGuid(),
                    IsActive = true
                });

                ctx.Assets.Add(new Asset
                {
                    Id = Guid.NewGuid(),
                    ProjectId = projectId,
                    AssetTag = "AST-001",
                    Name = "Active Asset",
                    Location = "Room 1",
                    PurchaseDate = DateTime.UtcNow,
                    Status = AssetStatus.Available,
                    IsActive = true
                });

                ctx.Assets.Add(new Asset
                {
                    Id = Guid.NewGuid(),
                    ProjectId = projectId,
                    AssetTag = "AST-002",
                    Name = "Deleted Asset",
                    Location = "Room 2",
                    PurchaseDate = DateTime.UtcNow,
                    Status = AssetStatus.Available,
                    IsActive = false
                });
            });

            var handler = new GetAssetsByProjectHandler(db);
            var query = new GetAssetsByProjectQuery(projectId);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.TotalCount.Should().Be(1);
            result.Items[0].Name.Should().Be("Active Asset");
        }
    }
}
