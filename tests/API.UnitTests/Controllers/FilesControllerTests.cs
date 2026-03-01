using API.Controllers;
using Application.Common.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace API.UnitTests.Controllers;

public class FilesControllerTests
{
    private readonly Mock<IStorageService> _storageMock;
    private readonly Mock<ILogger<FilesController>> _loggerMock;
    private readonly FilesController _controller;

    public FilesControllerTests()
    {
        _storageMock = new Mock<IStorageService>();
        _loggerMock = new Mock<ILogger<FilesController>>();
        _controller = new FilesController(_storageMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Upload_NullFile_ShouldReturnBadRequest()
    {
        var result = await _controller.Upload(null!);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Upload_EmptyFile_ShouldReturnBadRequest()
    {
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.Length).Returns(0);

        var result = await _controller.Upload(fileMock.Object);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Upload_FileTooLarge_ShouldReturnBadRequest()
    {
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.Length).Returns(50000); // > 40KB
        fileMock.Setup(f => f.FileName).Returns("test.jpg");

        var result = await _controller.Upload(fileMock.Object);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Upload_DisallowedExtension_ShouldReturnBadRequest()
    {
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.Length).Returns(1000);
        fileMock.Setup(f => f.FileName).Returns("test.exe");
        fileMock.Setup(f => f.ContentType).Returns("application/octet-stream");

        var result = await _controller.Upload(fileMock.Object);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Upload_DisallowedMimeType_ShouldReturnBadRequest()
    {
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.Length).Returns(1000);
        fileMock.Setup(f => f.FileName).Returns("test.jpg");
        fileMock.Setup(f => f.ContentType).Returns("application/octet-stream");

        var result = await _controller.Upload(fileMock.Object);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Upload_ValidFile_ShouldReturnOk()
    {
        var stream = new MemoryStream(new byte[100]);
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.Length).Returns(100);
        fileMock.Setup(f => f.FileName).Returns("test.jpg");
        fileMock.Setup(f => f.ContentType).Returns("image/jpeg");
        fileMock.Setup(f => f.OpenReadStream()).Returns(stream);

        _storageMock
            .Setup(s => s.UploadAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://storage.example.com/test.jpg");

        var result = await _controller.Upload(fileMock.Object);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Upload_StorageException_ShouldReturnBadRequest()
    {
        var stream = new MemoryStream(new byte[100]);
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.Length).Returns(100);
        fileMock.Setup(f => f.FileName).Returns("test.png");
        fileMock.Setup(f => f.ContentType).Returns("image/png");
        fileMock.Setup(f => f.OpenReadStream()).Returns(stream);

        _storageMock
            .Setup(s => s.UploadAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Storage unavailable"));

        var result = await _controller.Upload(fileMock.Object);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Upload_NoExtension_ShouldReturnBadRequest()
    {
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.Length).Returns(1000);
        fileMock.Setup(f => f.FileName).Returns("noextension");
        fileMock.Setup(f => f.ContentType).Returns("image/jpeg");

        var result = await _controller.Upload(fileMock.Object);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Theory]
    [InlineData("test.pdf", "application/pdf")]
    [InlineData("test.txt", "text/plain")]
    [InlineData("test.md", "text/markdown")]
    [InlineData("test.json", "application/json")]
    [InlineData("test.xml", "application/xml")]
    [InlineData("test.png", "image/png")]
    [InlineData("test.gif", "image/gif")]
    [InlineData("test.webp", "image/webp")]
    public async Task Upload_AllowedFileTypes_ShouldReturnOk(string fileName, string contentType)
    {
        var stream = new MemoryStream(new byte[100]);
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.Length).Returns(100);
        fileMock.Setup(f => f.FileName).Returns(fileName);
        fileMock.Setup(f => f.ContentType).Returns(contentType);
        fileMock.Setup(f => f.OpenReadStream()).Returns(stream);

        _storageMock
            .Setup(s => s.UploadAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync($"https://storage.example.com/{fileName}");

        var result = await _controller.Upload(fileMock.Object);

        result.Should().BeOfType<OkObjectResult>();
    }
}
