using System.Security.Claims;
using API.Middleware;
using API.UnitTests.TestHelpers;
using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace API.UnitTests.Middleware;

public class LegalAcceptanceMiddlewareTests
{
    private readonly Mock<ILogger<LegalAcceptanceMiddleware>> _loggerMock;
    private bool _nextCalled;

    public LegalAcceptanceMiddlewareTests()
    {
        _loggerMock = new Mock<ILogger<LegalAcceptanceMiddleware>>();
    }

    private LegalAcceptanceMiddleware CreateMiddleware(RequestDelegate? next = null)
    {
        next ??= _ => { _nextCalled = true; return Task.CompletedTask; };
        return new LegalAcceptanceMiddleware(next, _loggerMock.Object);
    }

    private static IAppDbContext CreateMockDb(
        LegalDocument? activeTerms = null,
        LegalDocument? activePrivacy = null,
        User? user = null)
    {
        var db = TestDbContextFactory.Create();

        if (activeTerms is not null)
            db.LegalDocuments.Add(activeTerms);
        if (activePrivacy is not null)
            db.LegalDocuments.Add(activePrivacy);
        if (user is not null)
            db.Users.Add(user);

        db.SaveChanges();
        return db;
    }

    private static HttpContext CreateHttpContext(string path, Guid? userId = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;

        if (userId.HasValue)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString())
            };
            context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        }

        return context;
    }

    [Fact]
    public async Task InvokeAsync_NonApiPath_ShouldCallNext()
    {
        var middleware = CreateMiddleware();
        var db = CreateMockDb();
        var context = CreateHttpContext("/health");

        await middleware.InvokeAsync(context, db);

        _nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_AuthPath_ShouldCallNext()
    {
        var middleware = CreateMiddleware();
        var db = CreateMockDb();
        var context = CreateHttpContext("/api/v1/auth/login");

        await middleware.InvokeAsync(context, db);

        _nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_LegalPath_ShouldCallNext()
    {
        var middleware = CreateMiddleware();
        var db = CreateMockDb();
        var context = CreateHttpContext("/api/v1/legal/terms");

        await middleware.InvokeAsync(context, db);

        _nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_UnauthenticatedRequest_ShouldCallNext()
    {
        var middleware = CreateMiddleware();
        var db = CreateMockDb();
        var context = CreateHttpContext("/api/v1/projects");

        await middleware.InvokeAsync(context, db);

        _nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_NoActiveLegalDocs_ShouldCallNext()
    {
        var userId = Guid.NewGuid();
        var middleware = CreateMiddleware();
        var db = CreateMockDb(user: new User
        {
            Id = userId,
            Email = "test@example.com",
            DisplayName = "Test",
            PasswordHash = "hash",
            Role = Role.Member
        });
        var context = CreateHttpContext("/api/v1/projects", userId);

        await middleware.InvokeAsync(context, db);

        _nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_UserAcceptedCurrentVersions_ShouldCallNext()
    {
        var userId = Guid.NewGuid();
        var middleware = CreateMiddleware();
        var db = CreateMockDb(
            activeTerms: new LegalDocument
            {
                Id = Guid.NewGuid(),
                DocumentType = LegalDocumentType.TermsOfService,
                Version = "1.0",
                Content = "Terms",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            activePrivacy: new LegalDocument
            {
                Id = Guid.NewGuid(),
                DocumentType = LegalDocumentType.PrivacyPolicy,
                Version = "1.0",
                Content = "Privacy",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            user: new User
            {
                Id = userId,
                Email = "test@example.com",
                DisplayName = "Test",
                PasswordHash = "hash",
                Role = Role.Member,
                TermsVersion = "1.0",
                PrivacyVersion = "1.0"
            });
        var context = CreateHttpContext("/api/v1/projects", userId);

        await middleware.InvokeAsync(context, db);

        _nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_UserNotAcceptedTerms_ShouldReturn451()
    {
        var userId = Guid.NewGuid();
        var middleware = CreateMiddleware();
        var db = CreateMockDb(
            activeTerms: new LegalDocument
            {
                Id = Guid.NewGuid(),
                DocumentType = LegalDocumentType.TermsOfService,
                Version = "2.0",
                Content = "Terms v2",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            user: new User
            {
                Id = userId,
                Email = "test@example.com",
                DisplayName = "Test",
                PasswordHash = "hash",
                Role = Role.Member,
                TermsVersion = "1.0"
            });
        var context = CreateHttpContext("/api/v1/projects", userId);

        await middleware.InvokeAsync(context, db);

        _nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(451);
    }

    [Fact]
    public async Task InvokeAsync_UserNotAcceptedPrivacy_ShouldReturn451()
    {
        var userId = Guid.NewGuid();
        var middleware = CreateMiddleware();
        var db = CreateMockDb(
            activePrivacy: new LegalDocument
            {
                Id = Guid.NewGuid(),
                DocumentType = LegalDocumentType.PrivacyPolicy,
                Version = "2.0",
                Content = "Privacy v2",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            user: new User
            {
                Id = userId,
                Email = "test@example.com",
                DisplayName = "Test",
                PasswordHash = "hash",
                Role = Role.Member,
                PrivacyVersion = "1.0"
            });
        var context = CreateHttpContext("/api/v1/projects", userId);

        await middleware.InvokeAsync(context, db);

        _nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(451);
    }

    [Fact]
    public async Task InvokeAsync_InvalidUserIdClaim_ShouldCallNext()
    {
        var middleware = CreateMiddleware();
        var db = CreateMockDb(
            activeTerms: new LegalDocument
            {
                Id = Guid.NewGuid(),
                DocumentType = LegalDocumentType.TermsOfService,
                Version = "1.0",
                Content = "Terms",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/v1/projects";
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "not-a-guid") };
        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));

        await middleware.InvokeAsync(context, db);

        _nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_UserNotFoundInDb_ShouldCallNext()
    {
        var userId = Guid.NewGuid();
        var middleware = CreateMiddleware();
        var db = CreateMockDb(
            activeTerms: new LegalDocument
            {
                Id = Guid.NewGuid(),
                DocumentType = LegalDocumentType.TermsOfService,
                Version = "1.0",
                Content = "Terms",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
        var context = CreateHttpContext("/api/v1/projects", userId);

        await middleware.InvokeAsync(context, db);

        _nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_HubsPath_ShouldCallNext()
    {
        var middleware = CreateMiddleware();
        var db = CreateMockDb();
        var context = CreateHttpContext("/api/v1/hubs/project");

        await middleware.InvokeAsync(context, db);

        _nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_HealthPath_ShouldCallNext()
    {
        var middleware = CreateMiddleware();
        var db = CreateMockDb();
        var context = CreateHttpContext("/api/v1/health/status");

        await middleware.InvokeAsync(context, db);

        _nextCalled.Should().BeTrue();
    }
}
