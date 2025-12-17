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
    public class WorkItemsControllerTests : IClassFixture<TestWebApplicationFactory>
    {
        private readonly TestWebApplicationFactory _factory;
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        public WorkItemsControllerTests(TestWebApplicationFactory factory)
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

        #region GetAll Tests

        [Fact]
        public async Task GetAll_WithAuth_ShouldReturnOk()
        {
            // Arrange
            var client = _factory.CreateClient();
            var token = await TestHelpers.GetAuthTokenAsync(client, $"workitems_get_{Guid.NewGuid()}@test.com");
            TestHelpers.SetAuthToken(client, token);
            var projectId = await CreateProjectAsync(client);

            // Act
            var response = await client.GetAsync($"/api/v1/projects/{projectId}/work-items");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        #endregion

        #region Epic Tests

        [Fact]
        public async Task GetEpics_ShouldReturnOk()
        {
            // Arrange
            var client = _factory.CreateClient();
            var token = await TestHelpers.GetAuthTokenAsync(client, $"epics_get_{Guid.NewGuid()}@test.com");
            TestHelpers.SetAuthToken(client, token);
            var projectId = await CreateProjectAsync(client);

            // Act
            var response = await client.GetAsync($"/api/v1/projects/{projectId}/work-items/epics");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task CreateEpic_ValidData_ShouldReturnEpicId()
        {
            // Arrange
            var client = _factory.CreateClient();
            var token = await TestHelpers.GetAuthTokenAsync(client, $"epic_create_{Guid.NewGuid()}@test.com");
            TestHelpers.SetAuthToken(client, token);
            var projectId = await CreateProjectAsync(client);

            // Act
            var response = await client.PostAsJsonAsync($"/api/v1/projects/{projectId}/work-items/epics", new
            {
                Title = "Test Epic",
                Description = "Epic Description"
            });

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<EpicResult>(content, JsonOptions);
            result!.EpicId.Should().NotBe(Guid.Empty);
        }

        #endregion

        #region UserStory Tests

        [Fact]
        public async Task GetUserStories_ShouldReturnOk()
        {
            // Arrange
            var client = _factory.CreateClient();
            var token = await TestHelpers.GetAuthTokenAsync(client, $"userstories_get_{Guid.NewGuid()}@test.com");
            TestHelpers.SetAuthToken(client, token);
            var projectId = await CreateProjectAsync(client);

            // Act
            var response = await client.GetAsync($"/api/v1/projects/{projectId}/work-items/user-stories");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task CreateUserStory_ValidData_ShouldReturnUserStoryId()
        {
            // Arrange
            var client = _factory.CreateClient();
            var token = await TestHelpers.GetAuthTokenAsync(client, $"userstory_create_{Guid.NewGuid()}@test.com");
            TestHelpers.SetAuthToken(client, token);
            var projectId = await CreateProjectAsync(client);

            // Act
            var response = await client.PostAsJsonAsync($"/api/v1/projects/{projectId}/work-items/user-stories", new
            {
                Title = "Test User Story"
            });

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<UserStoryResult>(content, JsonOptions);
            result!.UserStoryId.Should().NotBe(Guid.Empty);
        }

        [Fact]
        public async Task CreateUserStory_WithParent_ShouldWork()
        {
            // Arrange
            var client = _factory.CreateClient();
            var token = await TestHelpers.GetAuthTokenAsync(client, $"userstory_parent_{Guid.NewGuid()}@test.com");
            TestHelpers.SetAuthToken(client, token);
            var projectId = await CreateProjectAsync(client);

            // Create epic first
            var epicResponse = await client.PostAsJsonAsync($"/api/v1/projects/{projectId}/work-items/epics", new
            {
                Title = "Parent Epic"
            });
            var epicContent = await epicResponse.Content.ReadAsStringAsync();
            var epic = JsonSerializer.Deserialize<EpicResult>(epicContent, JsonOptions);

            // Act
            var response = await client.PostAsJsonAsync($"/api/v1/projects/{projectId}/work-items/user-stories", new
            {
                Title = "Child User Story",
                ParentId = epic!.EpicId
            });

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        #endregion

        #region Task under UserStory Tests

        [Fact]
        public async Task GetTasksForUserStory_ShouldReturnOk()
        {
            // Arrange
            var client = _factory.CreateClient();
            var token = await TestHelpers.GetAuthTokenAsync(client, $"tasks_userstory_{Guid.NewGuid()}@test.com");
            TestHelpers.SetAuthToken(client, token);
            var projectId = await CreateProjectAsync(client);

            var storyResponse = await client.PostAsJsonAsync($"/api/v1/projects/{projectId}/work-items/user-stories", new { Title = "Test Story" });
            storyResponse.EnsureSuccessStatusCode();
            var storyContent = await storyResponse.Content.ReadAsStringAsync();
            var story = JsonSerializer.Deserialize<UserStoryResult>(storyContent, JsonOptions);

            // Act
            var response = await client.GetAsync($"/api/v1/projects/{projectId}/work-items/user-stories/{story!.UserStoryId}/tasks");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task CreateTaskForUserStory_ShouldReturnTaskId()
        {
            // Arrange
            var client = _factory.CreateClient();
            var token = await TestHelpers.GetAuthTokenAsync(client, $"task_userstory_{Guid.NewGuid()}@test.com");
            TestHelpers.SetAuthToken(client, token);
            var projectId = await CreateProjectAsync(client);

            var storyResponse = await client.PostAsJsonAsync($"/api/v1/projects/{projectId}/work-items/user-stories", new { Title = "Parent Story" });
            storyResponse.EnsureSuccessStatusCode();
            var storyContent = await storyResponse.Content.ReadAsStringAsync();
            var story = JsonSerializer.Deserialize<UserStoryResult>(storyContent, JsonOptions);

            // Act
            var response = await client.PostAsJsonAsync($"/api/v1/projects/{projectId}/work-items/user-stories/{story!.UserStoryId}/tasks", new
            {
                Title = "Task under Story"
            });

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<TaskResult>(content, JsonOptions);
            result!.TaskId.Should().NotBe(Guid.Empty);
        }

        #endregion

        #region GetById and Children Tests

        [Fact]
        public async Task GetById_ExistingWorkItem_ShouldReturnOk()
        {
            // Arrange
            var client = _factory.CreateClient();
            var token = await TestHelpers.GetAuthTokenAsync(client, $"workitem_getbyid_{Guid.NewGuid()}@test.com");
            TestHelpers.SetAuthToken(client, token);
            var projectId = await CreateProjectAsync(client);

            var epicResponse = await client.PostAsJsonAsync($"/api/v1/projects/{projectId}/work-items/epics", new { Title = "Epic to Retrieve" });
            epicResponse.EnsureSuccessStatusCode();
            var epicContent = await epicResponse.Content.ReadAsStringAsync();
            var epic = JsonSerializer.Deserialize<EpicResult>(epicContent, JsonOptions);

            // Act
            var response = await client.GetAsync($"/api/v1/projects/{projectId}/work-items/{epic!.EpicId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GetById_NonExistingWorkItem_ShouldReturnNotFound()
        {
            // Arrange
            var client = _factory.CreateClient();
            var token = await TestHelpers.GetAuthTokenAsync(client, $"workitem_notfound_{Guid.NewGuid()}@test.com");
            TestHelpers.SetAuthToken(client, token);
            var projectId = await CreateProjectAsync(client);

            // Act
            var response = await client.GetAsync($"/api/v1/projects/{projectId}/work-items/{Guid.NewGuid()}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetChildren_ShouldReturnOk()
        {
            // Arrange
            var client = _factory.CreateClient();
            var token = await TestHelpers.GetAuthTokenAsync(client, $"children_get_{Guid.NewGuid()}@test.com");
            TestHelpers.SetAuthToken(client, token);
            var projectId = await CreateProjectAsync(client);

            var epicResponse = await client.PostAsJsonAsync($"/api/v1/projects/{projectId}/work-items/epics", new { Title = "Parent Epic" });
            epicResponse.EnsureSuccessStatusCode();
            var epicContent = await epicResponse.Content.ReadAsStringAsync();
            var epic = JsonSerializer.Deserialize<EpicResult>(epicContent, JsonOptions);

            // Act
            var response = await client.GetAsync($"/api/v1/projects/{projectId}/work-items/{epic!.EpicId}/children");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        #endregion

        #region Delete Tests

        [Fact]
        public async Task Delete_ExistingWorkItem_ShouldReturnNoContent()
        {
            // Arrange
            var client = _factory.CreateClient();
            var token = await TestHelpers.GetAuthTokenAsync(client, $"workitem_delete_{Guid.NewGuid()}@test.com");
            TestHelpers.SetAuthToken(client, token);
            var projectId = await CreateProjectAsync(client);

            var epicResponse = await client.PostAsJsonAsync($"/api/v1/projects/{projectId}/work-items/epics", new { Title = "Epic to Delete" });
            epicResponse.EnsureSuccessStatusCode();
            var epicContent = await epicResponse.Content.ReadAsStringAsync();
            var epic = JsonSerializer.Deserialize<EpicResult>(epicContent, JsonOptions);

            // Act
            var response = await client.DeleteAsync($"/api/v1/projects/{projectId}/work-items/{epic!.EpicId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task Delete_NonExistingWorkItem_ShouldReturnNotFound()
        {
            // Arrange
            var client = _factory.CreateClient();
            var token = await TestHelpers.GetAuthTokenAsync(client, $"workitem_delete_notfound_{Guid.NewGuid()}@test.com");
            TestHelpers.SetAuthToken(client, token);
            var projectId = await CreateProjectAsync(client);

            // Act
            var response = await client.DeleteAsync($"/api/v1/projects/{projectId}/work-items/{Guid.NewGuid()}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        #endregion

        [Fact]
        public async Task GetAll_WithoutAuth_ShouldReturnUnauthorized()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync($"/api/v1/projects/{Guid.NewGuid()}/work-items");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        private class ProjectResult { public Guid ProjectId { get; set; } }
        private class EpicResult { public Guid EpicId { get; set; } }
        private class UserStoryResult { public Guid UserStoryId { get; set; } }
        private class TaskResult { public Guid TaskId { get; set; } }
    }
}
