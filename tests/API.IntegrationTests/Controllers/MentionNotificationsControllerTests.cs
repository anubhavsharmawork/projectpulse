using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace API.IntegrationTests.Controllers
{
    public class MentionNotificationsControllerTests : IClassFixture<TestWebApplicationFactory>
    {
        private readonly TestWebApplicationFactory _factory;
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        public MentionNotificationsControllerTests(TestWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private async Task<(string token, Guid userId)> CreateUserAndGetTokenAsync(HttpClient client, string email)
        {
            var password = "TestPass123!";
            
            // Register
            await client.PostAsJsonAsync("/api/v1/auth/register", new
            {
                Email = email,
                Password = password,
                DisplayName = email.Split('@')[0]
            });

            // Login
            var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new
            {
                Email = email,
                Password = password
            });
            loginResponse.EnsureSuccessStatusCode();
            var loginContent = await loginResponse.Content.ReadAsStringAsync();
            var loginResult = JsonSerializer.Deserialize<LoginResult>(loginContent, JsonOptions);

            // Get user ID from database
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = db.Users.First(u => u.Email == email);

            return (loginResult!.Token, user.Id);
        }

        private async Task SeedNotificationAsync(Guid userId, bool isRead = false)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            var notification = new MentionNotification
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CommentId = Guid.NewGuid(),
                WorkItemId = Guid.NewGuid(),
                MentionedByUserId = Guid.NewGuid(),
                CommentBody = "Test comment body",
                WorkItemTitle = "Test Work Item",
                MentionedByName = "Test User",
                IsRead = isRead,
                CreatedAt = DateTime.UtcNow
            };
            
            db.MentionNotifications.Add(notification);
            await db.SaveChangesAsync();
        }

        [Fact]
        public async Task GetMyNotifications_WithAuth_ShouldReturnOk()
        {
            var client = _factory.CreateClient();
            var email = $"notifications_get_{Guid.NewGuid()}@test.com";
            var (token, _) = await CreateUserAndGetTokenAsync(client, email);
            TestHelpers.SetAuthToken(client, token);

            var response = await client.GetAsync("/api/v1/mentionnotifications");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GetMyNotifications_WithNotifications_ShouldReturnList()
        {
            var client = _factory.CreateClient();
            var email = $"notifications_list_{Guid.NewGuid()}@test.com";
            var (token, userId) = await CreateUserAndGetTokenAsync(client, email);
            await SeedNotificationAsync(userId);
            TestHelpers.SetAuthToken(client, token);

            var response = await client.GetAsync("/api/v1/mentionnotifications");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GetUnreadCount_ShouldReturnCount()
        {
            var client = _factory.CreateClient();
            var email = $"notifications_count_{Guid.NewGuid()}@test.com";
            var (token, userId) = await CreateUserAndGetTokenAsync(client, email);
            await SeedNotificationAsync(userId, isRead: false);
            TestHelpers.SetAuthToken(client, token);

            var response = await client.GetAsync("/api/v1/mentionnotifications/unread-count");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task MarkAsRead_ExistingNotification_ShouldReturnNoContent()
        {
            var client = _factory.CreateClient();
            var email = $"notifications_read_{Guid.NewGuid()}@test.com";
            var (token, userId) = await CreateUserAndGetTokenAsync(client, email);
            
            Guid notificationId;
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var notification = new MentionNotification
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    CommentId = Guid.NewGuid(),
                    WorkItemId = Guid.NewGuid(),
                    MentionedByUserId = Guid.NewGuid(),
                    CommentBody = "Test",
                    WorkItemTitle = "Test",
                    MentionedByName = "Test",
                    IsRead = false
                };
                db.MentionNotifications.Add(notification);
                await db.SaveChangesAsync();
                notificationId = notification.Id;
            }

            TestHelpers.SetAuthToken(client, token);
            var response = await client.PostAsync($"/api/v1/mentionnotifications/{notificationId}/read", null);
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task MarkAsRead_NonExistingNotification_ShouldReturnNotFound()
        {
            var client = _factory.CreateClient();
            var email = $"notifications_read_notfound_{Guid.NewGuid()}@test.com";
            var (token, _) = await CreateUserAndGetTokenAsync(client, email);
            TestHelpers.SetAuthToken(client, token);

            var response = await client.PostAsync($"/api/v1/mentionnotifications/{Guid.NewGuid()}/read", null);
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task MarkAllAsRead_ShouldReturnNoContent()
        {
            var client = _factory.CreateClient();
            var email = $"notifications_readall_{Guid.NewGuid()}@test.com";
            var (token, userId) = await CreateUserAndGetTokenAsync(client, email);
            await SeedNotificationAsync(userId, isRead: false);
            TestHelpers.SetAuthToken(client, token);

            var response = await client.PostAsync("/api/v1/mentionnotifications/read-all", null);
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task GetMyNotifications_WithoutAuth_ShouldReturnUnauthorized()
        {
            var client = _factory.CreateClient();
            var response = await client.GetAsync("/api/v1/mentionnotifications");
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        private class LoginResult { public string Token { get; set; } = string.Empty; }
    }
}
