using API.Controllers;
using FluentAssertions;
using API.UnitTests.TestHelpers;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Xunit;

namespace API.UnitTests.Controllers;

public class OpsControllerTests
{
    [Fact]
    public async Task Init_NoSecret_ReturnsOk()
    {
        using var db = TestDbContextFactory.Create();
        var logger = new LoggerFactory().CreateLogger<OpsController>();
        Environment.SetEnvironmentVariable("INIT_SECRET", null);

        var controller = new OpsController();

        var result = await controller.Init(db, logger, null);

        result.Should().BeOfType<OkObjectResult>();
        var ok = result as OkObjectResult;
        ok!.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task Init_WithSecretMismatch_ReturnsNotFound()
    {
        using var db = TestDbContextFactory.Create();
        var logger = new LoggerFactory().CreateLogger<OpsController>();
        Environment.SetEnvironmentVariable("INIT_SECRET", "expected");

        var controller = new OpsController();

        var result = await controller.Init(db, logger, "wrong");

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Init_WithSecretMatch_ReturnsOk()
    {
        using var db = TestDbContextFactory.Create();
        var logger = new LoggerFactory().CreateLogger<OpsController>();
        Environment.SetEnvironmentVariable("INIT_SECRET", "topsecret");

        var controller = new OpsController();

        var result = await controller.Init(db, logger, "topsecret");

        result.Should().BeOfType<OkObjectResult>();
    }
}
