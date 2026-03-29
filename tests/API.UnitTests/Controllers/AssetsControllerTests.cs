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

    private static T CreateDefault<T>()
    {
        var ctor = typeof(T).GetConstructors().First();
        var args = ctor.GetParameters().Select(p => p.ParameterType.IsValueType ? Activator.CreateInstance(p.ParameterType) : null).ToArray();
        return (T)ctor.Invoke(args);
    }

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

    [Fact]
    public async Task Create_ShouldReturnCreated()
    {
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<CreateAssetCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateAssetResult(Guid.NewGuid()));

        // create a command with minimal valid values using reflection helper
        var cmd = CreateDefault<CreateAssetCommand>();

        var result = await _controller.Create(Guid.NewGuid(), cmd);

        result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task Update_ShouldReturnNoContent()
    {
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<UpdateAssetCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Unit.Value);

        var cmd = CreateDefault<UpdateAssetCommand>();

        var result = await _controller.Update(Guid.NewGuid(), cmd);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Assign_ReturnsOk()
    {
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<AssignAssetCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssignAssetResult(Guid.NewGuid()));

        var cmd = new AssignAssetCommand(Guid.Empty, Guid.NewGuid(), DateTime.UtcNow, null);

        var result = await _controller.Assign(Guid.NewGuid(), cmd);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Return_ReturnsOk()
    {
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<ReturnAssetCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Unit.Value);

        var cmd = new ReturnAssetCommand(Guid.Empty, "Good", null);

        var result = await _controller.Return(Guid.NewGuid(), cmd);

        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task GetMaintenanceHistory_ReturnsOk()
    {
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetAssetMaintenanceHistoryQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MaintenanceRecordDto>());

        var result = await _controller.GetMaintenanceHistory(Guid.NewGuid());

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ScheduleMaintenance_ReturnsCreated()
    {
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<ScheduleMaintenanceCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ScheduleMaintenanceResult(Guid.NewGuid()));

        var cmd = new ScheduleMaintenanceCommand(Guid.Empty, MaintenanceType.Preventive, DateTime.UtcNow, "note", 50m, null);

        var result = await _controller.ScheduleMaintenance(Guid.NewGuid(), cmd);

        result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task RecordMaintenance_ReturnsNoContent()
    {
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<RecordMaintenanceCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Unit.Value);

        var cmd = new RecordMaintenanceCommand(Guid.Empty, DateTime.UtcNow, "tech", 123.45m, "notes");

        var result = await _controller.RecordMaintenance(Guid.NewGuid(), cmd);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task GetCheckoutHistory_ReturnsOk()
    {
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetAssetCheckoutHistoryQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssetCheckoutDto>());

        var result = await _controller.GetCheckoutHistory(Guid.NewGuid());

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetHistory_ReturnsOk()
    {
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetAssetHistoryQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssetHistoryDto>());

        var result = await _controller.GetHistory(Guid.NewGuid());

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Search_EmptyQuery_ReturnsBadRequest()
    {
        var result = await _controller.Search("", 1, 50);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Search_WithQuery_ReturnsOk()
    {
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<SearchAssetsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssetsByProjectResult(new List<AssetListItemDto>(), 0, 1, 50));

        var result = await _controller.Search("laptop", 1, 50);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetDomainAssetConfig_ReturnsOk()
    {
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetDomainAssetConfigQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DomainAssetConfigResult(DomainType.IT, new List<DomainAssetConfigItemDto>()));

        var result = await _controller.GetDomainAssetConfig(DomainType.IT);

        result.Should().BeOfType<OkObjectResult>();
    }
}
