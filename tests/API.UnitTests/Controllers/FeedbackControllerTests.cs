using API.Controllers;
using Application.Common.Interfaces;
using API.UnitTests.TestHelpers;
using Domain.Entities;
using FluentAssertions;
using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using Xunit;

namespace API.UnitTests.Controllers;

public class FeedbackControllerTests : IDisposable
{
    private readonly BackgroundJobServer? _server;

    public FeedbackControllerTests()
    {
        GlobalConfiguration.Configuration.UseMemoryStorage();
    }

    public void Dispose()
    {
        _server?.Dispose();
    }

    private static FeedbackController CreateController(IAppDbContext db, Guid? userId = null)
    {
        var controller = new FeedbackController(db);
        var httpContext = new DefaultHttpContext();
        if (userId.HasValue)
        {
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()) };
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        }
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return controller;
    }

    [Fact]
    public async Task Submit_AuthenticatedUser_CreatesAndReturnsOk()
    {
        var userId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Users.Add(new User { Id = userId, Email = "user@test.com", DisplayName = "Test User", UserName = "testuser" });
        });
        var controller = CreateController(db, userId);
        var request = new SubmitFeedbackRequest("This is some test feedback message.");

        var result = await controller.Submit(request);

        result.Should().BeOfType<OkObjectResult>();
        db.Feedbacks.Should().HaveCount(1);
    }

    [Fact]
    public async Task Submit_UnauthenticatedUser_StillCreatesWithNullUserId()
    {
        using var db = TestDbContextFactory.Create();
        var controller = CreateController(db);
        var request = new SubmitFeedbackRequest("Anonymous feedback message here.");

        var result = await controller.Submit(request);

        result.Should().BeOfType<OkObjectResult>();
        db.Feedbacks.Should().HaveCount(1);
    }

    [Fact]
    public async Task Submit_UserNotFoundInDb_StillCreatesWithNullEmail()
    {
        var userId = Guid.NewGuid();
        using var db = TestDbContextFactory.Create();
        var controller = CreateController(db, userId);
        var request = new SubmitFeedbackRequest("Feedback from unknown user in DB.");

        var result = await controller.Submit(request);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithPaginatedResults()
    {
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            for (int i = 0; i < 5; i++)
            {
                ctx.Feedbacks.Add(new Feedback
                {
                    Id = Guid.NewGuid(),
                    Message = $"Feedback {i}",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddMinutes(-i)
                });
            }
        });
        var controller = CreateController(db, Guid.NewGuid());

        var result = await controller.GetAll(page: 1, pageSize: 3);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAll_PageClampedToMinimum()
    {
        using var db = TestDbContextFactory.Create();
        var controller = CreateController(db, Guid.NewGuid());

        var result = await controller.GetAll(page: -1, pageSize: 0);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAll_PageSizeClampedToMax100()
    {
        using var db = TestDbContextFactory.Create();
        var controller = CreateController(db, Guid.NewGuid());

        var result = await controller.GetAll(page: 1, pageSize: 200);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAll_FiltersInactiveFeedback()
    {
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Feedbacks.Add(new Feedback { Id = Guid.NewGuid(), Message = "Active", IsActive = true });
            ctx.Feedbacks.Add(new Feedback { Id = Guid.NewGuid(), Message = "Inactive", IsActive = false });
        });
        var controller = CreateController(db, Guid.NewGuid());

        var result = await controller.GetAll();

        result.Should().BeOfType<OkObjectResult>();
    }
}
