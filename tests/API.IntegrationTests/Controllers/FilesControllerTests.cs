using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace API.IntegrationTests.Controllers
{
    public class FilesControllerTests : IClassFixture<TestWebApplicationFactory>
    {
        private readonly TestWebApplicationFactory _factory;
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        public FilesControllerTests(TestWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Upload_WithAuth_ValidFile_ShouldReturnUrl()
        {
            // Arrange
            var client = _factory.CreateClient();
            var token = await TestHelpers.GetAuthTokenAsync(client, $"files_upload_{Guid.NewGuid()}@test.com");
            TestHelpers.SetAuthToken(client, token);

            using var content = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(new byte[] { 1, 2, 3, 4, 5 });
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/plain");
            content.Add(fileContent, "file", "test.txt");

            // Act
            var response = await client.PostAsync("/api/v1/files/upload", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<UploadResult>(responseContent, JsonOptions);
            result!.Url.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task Upload_WithoutAuth_ShouldReturnUnauthorized()
        {
            // Arrange
            var client = _factory.CreateClient();

            using var content = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(new byte[] { 1, 2, 3 });
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/plain");
            content.Add(fileContent, "file", "test.txt");

            // Act
            var response = await client.PostAsync("/api/v1/files/upload", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Upload_EmptyFile_ShouldReturnBadRequest()
        {
            // Arrange
            var client = _factory.CreateClient();
            var token = await TestHelpers.GetAuthTokenAsync(client, $"files_empty_{Guid.NewGuid()}@test.com");
            TestHelpers.SetAuthToken(client, token);

            using var content = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(Array.Empty<byte>());
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/plain");
            content.Add(fileContent, "file", "empty.txt");

            // Act
            var response = await client.PostAsync("/api/v1/files/upload", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Upload_InvalidFileType_ShouldReturnBadRequest()
        {
            // Arrange
            var client = _factory.CreateClient();
            var token = await TestHelpers.GetAuthTokenAsync(client, $"files_invalid_{Guid.NewGuid()}@test.com");
            TestHelpers.SetAuthToken(client, token);

            using var content = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(new byte[] { 1, 2, 3, 4, 5 });
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
            content.Add(fileContent, "file", "test.bin");

            // Act
            var response = await client.PostAsync("/api/v1/files/upload", content);

            // Assert - Binary files are not allowed (OWASP security)
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        private class UploadResult
        {
            public string Url { get; set; } = string.Empty;
        }
    }
}
