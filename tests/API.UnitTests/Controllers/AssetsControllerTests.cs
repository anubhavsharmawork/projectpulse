using API.Controllers;
using Application.Assets.Commands;
using Application.Assets.Queries;
using Domain.Enums;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace API.UnitTests.Controllers;

public class AssetsControllerTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly AssetsController _controller;

    public AssetsControllerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _controller = new AssetsController(_mediatorMock.Object);
    }

    [Fact]
    public async Task GetByProject_ShouldReturnOk()
    {
        var assets = new AssetsByProjectResult(new List<AssetListItemDto>(), 0, 1, 50);
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetAssetsByProjectQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(assets);

        var result = await _controller.GetByProject(Guid.NewGuid(), null, null, null, 1, 50);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetByProject_WithFilters_ShouldReturnOk()
    {
        var assets = new AssetsByProjectResult(new List<AssetListItemDto>(), 0, 2, 25);
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetAssetsByProjectQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(assets);

        var result = await _controller.GetByProject(
            Guid.NewGuid(),
            AssetStatus.Available,
            AssetType.Equipment,
            "laptop",
            2,
            25);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_AssetExists_ShouldReturnOk()
    {
        var asset = new AssetDetailDto(
            Guid.NewGuid(), Guid.NewGuid(), "TAG001", "Laptop", null, DateTime.UtcNow,
            1000m, 800m, AssetStatus.Available, "Office", null, null, null, null, null, null,
            null, DepreciationMethod.StraightLine, 5, AssetType.Equipment,
            DateTime.UtcNow, DateTime.UtcNow, "admin", true, null, null, null, null, null, null);
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetAssetByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(asset);

        var result = await _controller.GetById(Guid.NewGuid());

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_AssetNotFound_ShouldReturnNotFound()
    {
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetAssetByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AssetDetailDto?)null);

        var result = await _controller.GetById(Guid.NewGuid());

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Delete_ShouldReturnNoContent()
    {
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<DeleteAssetCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(Unit.Value));

        var result = await _controller.Delete(Guid.NewGuid());

        result.Should().BeOfType<NoContentResult>();
    }
}
