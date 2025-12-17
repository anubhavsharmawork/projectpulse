using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Application.Common.Security;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace API.IntegrationTests
{
    public static class TestHelpers
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public static async Task<string> GetAuthTokenAsync(HttpClient client, string email = "testuser@example.com", string password = "TestPass123!")
        {
            // Register the user
            var registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", new
            {
                Email = email,
                Password = password,
                DisplayName = "Test User"
            });
            // Ignore if already registered

            // Login
            var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new
            {
                Email = email,
                Password = password
            });

            if (!loginResponse.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Login failed for {email}: {loginResponse.StatusCode}");
            }

            var content = await loginResponse.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<LoginResult>(content, JsonOptions);
            return result?.Token ?? throw new InvalidOperationException("Failed to get auth token");
        }

        public static async Task<string> GetAdminAuthTokenAsync(TestWebApplicationFactory factory, HttpClient client)
        {
            var email = $"admin_{Guid.NewGuid()}@test.local";
            var password = "AdminPass123!";
            
            // Seed admin user directly in database
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            var adminUser = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                DisplayName = "Admin User",
                PasswordHash = SimplePasswordHasher.Hash(password, "demo-salt"),
                Role = Role.Admin
            };
            db.Users.Add(adminUser);
            await db.SaveChangesAsync();

            // Login
            var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new
            {
                Email = email,
                Password = password
            });

            if (!loginResponse.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Admin login failed: {loginResponse.StatusCode}");
            }

            var content = await loginResponse.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<LoginResult>(content, JsonOptions);
            return result?.Token ?? throw new InvalidOperationException("Failed to get admin auth token");
        }

        public static void SetAuthToken(HttpClient client, string token)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        private class LoginResult
        {
            public string Token { get; set; } = string.Empty;
        }
    }
}
