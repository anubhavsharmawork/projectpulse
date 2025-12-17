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
    public class AuthControllerTests : IClassFixture<TestWebApplicationFactory>
    {
        private readonly TestWebApplicationFactory _factory;
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        public AuthControllerTests(TestWebApplicationFactory factory)
        {
            _factory = factory;
        }

        #region Register Tests

        [Fact]
        public async Task Register_ValidData_ShouldReturnOkWithUserId()
        {
            // Arrange
            var client = _factory.CreateClient();
            var uniqueEmail = $"register_{Guid.NewGuid()}@test.com";

            // Act
            var response = await client.PostAsJsonAsync("/api/v1/auth/register", new
            {
                Email = uniqueEmail,
                Password = "ValidPass123!",
                DisplayName = "Test User"
            });

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<RegisterResult>(content, JsonOptions);
            result.Should().NotBeNull();
            result!.UserId.Should().NotBe(Guid.Empty);
        }

        [Fact]
        public async Task Register_DuplicateEmail_ShouldReturnBadRequest()
        {
            // Arrange
            var client = _factory.CreateClient();
            var email = $"duplicate_{Guid.NewGuid()}@test.com";
            
            // First registration
            var firstResponse = await client.PostAsJsonAsync("/api/v1/auth/register", new
            {
                Email = email,
                Password = "ValidPass123!",
                DisplayName = "First User"
            });
            firstResponse.EnsureSuccessStatusCode();

            // Act - Second registration with same email
            var response = await client.PostAsJsonAsync("/api/v1/auth/register", new
            {
                Email = email,
                Password = "ValidPass123!",
                DisplayName = "Second User"
            });

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Register_FormUrlEncoded_ShouldWork()
        {
            // Arrange
            var client = _factory.CreateClient();
            var uniqueEmail = $"formregister_{Guid.NewGuid()}@test.com";
            var formData = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("Email", uniqueEmail),
                new KeyValuePair<string, string>("Password", "ValidPass123!"),
                new KeyValuePair<string, string>("DisplayName", "Form User")
            });

            // Act
            var response = await client.PostAsync("/api/v1/auth/register", formData);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        #endregion

        #region Login Tests

        [Fact]
        public async Task Login_ValidCredentials_ShouldReturnToken()
        {
            // Arrange
            var client = _factory.CreateClient();
            var email = $"login_{Guid.NewGuid()}@test.com";
            var password = "ValidPass123!";
            
            // Register first
            var registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", new
            {
                Email = email,
                Password = password,
                DisplayName = "Login Test User"
            });
            registerResponse.EnsureSuccessStatusCode();

            // Act
            var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
            {
                Email = email,
                Password = password
            });

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<LoginResult>(content, JsonOptions);
            result.Should().NotBeNull();
            result!.Token.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task Login_InvalidEmail_ShouldReturnUnauthorized()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
            {
                Email = $"nonexistent_{Guid.NewGuid()}@test.com",
                Password = "anypassword"
            });

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Login_InvalidPassword_ShouldReturnUnauthorized()
        {
            // Arrange
            var client = _factory.CreateClient();
            var email = $"wrongpass_{Guid.NewGuid()}@test.com";
            
            // Register first
            var registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", new
            {
                Email = email,
                Password = "CorrectPass123!",
                DisplayName = "Test User"
            });
            registerResponse.EnsureSuccessStatusCode();

            // Act
            var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
            {
                Email = email,
                Password = "WrongPassword"
            });

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Login_FormUrlEncoded_ShouldWork()
        {
            // Arrange
            var client = _factory.CreateClient();
            var email = $"formlogin_{Guid.NewGuid()}@test.com";
            var password = "ValidPass123!";
            
            // Register first
            var registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", new
            {
                Email = email,
                Password = password,
                DisplayName = "Form Login User"
            });
            registerResponse.EnsureSuccessStatusCode();

            var formData = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("Email", email),
                new KeyValuePair<string, string>("Password", password)
            });

            // Act
            var response = await client.PostAsync("/api/v1/auth/login", formData);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        #endregion

        private class RegisterResult
        {
            public Guid UserId { get; set; }
        }

        private class LoginResult
        {
            public string Token { get; set; } = string.Empty;
        }
    }
}
