using System.Security.Claims;
using API;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace API.UnitTests;

public class ProgramTests
{
    [Fact]
    public void HangfireAdminAuthorizationFilter_ReturnsTrue_ForAuthenticatedAdmin()
    {
        // Arrange
        var filter = new HangfireAdminAuthorizationFilter();
        var ctx = new DefaultHttpContext();
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "Admin") }, "Test");
        ctx.User = new ClaimsPrincipal(identity);

        // Act
        var result = filter.AuthorizeHttp(ctx);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void HangfireAdminAuthorizationFilter_ReturnsFalse_ForUnauthenticatedUser()
    {
        var filter = new HangfireAdminAuthorizationFilter();
        var ctx = new DefaultHttpContext();
        // no identity
        ctx.User = new ClaimsPrincipal(new ClaimsIdentity());

        var result = filter.AuthorizeHttp(ctx);

        result.Should().BeFalse();
    }

    [Fact]
    public void HangfireAdminAuthorizationFilter_ReturnsFalse_ForAuthenticatedNonAdmin()
    {
        var filter = new HangfireAdminAuthorizationFilter();
        var ctx = new DefaultHttpContext();
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "Member") }, "Test");
        ctx.User = new ClaimsPrincipal(identity);

        var result = filter.AuthorizeHttp(ctx);

        result.Should().BeFalse();
    }

    [Fact]
    public void HangfireAdminAuthorizationFilter_ReturnsFalse_ForNullContext()
    {
        var filter = new HangfireAdminAuthorizationFilter();
        var result = filter.AuthorizeHttp(null);
        result.Should().BeFalse();
    }
}
