using System.Security.Claims;
using API.Middleware;
using Application.Common.Interfaces;
using Domain.Constants;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace API.UnitTests.Middleware;

public class TenantMiddlewareTests
{
    private readonly Mock<ILogger<TenantMiddleware>> _loggerMock;
    private readonly Mock<ITenantService> _tenantServiceMock;
    private bool _nextCalled;

    public TenantMiddlewareTests()
    {
        _loggerMock = new Mock<ILogger<TenantMiddleware>>();
        _tenantServiceMock = new Mock<ITenantService>();
    }

    private TenantMiddleware CreateMiddleware(RequestDelegate? next = null)
    {
        next ??= _ => { _nextCalled = true; return Task.CompletedTask; };
        return new TenantMiddleware(next, _loggerMock.Object);
    }

    private static HttpContext CreateHttpContext(string path, ClaimsPrincipal? user = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        if (user is not null) context.User = user;
        return context;
    }

    private static ClaimsPrincipal CreateUserWithTenantClaim(Guid tenantId)
    {
        var claims = new[] { new Claim("tenant_id", tenantId.ToString()) };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
    }

    [Fact]
    public async Task InvokeAsync_NonApiPath_ShouldCallNextWithoutSettingTenant()
    {
        var middleware = CreateMiddleware();
        var context = CreateHttpContext("/health");

        await middleware.InvokeAsync(context, _tenantServiceMock.Object);

        _nextCalled.Should().BeTrue();
        context.Items.Should().NotContainKey("TenantId");
    }

    [Fact]
    public async Task InvokeAsync_AuthPath_ShouldSetDefaultTenantId()
    {
        var middleware = CreateMiddleware();
        var context = CreateHttpContext("/api/v1/auth/login");

        await middleware.InvokeAsync(context, _tenantServiceMock.Object);

        _nextCalled.Should().BeTrue();
        context.Items["TenantId"].Should().Be(TenantConstants.DefaultTenantId);
    }

    [Fact]
    public async Task InvokeAsync_AuthPathWithJwtTenant_ShouldUseJwtTenant()
    {
        var tenantId = Guid.NewGuid();
        var middleware = CreateMiddleware();
        var context = CreateHttpContext("/api/v1/auth/login", CreateUserWithTenantClaim(tenantId));

        await middleware.InvokeAsync(context, _tenantServiceMock.Object);

        _nextCalled.Should().BeTrue();
        context.Items["TenantId"].Should().Be(tenantId);
    }

    [Fact]
    public async Task InvokeAsync_SystemAdmin_ShouldAllowHeaderOverride()
    {
        var headerTenantId = Guid.NewGuid();
        _tenantServiceMock.Setup(t => t.IsSystemAdminContext()).Returns(true);
        var middleware = CreateMiddleware();
        var context = CreateHttpContext("/api/v1/projects");
        context.Request.Headers["X-Tenant-Id"] = headerTenantId.ToString();

        await middleware.InvokeAsync(context, _tenantServiceMock.Object);

        _nextCalled.Should().BeTrue();
        context.Items["TenantId"].Should().Be(headerTenantId);
    }

    [Fact]
    public async Task InvokeAsync_SystemAdminNoHeader_ShouldFallbackToJwt()
    {
        var jwtTenantId = Guid.NewGuid();
        _tenantServiceMock.Setup(t => t.IsSystemAdminContext()).Returns(true);
        var middleware = CreateMiddleware();
        var context = CreateHttpContext("/api/v1/projects", CreateUserWithTenantClaim(jwtTenantId));

        await middleware.InvokeAsync(context, _tenantServiceMock.Object);

        context.Items["TenantId"].Should().Be(jwtTenantId);
    }

    [Fact]
    public async Task InvokeAsync_SystemAdminNoHeaderNoJwt_ShouldUseDefault()
    {
        _tenantServiceMock.Setup(t => t.IsSystemAdminContext()).Returns(true);
        var middleware = CreateMiddleware();
        var context = CreateHttpContext("/api/v1/projects");

        await middleware.InvokeAsync(context, _tenantServiceMock.Object);

        context.Items["TenantId"].Should().Be(TenantConstants.DefaultTenantId);
    }

    [Fact]
    public async Task InvokeAsync_AuthenticatedWithJwtTenant_ShouldUseJwtTenant()
    {
        var tenantId = Guid.NewGuid();
        _tenantServiceMock.Setup(t => t.IsSystemAdminContext()).Returns(false);
        var middleware = CreateMiddleware();
        var context = CreateHttpContext("/api/v1/projects", CreateUserWithTenantClaim(tenantId));

        await middleware.InvokeAsync(context, _tenantServiceMock.Object);

        context.Items["TenantId"].Should().Be(tenantId);
    }

    [Fact]
    public async Task InvokeAsync_NoJwtTenant_ShouldFallbackToDefault()
    {
        _tenantServiceMock.Setup(t => t.IsSystemAdminContext()).Returns(false);
        var middleware = CreateMiddleware();
        var context = CreateHttpContext("/api/v1/projects");

        await middleware.InvokeAsync(context, _tenantServiceMock.Object);

        context.Items["TenantId"].Should().Be(TenantConstants.DefaultTenantId);
    }

    [Fact]
    public void DefaultTenantId_ShouldMatchTenantConstants()
    {
        TenantMiddleware.DefaultTenantId.Should().Be(TenantConstants.DefaultTenantId);
    }
}
