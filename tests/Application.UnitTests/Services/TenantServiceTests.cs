using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using System.Linq;
using global::Domain.Entities;
using global::Domain.Enums;
using Infrastructure.Persistence;
using FluentAssertions;
using Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using Application.UnitTests.TestHelpers;

namespace Application.UnitTests.Services;

public class TenantServiceTests
{
    private static IServiceProvider MakeProvider(AppDbContext db)
    {
        var services = new ServiceCollection();
        // Register the AppDbContext as scoped but return the provided instance for tests
        services.AddScoped(_ => db);
        return services.BuildServiceProvider();
    }

    [Fact]
    public void GetCurrentTenantId_UsesExplicitOverride()
    {
        var http = new DefaultHttpContext();
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(a => a.HttpContext).Returns(http);

        var provider = MakeProvider(TestDbContextFactory.Create());
        var svc = new TenantService(accessor.Object, provider);

        var id = Guid.NewGuid();
        svc.SetTenantId(id);
        svc.GetCurrentTenantId().Should().Be(id);
    }

    [Fact]
    public void GetCurrentTenantId_FromHttpContextItems()
    {
        var http = new DefaultHttpContext();
        var id = Guid.NewGuid();
        http.Items["TenantId"] = id;
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(a => a.HttpContext).Returns(http);

        var provider = MakeProvider(TestDbContextFactory.Create());
        var svc = new TenantService(accessor.Object, provider);

        svc.GetCurrentTenantId().Should().Be(id);
    }

    [Fact]
    public void GetCurrentTenantId_FromClaim()
    {
        var http = new DefaultHttpContext();
        var id = Guid.NewGuid();
        http.User = new System.Security.Claims.ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("tenant_id", id.ToString()) }));
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(a => a.HttpContext).Returns(http);

        var provider = MakeProvider(TestDbContextFactory.Create());
        var svc = new TenantService(accessor.Object, provider);

        svc.GetCurrentTenantId().Should().Be(id);
    }

    [Fact]
    public async Task GetCurrentTenantAsync_Throws_WhenNotFoundOrInactive()
    {
        var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            var t = new Tenant { Id = Guid.NewGuid(), Name = "T1", IsActive = false };
            ctx.Tenants.Add(t);
        });

        var http = new DefaultHttpContext();
        http.Items["TenantId"] = db.Tenants.First().Id;
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(a => a.HttpContext).Returns(http);

        var provider = MakeProvider(db);
        var svc = new TenantService(accessor.Object, provider);

        await Assert.ThrowsAsync<Application.Common.Exceptions.TenantNotFoundException>(() => svc.GetCurrentTenantAsync());
    }

    [Fact]
    public async Task HasFeatureAsync_RespectsTierMapping()
    {
        var tenantId = Guid.NewGuid();
        var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Tenants.Add(new Tenant { Id = tenantId, Name = "T", IsActive = true, Tier = global::Domain.Enums.TenantTier.Business, MaxUsers = 10, MaxProjects = 5, MaxStorageBytes = 1024 * 1024 * 10 });
        });

        var http = new DefaultHttpContext();
        http.Items["TenantId"] = tenantId;
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(a => a.HttpContext).Returns(http);

        var provider = MakeProvider(db);
        var svc = new TenantService(accessor.Object, provider);

        (await svc.HasFeatureAsync("custom-workflows")).Should().BeTrue();
        (await svc.HasFeatureAsync("sso")).Should().BeFalse();
        (await svc.HasFeatureAsync("unknown-feature")).Should().BeTrue();
    }

    [Fact]
    public async Task IsWithinLimitAsync_WorksForKnownResources()
    {
        var tenantId = Guid.NewGuid();
        var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Tenants.Add(new Tenant { Id = tenantId, Name = "T", IsActive = true, Tier = global::Domain.Enums.TenantTier.Business, MaxUsers = 3, MaxProjects = 2, MaxStorageBytes = 1024 * 1024 * 5 });
        });

        var http = new DefaultHttpContext();
        http.Items["TenantId"] = tenantId;
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(a => a.HttpContext).Returns(http);

        var provider = MakeProvider(db);
        var svc = new TenantService(accessor.Object, provider);

        (await svc.IsWithinLimitAsync("users", 2)).Should().BeTrue();
        (await svc.IsWithinLimitAsync("users", 3)).Should().BeFalse();
        (await svc.IsWithinLimitAsync("projects", 1)).Should().BeTrue();
        (await svc.IsWithinLimitAsync("storage", 6)).Should().BeFalse();
    }

    [Fact]
    public void IsSystemAdminContext_DetectsSystemAdminClaimOrRole()
    {
        var http = new DefaultHttpContext();
        http.User = new System.Security.Claims.ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("system_role", "SystemAdmin") }));
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(a => a.HttpContext).Returns(http);

        var provider = MakeProvider(TestDbContextFactory.Create());
        var svc = new TenantService(accessor.Object, provider);

        svc.IsSystemAdminContext().Should().BeTrue();

        // Role check
        http.User = new System.Security.Claims.ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "Admin") }));
        svc.IsSystemAdminContext().Should().BeTrue();
    }

    [Fact]
    public async Task GetAllTenantsAsync_RequiresSystemAdmin()
    {
        var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Tenants.Add(new Tenant { Id = Guid.NewGuid(), Name = "A", IsActive = true });
            ctx.Tenants.Add(new Tenant { Id = Guid.NewGuid(), Name = "B", IsActive = true });
        });

        var http = new DefaultHttpContext();
        http.User = new System.Security.Claims.ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("system_role", "SystemAdmin") }));
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(a => a.HttpContext).Returns(http);

        var provider = MakeProvider(db);
        var svc = new TenantService(accessor.Object, provider);

        var list = await svc.GetAllTenantsAsync();
        list.Count.Should().Be(2);
    }
}
