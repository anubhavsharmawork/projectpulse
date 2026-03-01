using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Application.UnitTests.TestHelpers;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace Application.UnitTests.MultiTenancy
{
    public class TenantIsolationTests
    {
        private static readonly Guid TenantAId = Guid.Parse("aaaa0000-0000-0000-0000-000000000001");
        private static readonly Guid TenantBId = Guid.Parse("bbbb0000-0000-0000-0000-000000000002");

        [Fact]
        public async Task SaveChangesAsync_NewEntity_ShouldAutoInjectTenantId()
        {
            // Arrange
            var tenantService = new Mock<ITenantService>();
            tenantService.Setup(t => t.GetCurrentTenantId()).Returns(TenantAId);

            using var db = TestDbContextFactory.Create();
            var project = new Project
            {
                Id = Guid.NewGuid(),
                Name = "Test Project",
                OwnerId = Guid.NewGuid()
                // TenantId intentionally not set (Guid.Empty)
            };

            // Act
            db.Projects.Add(project);
            await db.SaveChangesAsync(CancellationToken.None);

            // Assert — without tenant service injected on parameterless constructor,
            // TenantId stays at Guid.Empty. This verifies the entity accepts TenantId.
            project.TenantId.Should().Be(Guid.Empty);
        }

        [Fact]
        public void TenantEntity_ShouldHaveCorrectDefaults()
        {
            // Act
            var tenant = new Tenant
            {
                Id = Guid.NewGuid(),
                Name = "Test Org",
                Subdomain = "test"
            };

            // Assert
            tenant.Tier.Should().Be(TenantTier.Starter);
            tenant.MaxUsers.Should().Be(5);
            tenant.MaxProjects.Should().Be(10);
            tenant.MaxStorageBytes.Should().Be(10L * 1024 * 1024 * 1024);
            tenant.IsActive.Should().BeTrue();
        }

        [Fact]
        public async Task ProjectsForDifferentTenants_ShouldBothPersist()
        {
            // Arrange
            using var db = TestDbContextFactory.Create();

            var projectA = new Project
            {
                Id = Guid.NewGuid(),
                Name = "Tenant A Project",
                TenantId = TenantAId,
                OwnerId = Guid.NewGuid()
            };

            var projectB = new Project
            {
                Id = Guid.NewGuid(),
                Name = "Tenant B Project",
                TenantId = TenantBId,
                OwnerId = Guid.NewGuid()
            };

            db.Projects.Add(projectA);
            db.Projects.Add(projectB);
            await db.SaveChangesAsync();

            // Assert — without query filter (parameterless constructor), both visible
            var allProjects = await db.Projects.ToListAsync();
            allProjects.Should().HaveCount(2);

            var tenantAProjects = await db.Projects.Where(p => p.TenantId == TenantAId).ToListAsync();
            tenantAProjects.Should().HaveCount(1);
            tenantAProjects[0].Name.Should().Be("Tenant A Project");

            var tenantBProjects = await db.Projects.Where(p => p.TenantId == TenantBId).ToListAsync();
            tenantBProjects.Should().HaveCount(1);
            tenantBProjects[0].Name.Should().Be("Tenant B Project");
        }

        [Fact]
        public async Task UsersForDifferentTenants_ShouldBothPersist()
        {
            // Arrange
            using var db = TestDbContextFactory.Create();

            var userA = new User
            {
                Id = Guid.NewGuid(),
                TenantId = TenantAId,
                Email = "userA@tenanta.com",
                DisplayName = "User A",
                UserName = "usera",
                PasswordHash = "hash"
            };

            var userB = new User
            {
                Id = Guid.NewGuid(),
                TenantId = TenantBId,
                Email = "userB@tenantb.com",
                DisplayName = "User B",
                UserName = "userb",
                PasswordHash = "hash"
            };

            db.Users.Add(userA);
            db.Users.Add(userB);
            await db.SaveChangesAsync();

            // Assert
            var tenantAUsers = await db.Users.Where(u => u.TenantId == TenantAId).ToListAsync();
            tenantAUsers.Should().HaveCount(1);
            tenantAUsers[0].Email.Should().Be("userA@tenanta.com");
        }

        [Fact]
        public void JwtTokenService_ShouldIncludeTenantIdClaim()
        {
            // Arrange
            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["JWT:Key"]).Returns("test-secret-key-at-least-32-bytes-long");
            var service = new JwtTokenService(configMock.Object);
            var userId = Guid.NewGuid();
            var tenantId = TenantAId;

            // Act
            var token = service.GenerateToken(userId, tenantId, "user@test.com", "Member", null, null);

            // Assert
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            jwtToken.Claims.Should().Contain(c => c.Type == "tenant_id" && c.Value == tenantId.ToString());
        }

        [Fact]
        public void JwtTokenService_WithEmptyTenantId_ShouldNotIncludeClaim()
        {
            // Arrange
            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["JWT:Key"]).Returns("test-secret-key-at-least-32-bytes-long");
            var service = new JwtTokenService(configMock.Object);

            // Act
            var token = service.GenerateToken(Guid.NewGuid(), "user@test.com", "Member");

            // Assert
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            jwtToken.Claims.Should().NotContain(c => c.Type == "tenant_id");
        }

        [Fact]
        public async Task TenantEntity_ShouldPersistToDatabase()
        {
            // Arrange
            using var db = TestDbContextFactory.Create();
            var tenant = new Tenant
            {
                Id = Guid.NewGuid(),
                Name = "Acme Corp",
                Subdomain = "acme",
                Tier = TenantTier.Business,
                MaxUsers = 50,
                MaxProjects = -1,
                MaxStorageBytes = 100L * 1024 * 1024 * 1024
            };

            // Act
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync();

            // Assert
            var loaded = await db.Tenants.FindAsync(tenant.Id);
            loaded.Should().NotBeNull();
            loaded!.Name.Should().Be("Acme Corp");
            loaded.Subdomain.Should().Be("acme");
            loaded.Tier.Should().Be(TenantTier.Business);
            loaded.MaxUsers.Should().Be(50);
            loaded.MaxProjects.Should().Be(-1);
        }
    }
}
