using API.Controllers;
using Application.CustomFields.Commands;
using Application.CustomFields.Queries;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace API.UnitTests.Controllers;

public class CustomFieldsControllerTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ILogger<CustomFieldsController>> _loggerMock;
    private readonly CustomFieldsController _controller;

    public CustomFieldsControllerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _loggerMock = new Mock<ILogger<CustomFieldsController>>();
        _controller = new CustomFieldsController(_mediatorMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetByDomain_ValidDomain_ShouldReturnOk()
    {
        var fields = new List<CustomFieldDto>();
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetCustomFieldsByDomainQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(fields);

        var result = await _controller.GetByDomain("IT");

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetByDomain_WithEntityType_ShouldReturnOk()
    {
        var fields = new List<CustomFieldDto>();
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetCustomFieldsByDomainQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(fields);

        var result = await _controller.GetByDomain("IT", "Epic");

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetByDomain_Exception_ShouldReturn500()
    {
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetCustomFieldsByDomainQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DB error"));

        var result = await _controller.GetByDomain("IT");

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task GetValuesForEntity_ValidEntity_ShouldReturnOk()
    {
        var values = new List<CustomFieldValueDto>();
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetCustomFieldValuesForEntityQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(values);

        var result = await _controller.GetValuesForEntity(Guid.NewGuid());

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetValuesForEntity_Exception_ShouldReturn500()
    {
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetCustomFieldValuesForEntityQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DB error"));

        var result = await _controller.GetValuesForEntity(Guid.NewGuid());

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task SaveValue_ValidCommand_ShouldReturnOk()
    {
        var id = Guid.NewGuid();
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<SaveCustomFieldValueCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(id);
        var cmd = new SaveCustomFieldValueCommand(Guid.NewGuid(), Guid.NewGuid(), "test-value");

        var result = await _controller.SaveValue(cmd);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task SaveValue_Exception_ShouldReturn500()
    {
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<SaveCustomFieldValueCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Save failed"));
        var cmd = new SaveCustomFieldValueCommand(Guid.NewGuid(), Guid.NewGuid(), "test-value");

        var result = await _controller.SaveValue(cmd);

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(500);
    }
}
