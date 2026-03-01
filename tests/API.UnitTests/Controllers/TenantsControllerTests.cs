using API.Controllers;
using Application.Common.Interfaces;
using API.UnitTests.TestHelpers;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;

namespace API.UnitTests.Controllers;

public class TenantsControllerTests
{
    private readonly Mock<ITenantService> _tenantService = new();
    private readonly Mock<ILogger<TenantsController>> _logger = new();

    private TenantsController CreateController(Guid? userId = null, string? role = null)
    {
        using var db = TestDbContextFactory.Create();
        var controller = new TenantsController(db, _tenantService.Object, _logger.Object);
        var httpContext = new DefaultHttpContext();
        var claims = new List<Claim>();
        if (userId.HasValue)
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()));
        if (role != null)
            claims.Add(new Claim("system_role", role));
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return controller;
    }

    [Fact]
    public async Task GetCurrent_TenantExists_ReturnsOk()
    {
        var tenantId = Guid.NewGuid();
        _tenantService.Setup(s => s.GetCurrentTenantId()).Returns(tenantId);
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Tenants.Add(new Tenant { Id = tenantId, Name = "Test Tenant", Subdomain = "test" });
        });
        var controller = new TenantsController(db, _tenantService.Object, _logger.Object);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        var result = await controller.GetCurrent(CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetCurrent_TenantNotFound_ReturnsNotFound()
    {
        _tenantService.Setup(s => s.GetCurrentTenantId()).Returns(Guid.NewGuid());
        using var db = TestDbContextFactory.Create();
        var controller = new TenantsController(db, _tenantService.Object, _logger.Object);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        var result = await controller.GetCurrent(CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetCurrent_TenantServiceThrows_ReturnsNotFound()
    {
        _tenantService.Setup(s => s.GetCurrentTenantId()).Throws(new InvalidOperationException());
        using var db = TestDbContextFactory.Create();
        var controller = new TenantsController(db, _tenantService.Object, _logger.Object);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        var result = await controller.GetCurrent(CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task UpdateCurrent_ValidRequest_ReturnsOk()
    {
        var tenantId = Guid.NewGuid();
        _tenantService.Setup(s => s.GetCurrentTenantId()).Returns(tenantId);
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Tenants.Add(new Tenant { Id = tenantId, Name = "Test", Subdomain = "test" });
        });
        var controller = new TenantsController(db, _tenantService.Object, _logger.Object);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        var request = new UpdateTenantRequest("New Name", "{\"theme\":\"dark\"}");

        var result = await controller.UpdateCurrent(request, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task UpdateCurrent_TenantNotFound_ReturnsNotFound()
    {
        _tenantService.Setup(s => s.GetCurrentTenantId()).Returns(Guid.NewGuid());
        using var db = TestDbContextFactory.Create();
        var controller = new TenantsController(db, _tenantService.Object, _logger.Object);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        var result = await controller.UpdateCurrent(new UpdateTenantRequest(null, null), CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetUsage_ValidTenant_ReturnsOk()
    {
        var tenantId = Guid.NewGuid();
        _tenantService.Setup(s => s.GetCurrentTenantId()).Returns(tenantId);
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Tenants.Add(new Tenant { Id = tenantId, Name = "Test", Subdomain = "test" });
        });
        var controller = new TenantsController(db, _tenantService.Object, _logger.Object);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        var result = await controller.GetUsage(CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetUsage_TenantNotFound_ReturnsNotFound()
    {
        _tenantService.Setup(s => s.GetCurrentTenantId()).Returns(Guid.NewGuid());
        using var db = TestDbContextFactory.Create();
        var controller = new TenantsController(db, _tenantService.Object, _logger.Object);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        var result = await controller.GetUsage(CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetUsage_TenantServiceThrows_ReturnsNotFound()
    {
        _tenantService.Setup(s => s.GetCurrentTenantId()).Throws(new Exception("No tenant"));
        using var db = TestDbContextFactory.Create();
        var controller = new TenantsController(db, _tenantService.Object, _logger.Object);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        var result = await controller.GetUsage(CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task ListAll_ReturnsOk()
    {
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Tenants.Add(new Tenant { Id = Guid.NewGuid(), Name = "A", Subdomain = "a" });
            ctx.Tenants.Add(new Tenant { Id = Guid.NewGuid(), Name = "B", Subdomain = "b" });
        });
        var userId = Guid.NewGuid();
        var controller = new TenantsController(db, _tenantService.Object, _logger.Object);
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim("system_role", "SystemAdmin")
        }, "Test"));
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        var result = await controller.ListAll(CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Health_ReturnsOkWithStatus()
    {
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Tenants.Add(new Tenant { Id = Guid.NewGuid(), Name = "T", Subdomain = "t" });
        });
        var controller = new TenantsController(db, _tenantService.Object, _logger.Object);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        var result = await controller.Health(CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Create_ValidRequest_ReturnsCreated()
    {
        using var db = TestDbContextFactory.Create();
        var controller = new TenantsController(db, _tenantService.Object, _logger.Object);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        var request = new CreateTenantRequest("Acme Corp", null, TenantTier.Starter);

        var result = await controller.Create(request, CancellationToken.None);

        result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task Create_ShortName_ReturnsBadRequest()
    {
        using var db = TestDbContextFactory.Create();
        var controller = new TenantsController(db, _tenantService.Object, _logger.Object);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        var request = new CreateTenantRequest("A", null, TenantTier.Starter);

        var result = await controller.Create(request, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Create_EmptyName_ReturnsBadRequest()
    {
        using var db = TestDbContextFactory.Create();
        var controller = new TenantsController(db, _tenantService.Object, _logger.Object);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        var request = new CreateTenantRequest("", null, TenantTier.Business);

        var result = await controller.Create(request, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Create_BusinessTier_SetsCorrectLimits()
    {
        using var db = TestDbContextFactory.Create();
        var controller = new TenantsController(db, _tenantService.Object, _logger.Object);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        var request = new CreateTenantRequest("Business Co", null, TenantTier.Business);

        var result = await controller.Create(request, CancellationToken.None);

        result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task Create_EnterpriseTier_SetsUnlimitedLimits()
    {
        using var db = TestDbContextFactory.Create();
        var controller = new TenantsController(db, _tenantService.Object, _logger.Object);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        var request = new CreateTenantRequest("Enterprise Co", null, TenantTier.Enterprise);

        var result = await controller.Create(request, CancellationToken.None);

        result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task Create_DuplicateSubdomain_GeneratesUnique()
    {
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Tenants.Add(new Tenant { Id = Guid.NewGuid(), Name = "Existing", Subdomain = "acme-corp" });
        });
        var controller = new TenantsController(db, _tenantService.Object, _logger.Object);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        var request = new CreateTenantRequest("Acme Corp", null, TenantTier.Starter);

        var result = await controller.Create(request, CancellationToken.None);

        result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task Create_ExplicitSubdomain_UsesProvided()
    {
        using var db = TestDbContextFactory.Create();
        var controller = new TenantsController(db, _tenantService.Object, _logger.Object);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        var request = new CreateTenantRequest("Acme Corp", "my-custom-subdomain", TenantTier.Starter);

        var result = await controller.Create(request, CancellationToken.None);

        result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task Update_ExistingTenant_ReturnsOk()
    {
        var tenantId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Tenants.Add(new Tenant { Id = tenantId, Name = "Old", Subdomain = "old" });
        });
        var controller = new TenantsController(db, _tenantService.Object, _logger.Object);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        var request = new AdminUpdateTenantRequest(TenantTier.Enterprise, 100, 200, 500L, true);

        var result = await controller.Update(tenantId, request, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Update_NotFound_ReturnsNotFound()
    {
        using var db = TestDbContextFactory.Create();
        var controller = new TenantsController(db, _tenantService.Object, _logger.Object);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        var result = await controller.Update(Guid.NewGuid(), new AdminUpdateTenantRequest(null, null, null, null, null), CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Suspend_ExistingTenant_ReturnsOk()
    {
        var tenantId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Tenants.Add(new Tenant { Id = tenantId, Name = "Active", Subdomain = "active", IsActive = true });
        });
        var controller = new TenantsController(db, _tenantService.Object, _logger.Object);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        var result = await controller.Suspend(tenantId, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Suspend_NotFound_ReturnsNotFound()
    {
        using var db = TestDbContextFactory.Create();
        var controller = new TenantsController(db, _tenantService.Object, _logger.Object);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        var result = await controller.Suspend(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Activate_ExistingTenant_ReturnsOk()
    {
        var tenantId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Tenants.Add(new Tenant { Id = tenantId, Name = "Suspended", Subdomain = "suspended", IsActive = false });
        });
        var controller = new TenantsController(db, _tenantService.Object, _logger.Object);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        var result = await controller.Activate(tenantId, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Activate_NotFound_ReturnsNotFound()
    {
        using var db = TestDbContextFactory.Create();
        var controller = new TenantsController(db, _tenantService.Object, _logger.Object);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        var result = await controller.Activate(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }
}
