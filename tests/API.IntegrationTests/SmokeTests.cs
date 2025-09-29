using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace API.IntegrationTests
{
    public class SmokeTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        public SmokeTests(WebApplicationFactory<Program> factory) => _factory = factory;

        [Fact]
        public async Task Root_ReturnsOk()
        {
            var client = _factory.CreateClient();
            var resp = await client.GetAsync("/");
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        }
    }
}
