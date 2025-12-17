using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace API.IntegrationTests.Controllers
{
    public class DashboardControllerTests : IClassFixture<TestWebApplicationFactory>
    {
        private readonly TestWebApplicationFactory _factory;
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        public DashboardControllerTests(TestWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task GetMetrics_WithAdminAuth_ShouldReturnOk()
        {
            var client = _factory.CreateClient();
            var token = await TestHelpers.GetAdminAuthTokenAsync(_factory, client);
            TestHelpers.SetAuthToken(client, token);

            var response = await client.GetAsync("/api/v1/dashboard/metrics");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GetMetrics_WithMemberAuth_ShouldReturnForbidden()
        {
            var client = _factory.CreateClient();
            var token = await TestHelpers.GetAuthTokenAsync(client, $"dashboard_member_{Guid.NewGuid()}@test.com");
            TestHelpers.SetAuthToken(client, token);

            var response = await client.GetAsync("/api/v1/dashboard/metrics");
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task GetMetrics_WithoutAuth_ShouldReturnUnauthorized()
        {
            var client = _factory.CreateClient();
            var response = await client.GetAsync("/api/v1/dashboard/metrics");
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }
}
