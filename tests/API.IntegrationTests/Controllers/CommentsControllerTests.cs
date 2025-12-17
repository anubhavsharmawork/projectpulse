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
    public class CommentsControllerTests : IClassFixture<TestWebApplicationFactory>
    {
        private readonly TestWebApplicationFactory _factory;
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        public CommentsControllerTests(TestWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private async Task<(Guid projectId, Guid workItemId)> CreateProjectAndTaskAsync(HttpClient client)
        {
            var projectResponse = await client.PostAsJsonAsync("/api/v1/projects", new { Name = "Test Project" });
            projectResponse.EnsureSuccessStatusCode();
            var projectContent = await projectResponse.Content.ReadAsStringAsync();
            var project = JsonSerializer.Deserialize<ProjectResult>(projectContent, JsonOptions);

            var taskResponse = await client.PostAsJsonAsync($"/api/v1/projects/{project!.ProjectId}/tasks", new { Title = "Test Task" });
            taskResponse.EnsureSuccessStatusCode();
            var taskContent = await taskResponse.Content.ReadAsStringAsync();
            var task = JsonSerializer.Deserialize<TaskResult>(taskContent, JsonOptions);

            return (project.ProjectId, task!.TaskId);
        }

        [Fact]
        public async Task GetAll_WithAuth_ShouldReturnOk()
        {
            var client = _factory.CreateClient();
            var token = await TestHelpers.GetAuthTokenAsync(client, $"comments_get_{Guid.NewGuid()}@test.com");
            TestHelpers.SetAuthToken(client, token);
            var (_, workItemId) = await CreateProjectAndTaskAsync(client);

            var response = await client.GetAsync($"/api/v1/work-items/{workItemId}/comments");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task Create_ValidComment_ShouldReturnCommentId()
        {
            var client = _factory.CreateClient();
            var token = await TestHelpers.GetAuthTokenAsync(client, $"comments_create_{Guid.NewGuid()}@test.com");
            TestHelpers.SetAuthToken(client, token);
            var (_, workItemId) = await CreateProjectAndTaskAsync(client);

            var response = await client.PostAsJsonAsync($"/api/v1/work-items/{workItemId}/comments", new
            {
                Body = "This is a test comment"
            });

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<CommentResult>(content, JsonOptions);
            result!.CommentId.Should().NotBe(Guid.Empty);
        }

        [Fact]
        public async Task Delete_ExistingComment_ShouldReturnNoContent()
        {
            var client = _factory.CreateClient();
            var token = await TestHelpers.GetAuthTokenAsync(client, $"comments_delete_{Guid.NewGuid()}@test.com");
            TestHelpers.SetAuthToken(client, token);
            var (_, workItemId) = await CreateProjectAndTaskAsync(client);

            var createResponse = await client.PostAsJsonAsync($"/api/v1/work-items/{workItemId}/comments", new { Body = "Comment to delete" });
            createResponse.EnsureSuccessStatusCode();
            var createContent = await createResponse.Content.ReadAsStringAsync();
            var comment = JsonSerializer.Deserialize<CommentResult>(createContent, JsonOptions);

            var response = await client.DeleteAsync($"/api/v1/work-items/{workItemId}/comments/{comment!.CommentId}");
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task Delete_NonExistingComment_ShouldReturnNotFound()
        {
            var client = _factory.CreateClient();
            var token = await TestHelpers.GetAuthTokenAsync(client, $"comments_delete_notfound_{Guid.NewGuid()}@test.com");
            TestHelpers.SetAuthToken(client, token);
            var (_, workItemId) = await CreateProjectAndTaskAsync(client);

            var response = await client.DeleteAsync($"/api/v1/work-items/{workItemId}/comments/{Guid.NewGuid()}");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetAll_WithoutAuth_ShouldReturnUnauthorized()
        {
            var client = _factory.CreateClient();
            var response = await client.GetAsync($"/api/v1/work-items/{Guid.NewGuid()}/comments");
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        private class ProjectResult { public Guid ProjectId { get; set; } }
        private class TaskResult { public Guid TaskId { get; set; } }
        private class CommentResult { public Guid CommentId { get; set; } }
    }
}
