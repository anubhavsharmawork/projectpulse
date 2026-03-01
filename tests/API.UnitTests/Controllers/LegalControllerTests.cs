using API.Controllers;
using Application.Legal.Commands;
using Application.Legal.Queries;
using Domain.Enums;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace API.UnitTests.Controllers;

public class LegalControllerTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly LegalController _controller;

    public LegalControllerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _controller = new LegalController(_mediatorMock.Object);
    }

    [Fact]
    public async Task GetTerms_DocumentExists_ShouldReturnOk()
    {
        var doc = new LegalDocumentDto(Guid.NewGuid(), "TermsOfService", "1.0", DateTime.UtcNow, "Terms content");
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetLegalDocumentQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(doc);

        var result = await _controller.GetTerms();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(doc);
    }

    [Fact]
    public async Task GetTerms_NoDocument_ShouldReturnNotFound()
    {
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetLegalDocumentQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((LegalDocumentDto?)null);

        var result = await _controller.GetTerms();

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetPrivacy_DocumentExists_ShouldReturnOk()
    {
        var doc = new LegalDocumentDto(Guid.NewGuid(), "PrivacyPolicy", "1.0", DateTime.UtcNow, "Privacy content");
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetLegalDocumentQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(doc);

        var result = await _controller.GetPrivacy();

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetPrivacy_NoDocument_ShouldReturnNotFound()
    {
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetLegalDocumentQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((LegalDocumentDto?)null);

        var result = await _controller.GetPrivacy();

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetStatus_ShouldReturnOk()
    {
        var status = new LegalStatusDto(true, "1.0", "1.0", true, "1.0", "1.0", false);
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetLegalStatusQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(status);

        var result = await _controller.GetStatus();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(status);
    }

    [Fact]
    public async Task Accept_ValidRequest_ShouldReturnOk()
    {
        var request = new AcceptLegalRequest("1.0", "1.0");
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<AcceptLegalCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(Unit.Value));

        var result = await _controller.Accept(request);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Accept_InvalidVersion_ShouldReturnBadRequest()
    {
        var request = new AcceptLegalRequest("99.0", "99.0");
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<AcceptLegalCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Version mismatch"));

        var result = await _controller.Accept(request);

        result.Should().BeOfType<BadRequestObjectResult>();
    }
}
