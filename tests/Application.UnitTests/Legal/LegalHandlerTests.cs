using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Legal.Commands;
using Application.Legal.Queries;
using Application.UnitTests.TestHelpers;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using System.Security.Claims;
using Xunit;

namespace Application.UnitTests.Legal;

public class AcceptLegalHandlerTests
{
    private static Mock<IHttpContextAccessor> CreateHttpAccessor(Guid? userId = null)
    {
        var mock = new Mock<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext();
        if (userId.HasValue)
        {
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()) }, "Test"));
        }
        mock.Setup(x => x.HttpContext).Returns(httpContext);
        return mock;
    }

    [Fact]
    public async Task Handle_ValidAcceptance_UpdatesUser()
    {
        var userId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Users.Add(new User { Id = userId, Email = "test@test.com", UserName = "test" });
            ctx.LegalDocuments.Add(new LegalDocument { Id = Guid.NewGuid(), DocumentType = LegalDocumentType.TermsOfService, Version = "1.0", IsActive = true, Content = "Terms", EffectiveDate = DateTime.UtcNow });
            ctx.LegalDocuments.Add(new LegalDocument { Id = Guid.NewGuid(), DocumentType = LegalDocumentType.PrivacyPolicy, Version = "1.0", IsActive = true, Content = "Privacy", EffectiveDate = DateTime.UtcNow });
        });
        var handler = new AcceptLegalHandler(db, CreateHttpAccessor(userId).Object);

        await handler.Handle(new AcceptLegalCommand("1.0", "1.0"), CancellationToken.None);

        var user = db.Users.First(u => u.Id == userId);
        user.TermsVersion.Should().Be("1.0");
        user.PrivacyVersion.Should().Be("1.0");
        user.TermsAcceptedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_WrongTermsVersion_Throws()
    {
        var userId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Users.Add(new User { Id = userId, Email = "test@test.com", UserName = "test" });
            ctx.LegalDocuments.Add(new LegalDocument { Id = Guid.NewGuid(), DocumentType = LegalDocumentType.TermsOfService, Version = "2.0", IsActive = true, Content = "Terms", EffectiveDate = DateTime.UtcNow });
        });
        var handler = new AcceptLegalHandler(db, CreateHttpAccessor(userId).Object);

        var act = async () => await handler.Handle(new AcceptLegalCommand("1.0", "1.0"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*not the current active version*");
    }

    [Fact]
    public async Task Handle_WrongPrivacyVersion_Throws()
    {
        var userId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Users.Add(new User { Id = userId, Email = "test@test.com", UserName = "test" });
            ctx.LegalDocuments.Add(new LegalDocument { Id = Guid.NewGuid(), DocumentType = LegalDocumentType.TermsOfService, Version = "1.0", IsActive = true, Content = "Terms", EffectiveDate = DateTime.UtcNow });
            ctx.LegalDocuments.Add(new LegalDocument { Id = Guid.NewGuid(), DocumentType = LegalDocumentType.PrivacyPolicy, Version = "2.0", IsActive = true, Content = "Privacy", EffectiveDate = DateTime.UtcNow });
        });
        var handler = new AcceptLegalHandler(db, CreateHttpAccessor(userId).Object);

        var act = async () => await handler.Handle(new AcceptLegalCommand("1.0", "1.0"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Privacy version*");
    }

    [Fact]
    public async Task Handle_UserNotFound_Throws()
    {
        using var db = TestDbContextFactory.Create();
        var handler = new AcceptLegalHandler(db, CreateHttpAccessor(Guid.NewGuid()).Object);

        var act = async () => await handler.Handle(new AcceptLegalCommand("1.0", "1.0"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*User not found*");
    }

    [Fact]
    public async Task Handle_NoUserId_ThrowsUnauthorized()
    {
        using var db = TestDbContextFactory.Create();
        var handler = new AcceptLegalHandler(db, CreateHttpAccessor().Object);

        var act = async () => await handler.Handle(new AcceptLegalCommand("1.0", "1.0"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }
}

public class GetLegalDocumentHandlerTests
{
    [Fact]
    public async Task Handle_ActiveDocumentExists_ReturnsDto()
    {
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.LegalDocuments.Add(new LegalDocument { Id = Guid.NewGuid(), DocumentType = LegalDocumentType.TermsOfService, Version = "1.0", IsActive = true, Content = "Terms content", EffectiveDate = DateTime.UtcNow });
        });
        var handler = new GetLegalDocumentHandler(db);

        var result = await handler.Handle(new GetLegalDocumentQuery(LegalDocumentType.TermsOfService), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Version.Should().Be("1.0");
        result.Content.Should().Be("Terms content");
    }

    [Fact]
    public async Task Handle_NoActiveDocument_ReturnsNull()
    {
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.LegalDocuments.Add(new LegalDocument { Id = Guid.NewGuid(), DocumentType = LegalDocumentType.TermsOfService, Version = "1.0", IsActive = false, Content = "Old", EffectiveDate = DateTime.UtcNow });
        });
        var handler = new GetLegalDocumentHandler(db);

        var result = await handler.Handle(new GetLegalDocumentQuery(LegalDocumentType.TermsOfService), CancellationToken.None);

        result.Should().BeNull();
    }
}

public class GetLegalStatusHandlerTests
{
    private static Mock<IHttpContextAccessor> CreateHttpAccessor(Guid userId)
    {
        var mock = new Mock<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) }, "Test"));
        mock.Setup(x => x.HttpContext).Returns(httpContext);
        return mock;
    }

    [Fact]
    public async Task Handle_UserAcceptedCurrentVersions_NoAcceptanceRequired()
    {
        var userId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Users.Add(new User { Id = userId, Email = "test@test.com", UserName = "test", TermsVersion = "1.0", PrivacyVersion = "1.0" });
            ctx.LegalDocuments.Add(new LegalDocument { Id = Guid.NewGuid(), DocumentType = LegalDocumentType.TermsOfService, Version = "1.0", IsActive = true, Content = "Terms", EffectiveDate = DateTime.UtcNow });
            ctx.LegalDocuments.Add(new LegalDocument { Id = Guid.NewGuid(), DocumentType = LegalDocumentType.PrivacyPolicy, Version = "1.0", IsActive = true, Content = "Privacy", EffectiveDate = DateTime.UtcNow });
        });
        var handler = new GetLegalStatusHandler(db, CreateHttpAccessor(userId).Object);

        var result = await handler.Handle(new GetLegalStatusQuery(), CancellationToken.None);

        result.RequiresAcceptance.Should().BeFalse();
        result.TermsAccepted.Should().BeTrue();
        result.PrivacyAccepted.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_UserHasOutdatedVersion_RequiresAcceptance()
    {
        var userId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Users.Add(new User { Id = userId, Email = "test@test.com", UserName = "test", TermsVersion = "1.0", PrivacyVersion = "1.0" });
            ctx.LegalDocuments.Add(new LegalDocument { Id = Guid.NewGuid(), DocumentType = LegalDocumentType.TermsOfService, Version = "2.0", IsActive = true, Content = "New Terms", EffectiveDate = DateTime.UtcNow });
            ctx.LegalDocuments.Add(new LegalDocument { Id = Guid.NewGuid(), DocumentType = LegalDocumentType.PrivacyPolicy, Version = "1.0", IsActive = true, Content = "Privacy", EffectiveDate = DateTime.UtcNow });
        });
        var handler = new GetLegalStatusHandler(db, CreateHttpAccessor(userId).Object);

        var result = await handler.Handle(new GetLegalStatusQuery(), CancellationToken.None);

        result.RequiresAcceptance.Should().BeTrue();
        result.TermsAccepted.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_NoLegalDocuments_NoAcceptanceRequired()
    {
        var userId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Users.Add(new User { Id = userId, Email = "test@test.com", UserName = "test" });
        });
        var handler = new GetLegalStatusHandler(db, CreateHttpAccessor(userId).Object);

        var result = await handler.Handle(new GetLegalStatusQuery(), CancellationToken.None);

        result.RequiresAcceptance.Should().BeFalse();
    }
}
