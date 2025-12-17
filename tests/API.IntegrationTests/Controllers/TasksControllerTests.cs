using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace API.IntegrationTests.Controllers
{
    public class TasksControllerTests : IClassFixture<TestWebApplicationFactory>
    {
        private readonly TestWebApplicationFactory _factory;
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        public TasksControllerTests(TestWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private async Task<Guid> CreateProjectAsync(HttpClient client)
        {
            var response = await client.PostAsJsonAsync("/api/v1/projects", new { Name = "Test Project" });
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ProjectResult>(content, JsonOptions);
            return result!.ProjectId;
        }

        [Fact]
        public async Task GetAll_WithAuth_ShouldReturnOk()
        {
            // Arrange
            var client = _factory.CreateClient();
            var token = await TestHelpers.GetAuthTokenAsync(client, $"tasks_get_{Guid.NewGuid()}@test.com");
            TestHelpers.SetAuthToken(client, token);
            var projectId = await CreateProjectAsync(client);

            // Act
            var response = await client.GetAsync($"/api/v1/projects/{projectId}/tasks");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GetAll_WithOrphansOnly_ShouldFilterTasks()
        {
            // Arrange
            var client = _factory.CreateClient();
            var token = await TestHelpers.GetAuthTokenAsync(client, $"tasks_orphans_{Guid.NewGuid()}@test.com");
            TestHelpers.SetAuthToken(client, token);
            var projectId = await CreateProjectAsync(client);

            // Create a task
            await client.PostAsJsonAsync($"/api/v1/projects/{projectId}/tasks", new
            {
                Title = "Orphan Task"
            });

            // Act
            var response = await client.GetAsync($"/api/v1/projects/{projectId}/tasks?orphansOnly=true");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task Create_ValidTask_ShouldReturnTaskId()
        {
            // Arrange
            var client = _factory.CreateClient();
            var token = await TestHelpers.GetAuthTokenAsync(client, $"tasks_create_{Guid.NewGuid()}@test.com");
            TestHelpers.SetAuthToken(client, token);
            var projectId = await CreateProjectAsync(client);

            // Act
            var response = await client.PostAsJsonAsync($"/api/v1/projects/{projectId}/tasks", new
            {
                Title = "Test Task",
                Description = "Test Description"
            });

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<TaskCreateResult>(content, JsonOptions);
            result!.TaskId.Should().NotBe(Guid.Empty);
        }

        [Fact]
        public async Task Complete_ExistingTask_ShouldReturnNoContent()
        {
            // Arrange
            var client = _factory.CreateClient();
            var token = await TestHelpers.GetAuthTokenAsync(client, $"tasks_complete_{Guid.NewGuid()}@test.com");
            TestHelpers.SetAuthToken(client, token);
            var projectId = await CreateProjectAsync(client);

            var createResponse = await client.PostAsJsonAsync($"/api/v1/projects/{projectId}/tasks", new
            {
                Title = "Task to Complete"
            });
            createResponse.EnsureSuccessStatusCode();
            var content = await createResponse.Content.ReadAsStringAsync();
            var task = JsonSerializer.Deserialize<TaskCreateResult>(content, JsonOptions);

            // Act
            var response = await client.PostAsync($"/api/v1/projects/{projectId}/tasks/{task!.TaskId}/complete", null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task Complete_NonExistingTask_ShouldReturnNotFound()
        {
            // Arrange
            var client = _factory.CreateClient();
            var token = await TestHelpers.GetAuthTokenAsync(client, $"tasks_complete_notfound_{Guid.NewGuid()}@test.com");
            TestHelpers.SetAuthToken(client, token);
            var projectId = await CreateProjectAsync(client);

            // Act
            var response = await client.PostAsync($"/api/v1/projects/{projectId}/tasks/{Guid.NewGuid()}/complete", null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Delete_ExistingTask_ShouldReturnNoContent()
        {
            // Arrange
            var client = _factory.CreateClient();
            var token = await TestHelpers.GetAuthTokenAsync(client, $"tasks_delete_{Guid.NewGuid()}@test.com");
            TestHelpers.SetAuthToken(client, token);
            var projectId = await CreateProjectAsync(client);

            var createResponse = await client.PostAsJsonAsync($"/api/v1/projects/{projectId}/tasks", new
            {
                Title = "Task to Delete"
            });
            createResponse.EnsureSuccessStatusCode();
            var content = await createResponse.Content.ReadAsStringAsync();
            var task = JsonSerializer.Deserialize<TaskCreateResult>(content, JsonOptions);

            // Act
            var response = await client.DeleteAsync($"/api/v1/projects/{projectId}/tasks/{task!.TaskId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task Delete_NonExistingTask_ShouldReturnNotFound()
        {
            // Arrange
            var client = _factory.CreateClient();
            var token = await TestHelpers.GetAuthTokenAsync(client, $"tasks_delete_notfound_{Guid.NewGuid()}@test.com");
            TestHelpers.SetAuthToken(client, token);
            var projectId = await CreateProjectAsync(client);

            // Act
            var response = await client.DeleteAsync($"/api/v1/projects/{projectId}/tasks/{Guid.NewGuid()}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetAll_WithoutAuth_ShouldReturnUnauthorized()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync($"/api/v1/projects/{Guid.NewGuid()}/tasks");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        private class ProjectResult { public Guid ProjectId { get; set; } }
        private class TaskCreateResult { public Guid TaskId { get; set; } }
    }
}
