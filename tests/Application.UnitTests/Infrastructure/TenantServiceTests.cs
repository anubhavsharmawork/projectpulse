using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Application.UnitTests.TestHelpers;
using global::Domain.Entities;
using global::Domain.Enums;
using FluentAssertions;
using Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Application.UnitTests.Infrastructure
{
    public class TenantServiceTests
    {
        private static IServiceProvider BuildServiceProvider(Action<global::Infrastructure.Persistence.AppDbContext>? seeding = null)
        {
            var services = new ServiceCollection();
            var dbName = Guid.NewGuid().ToString();
            services.AddDbContext<global::Infrastructure.Persistence.AppDbContext>(options =>
            {
                options.UseInMemoryDatabase(dbName);
            });
            var sp = services.BuildServiceProvider();

            // seed
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<global::Infrastructure.Persistence.AppDbContext>();
            seeding?.Invoke(db);
            db.SaveChanges();

            return sp;
        }

        [Fact]
        public void GetCurrentTenantId_ExplicitSet_Returns()
        {
            var http = new Mock<IHttpContextAccessor>();
            http.Setup(x => x.HttpContext).Returns((HttpContext)null);
            var sp = BuildServiceProvider();
            var svc = new TenantService(http.Object, sp);

            var tid = Guid.NewGuid();
            svc.SetTenantId(tid);
            svc.GetCurrentTenantId().Should().Be(tid);
        }

        [Fact]
        public void GetCurrentTenantId_FromHttpContextItems_Returns()
        {
            var mockHttp = new DefaultHttpContext();
            var tid = Guid.NewGuid();
            mockHttp.Items["TenantId"] = tid;
            var http = new Mock<IHttpContextAccessor>();
            http.Setup(x => x.HttpContext).Returns(mockHttp);

            var sp = BuildServiceProvider();
            var svc = new TenantService(http.Object, sp);

            svc.GetCurrentTenantId().Should().Be(tid);
        }

        [Fact]
        public void GetCurrentTenantId_FromClaim_Returns()
        {
            var mockHttp = new DefaultHttpContext();
            var tid = Guid.NewGuid();
            mockHttp.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("tenant_id", tid.ToString()) }));
            var http = new Mock<IHttpContextAccessor>();
            http.Setup(x => x.HttpContext).Returns(mockHttp);

            var sp = BuildServiceProvider();
            var svc = new TenantService(http.Object, sp);

            svc.GetCurrentTenantId().Should().Be(tid);
        }

        [Fact]
        public async Task GetCurrentTenantAsync_ReturnsTenant()
        {
            var tid = Guid.NewGuid();
            var sp = BuildServiceProvider(db =>
            {
                db.Tenants.Add(new Tenant { Id = tid, Name = "T", IsActive = true, MaxUsers = 10, MaxProjects = 5, MaxStorageBytes = 1024 * 1024 * 10 });
            });

            var mockHttp = new DefaultHttpContext();
            mockHttp.Items["TenantId"] = tid;
            var http = new Mock<IHttpContextAccessor>();
            http.Setup(x => x.HttpContext).Returns(mockHttp);

            var svc = new TenantService(http.Object, sp);
            var t = await svc.GetCurrentTenantAsync();
            t.Id.Should().Be(tid);
        }

        [Fact]
        public async Task HasFeatureAsync_UnknownFeature_ReturnsTrue()
        {
            var tid = Guid.NewGuid();
            var sp = BuildServiceProvider(db =>
            {
                db.Tenants.Add(new Tenant { Id = tid, Name = "T", IsActive = true, Tier = global::Domain.Enums.TenantTier.Starter });
            });

            var mockHttp = new DefaultHttpContext();
            mockHttp.Items["TenantId"] = tid;
            var http = new Mock<IHttpContextAccessor>();
            http.Setup(x => x.HttpContext).Returns(mockHttp);

            var svc = new TenantService(http.Object, sp);
            (await svc.HasFeatureAsync("some-unknown-feature")).Should().BeTrue();
        }

        [Fact]
        public async Task IsWithinLimitAsync_Users_ReturnsCorrect()
        {
            var tid = Guid.NewGuid();
            var sp = BuildServiceProvider(db =>
            {
                db.Tenants.Add(new Tenant { Id = tid, Name = "T", IsActive = true, MaxUsers = 3, MaxProjects = 5, MaxStorageBytes = 1024 * 1024 * 10 });
            });

            var mockHttp = new DefaultHttpContext();
            mockHttp.Items["TenantId"] = tid;
            var http = new Mock<IHttpContextAccessor>();
            http.Setup(x => x.HttpContext).Returns(mockHttp);

            var svc = new TenantService(http.Object, sp);
            (await svc.IsWithinLimitAsync("users", 2)).Should().BeTrue();
            (await svc.IsWithinLimitAsync("users", 3)).Should().BeFalse();
        }

        [Fact]
        public async Task GetAllTenantsAsync_RequiresSystemAdmin()
        {
            var tid = Guid.NewGuid();
            var sp = BuildServiceProvider(db =>
            {
                db.Tenants.Add(new Tenant { Id = tid, Name = "T", IsActive = true });
            });

            var mockHttp = new DefaultHttpContext();
            var http = new Mock<IHttpContextAccessor>();
            http.Setup(x => x.HttpContext).Returns(mockHttp);

            var svc = new TenantService(http.Object, sp);
            await Assert.ThrowsAsync<Application.Common.Exceptions.UnauthorizedTenantAccessException>(() => svc.GetAllTenantsAsync());
        }

        [Fact]
        public async Task GetAllTenantsAsync_AsAdmin_ReturnsList()
        {
            var tid = Guid.NewGuid();
            var sp = BuildServiceProvider(db =>
            {
                db.Tenants.Add(new Tenant { Id = tid, Name = "T1", IsActive = true });
                db.Tenants.Add(new Tenant { Id = Guid.NewGuid(), Name = "T2", IsActive = true });
            });

            var mockHttp = new DefaultHttpContext();
            mockHttp.User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(new[] { new System.Security.Claims.Claim("system_role", "SystemAdmin") }));
            var http = new Mock<IHttpContextAccessor>();
            http.Setup(x => x.HttpContext).Returns(mockHttp);

            var svc = new TenantService(http.Object, sp);
            var list = await svc.GetAllTenantsAsync();
            list.Count.Should().BeGreaterOrEqualTo(2);
        }
    }
}
