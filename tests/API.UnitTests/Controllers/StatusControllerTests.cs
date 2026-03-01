using API.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace API.UnitTests.Controllers;

public class StatusControllerTests
{
    [Fact]
    public void Root_ShouldReturnOkWithStatus()
    {
        var controller = new StatusController();

        var result = controller.Root();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }
}
