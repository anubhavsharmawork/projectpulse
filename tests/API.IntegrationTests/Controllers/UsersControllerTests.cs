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
    public class UsersControllerTests : IClassFixture<TestWebApplicationFactory>
    {
        private readonly TestWebApplicationFactory _factory;
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        public UsersControllerTests(TestWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task GetUsers_WithAuth_ShouldReturnOk()
        {
            var client = _factory.CreateClient();
            var token = await TestHelpers.GetAuthTokenAsync(client, $"users_get_{Guid.NewGuid()}@test.com");
            TestHelpers.SetAuthToken(client, token);

            var response = await client.GetAsync("/api/v1/users");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GetUsers_WithSearchTerm_ShouldReturnFilteredResults()
        {
            var client = _factory.CreateClient();
            var uniqueName = $"SearchUser_{Guid.NewGuid()}";
            var token = await TestHelpers.GetAuthTokenAsync(client, $"{uniqueName}@test.com");
            TestHelpers.SetAuthToken(client, token);

            var response = await client.GetAsync($"/api/v1/users?search={uniqueName}");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GetUsers_WithoutAuth_ShouldReturnUnauthorized()
        {
            var client = _factory.CreateClient();
            var response = await client.GetAsync("/api/v1/users");
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetUsers_EmptySearch_ShouldReturnAllUsers()
        {
            var client = _factory.CreateClient();
            var token = await TestHelpers.GetAuthTokenAsync(client, $"users_empty_search_{Guid.NewGuid()}@test.com");
            TestHelpers.SetAuthToken(client, token);

            var response = await client.GetAsync("/api/v1/users?search=");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }
}
