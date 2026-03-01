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
    // NOTE: Integration tests — exclude with: dotnet test --filter "Category!=Integration"
    [Trait("Category", "Integration")]
    public class UsersControllerTests : IClassFixture<TestWebApplicationFactory>
    {
        private readonly TestWebApplicationFactory _factory;
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        public UsersControllerTests(TestWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task GetUsers_WithMemberAuth_ShouldReturnForbidden()
        {
            var client = _factory.CreateClient();
            var token = await TestHelpers.GetAuthTokenAsync(client, $"users_get_{Guid.NewGuid()}@test.com");
            TestHelpers.SetAuthToken(client, token);

            var response = await client.GetAsync("/api/v1/users");
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task GetUsers_WithoutAuth_ShouldReturnUnauthorized()
        {
            var client = _factory.CreateClient();
            var response = await client.GetAsync("/api/v1/users");
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task ResolveUsername_WithAuth_ValidUsername_ShouldReturnOk()
        {
            var client = _factory.CreateClient();
            var email = $"resolve_test_{Guid.NewGuid()}@test.com";
            var token = await TestHelpers.GetAuthTokenAsync(client, email);
            TestHelpers.SetAuthToken(client, token);

            var expectedUsername = email.Split('@')[0].ToLowerInvariant();
            var response = await client.PostAsJsonAsync("/api/v1/users/resolve", new { username = expectedUsername });
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task ResolveUsername_WithAuth_InvalidUsername_ShouldReturnNotFound()
        {
            var client = _factory.CreateClient();
            var token = await TestHelpers.GetAuthTokenAsync(client, $"resolve_notfound_{Guid.NewGuid()}@test.com");
            TestHelpers.SetAuthToken(client, token);

            var response = await client.PostAsJsonAsync("/api/v1/users/resolve", new { username = "nonexistent_user_xyz" });
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task ResolveUsername_WithoutAuth_ShouldReturnUnauthorized()
        {
            var client = _factory.CreateClient();
            var response = await client.PostAsJsonAsync("/api/v1/users/resolve", new { username = "test" });
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }
}
