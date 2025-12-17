using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace API.IntegrationTests.Controllers
{
    public class ProjectsControllerTests : IClassFixture<TestWebApplicationFactory>
    {
        private readonly TestWebApplicationFactory _factory;
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        public ProjectsControllerTests(TestWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task GetAll_WithoutAuth_ShouldReturnUnauthorized()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/api/v1/projects");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetAll_WithAuth_ShouldReturnOk()
        {
            // Arrange
            var client = _factory.CreateClient();
            var token = await TestHelpers.GetAuthTokenAsync(client, $"projects_get_{Guid.NewGuid()}@test.com");
            TestHelpers.SetAuthToken(client, token);

            // Act
            var response = await client.GetAsync("/api/v1/projects");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task Create_ValidData_ShouldReturnProjectId()
        {
            // Arrange
            var client = _factory.CreateClient();
            var token = await TestHelpers.GetAuthTokenAsync(client, $"create_proj_{Guid.NewGuid()}@test.com");
            TestHelpers.SetAuthToken(client, token);

            // Act
            var response = await client.PostAsJsonAsync("/api/v1/projects", new
            {
                Name = "Test Project",
                Description = "Test Description"
            });

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<CreateProjectResult>(content, JsonOptions);
            result.Should().NotBeNull();
            result!.ProjectId.Should().NotBe(Guid.Empty);
        }

        [Fact]
        public async Task Create_NullDescription_ShouldWork()
        {
            // Arrange
            var client = _factory.CreateClient();
            var token = await TestHelpers.GetAuthTokenAsync(client, $"null_desc_{Guid.NewGuid()}@test.com");
            TestHelpers.SetAuthToken(client, token);

            // Act
            var response = await client.PostAsJsonAsync("/api/v1/projects", new
            {
                Name = "Test Project"
            });

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task Delete_ExistingProject_ShouldReturnNoContent()
        {
            // Arrange
            var client = _factory.CreateClient();
            var token = await TestHelpers.GetAuthTokenAsync(client, $"delete_proj_{Guid.NewGuid()}@test.com");
            TestHelpers.SetAuthToken(client, token);

            var createResponse = await client.PostAsJsonAsync("/api/v1/projects", new
            {
                Name = "Project to Delete"
            });
            createResponse.EnsureSuccessStatusCode();
            var content = await createResponse.Content.ReadAsStringAsync();
            var project = JsonSerializer.Deserialize<CreateProjectResult>(content, JsonOptions);

            // Act
            var response = await client.DeleteAsync($"/api/v1/projects/{project!.ProjectId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task Delete_NonExistingProject_ShouldReturnNotFound()
        {
            // Arrange
            var client = _factory.CreateClient();
            var token = await TestHelpers.GetAuthTokenAsync(client, $"delete_notfound_{Guid.NewGuid()}@test.com");
            TestHelpers.SetAuthToken(client, token);

            // Act
            var response = await client.DeleteAsync($"/api/v1/projects/{Guid.NewGuid()}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Create_WithoutAuth_ShouldReturnUnauthorized()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.PostAsJsonAsync("/api/v1/projects", new
            {
                Name = "Unauthorized Project"
            });

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        private class CreateProjectResult
        {
            public Guid ProjectId { get; set; }
        }
    }
}
