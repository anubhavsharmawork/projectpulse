using System;
using System.Linq;
using System.Threading.Tasks;
using Application.UnitTests.TestHelpers;
using Domain.Entities;
using FluentAssertions;
using global::Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Application.UnitTests.Infrastructure
{
    public class RepositoryAndAuditTests
    {
        [Fact]
        public async Task Repository_CRUD_And_Exists()
        {
            using var db = TestDbContextFactory.Create();
            var repo = new Repository<Project>(db);

            var p = new Project { Id = Guid.NewGuid(), Name = "RepoTest" };
            await repo.AddAsync(p);
            await db.SaveChangesAsync();

            (await repo.ExistsAsync(p.Id)).Should().BeTrue();

            var fetched = await repo.GetByIdAsync(p.Id);
            fetched.Should().NotBeNull();
            fetched!.Name.Should().Be("RepoTest");

            var all = await repo.GetAllAsync();
            all.Should().ContainSingle();

            var found = await repo.FindAsync(x => x.Name.Contains("Repo"));
            found.Should().ContainSingle();

            p.Name = "Updated";
            repo.Update(p);
            await db.SaveChangesAsync();

            var updated = await repo.GetByIdAsync(p.Id);
            updated!.Name.Should().Be("Updated");

            repo.Remove(p);
            await db.SaveChangesAsync();

            (await repo.ExistsAsync(p.Id)).Should().BeFalse();
        }

        [Fact]
        public async Task AuditInterceptor_CreatesAuditLogs_ForCreateUpdateDelete()
        {
            var http = new Mock<IHttpContextAccessor>();
            var userId = Guid.NewGuid();
            var ctx = new DefaultHttpContext();
            ctx.User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(new[] { new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, userId.ToString()) }));
            http.Setup(h => h.HttpContext).Returns(ctx);

            var options = new DbContextOptionsBuilder<global::Infrastructure.Persistence.AppDbContext>()
                .UseInMemoryDatabase("AuditTestDb")
                .AddInterceptors(new AuditInterceptor(http.Object))
                .Options;

            using (var db = new global::Infrastructure.Persistence.AppDbContext(options))
            {
                var proj = new Project { Id = Guid.NewGuid(), Name = "P1" };
                db.Projects.Add(proj);
                await db.SaveChangesAsync();

                var logs = db.AuditLogs.ToList();
                logs.Should().ContainSingle();
                var created = logs.Single();
                created.Action.Should().Be("Created");
                created.NewValues.Should().Contain("P1");

                // update
                proj.Name = "P2";
                db.Projects.Update(proj);
                await db.SaveChangesAsync();

                var logs2 = db.AuditLogs.Where(a => a.Action == "Updated").ToList();
                logs2.Should().ContainSingle();
                logs2[0].OldValues.Should().Contain("P1");
                logs2[0].NewValues.Should().Contain("P2");

                // delete
                db.Projects.Remove(proj);
                await db.SaveChangesAsync();

                var del = db.AuditLogs.Where(a => a.Action == "Deleted").ToList();
                del.Should().ContainSingle();
                del[0].OldValues.Should().Contain("P2");
            }
        }

        [Fact]
        public async Task AppDbContext_SaveChanges_PopulatesTenantId_WhenTenantServiceProvided()
        {
            var tenantId = Guid.NewGuid();
            var options = new DbContextOptionsBuilder<global::Infrastructure.Persistence.AppDbContext>()
                .UseInMemoryDatabase("TenantTestDb")
                .Options;

            var tenantService = new Mock<Application.Common.Interfaces.ITenantService>();
            tenantService.Setup(t => t.GetCurrentTenantId()).Returns(tenantId);

            using var db = new global::Infrastructure.Persistence.AppDbContext(options, tenantService.Object);
            var proj = new Project { Id = Guid.NewGuid(), Name = "Tst", TenantId = Guid.Empty };
            db.Projects.Add(proj);
            await db.SaveChangesAsync();

            var saved = await db.Projects.FindAsync(proj.Id);
            saved.Should().NotBeNull();
            saved!.TenantId.Should().Be(tenantId);
        }
    }
}
