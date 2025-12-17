using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.Users.Queries;
using Application.UnitTests.TestHelpers;
using Domain.Entities;
using FluentAssertions;
using Xunit;

namespace Application.UnitTests.Users
{
    public class GetUsersHandlerTests
    {
        [Fact]
        public async Task Handle_NoSearchTerm_ShouldReturnAllUsers()
        {
            // Arrange
            using var db = TestDbContextFactory.CreateWithData(ctx =>
            {
                ctx.Users.Add(new User { Id = Guid.NewGuid(), Email = "user1@example.com", DisplayName = "User One", PasswordHash = "hash" });
                ctx.Users.Add(new User { Id = Guid.NewGuid(), Email = "user2@example.com", DisplayName = "User Two", PasswordHash = "hash" });
            });
            var handler = new GetUsersHandler(db);
            var query = new GetUsersQuery();

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().HaveCount(2);
        }

        [Fact]
        public async Task Handle_WithSearchTerm_ShouldFilterByDisplayName()
        {
            // Arrange
            using var db = TestDbContextFactory.CreateWithData(ctx =>
            {
                ctx.Users.Add(new User { Id = Guid.NewGuid(), Email = "john@example.com", DisplayName = "John Doe", PasswordHash = "hash" });
                ctx.Users.Add(new User { Id = Guid.NewGuid(), Email = "jane@example.com", DisplayName = "Jane Smith", PasswordHash = "hash" });
            });
            var handler = new GetUsersHandler(db);
            var query = new GetUsersQuery("john");

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().HaveCount(1);
            result[0].DisplayName.Should().Be("John Doe");
        }

        [Fact]
        public async Task Handle_WithSearchTerm_ShouldFilterByEmail()
        {
            // Arrange
            using var db = TestDbContextFactory.CreateWithData(ctx =>
            {
                ctx.Users.Add(new User { Id = Guid.NewGuid(), Email = "john@example.com", DisplayName = "John Doe", PasswordHash = "hash" });
                ctx.Users.Add(new User { Id = Guid.NewGuid(), Email = "jane@example.com", DisplayName = "Jane Smith", PasswordHash = "hash" });
            });
            var handler = new GetUsersHandler(db);
            var query = new GetUsersQuery("jane@");

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().HaveCount(1);
            result[0].Email.Should().Be("jane@example.com");
        }

        [Fact]
        public async Task Handle_CaseInsensitiveSearch_ShouldWork()
        {
            // Arrange
            using var db = TestDbContextFactory.CreateWithData(ctx =>
            {
                ctx.Users.Add(new User { Id = Guid.NewGuid(), Email = "john@example.com", DisplayName = "John Doe", PasswordHash = "hash" });
            });
            var handler = new GetUsersHandler(db);
            var query = new GetUsersQuery("JOHN");

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().HaveCount(1);
        }

        [Fact]
        public async Task Handle_ShouldOrderByDisplayName()
        {
            // Arrange
            using var db = TestDbContextFactory.CreateWithData(ctx =>
            {
                ctx.Users.Add(new User { Id = Guid.NewGuid(), Email = "z@example.com", DisplayName = "Zoe", PasswordHash = "hash" });
                ctx.Users.Add(new User { Id = Guid.NewGuid(), Email = "a@example.com", DisplayName = "Alice", PasswordHash = "hash" });
                ctx.Users.Add(new User { Id = Guid.NewGuid(), Email = "m@example.com", DisplayName = "Mike", PasswordHash = "hash" });
            });
            var handler = new GetUsersHandler(db);
            var query = new GetUsersQuery();

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().HaveCount(3);
            result[0].DisplayName.Should().Be("Alice");
            result[1].DisplayName.Should().Be("Mike");
            result[2].DisplayName.Should().Be("Zoe");
        }

        [Fact]
        public async Task Handle_ShouldLimitTo20Users()
        {
            // Arrange
            using var db = TestDbContextFactory.CreateWithData(ctx =>
            {
                for (int i = 0; i < 25; i++)
                {
                    ctx.Users.Add(new User 
                    { 
                        Id = Guid.NewGuid(), 
                        Email = $"user{i}@example.com", 
                        DisplayName = $"User {i:D2}", 
                        PasswordHash = "hash" 
                    });
                }
            });
            var handler = new GetUsersHandler(db);
            var query = new GetUsersQuery();

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().HaveCount(20);
        }

        [Fact]
        public async Task Handle_ShouldReturnCorrectDtoProperties()
        {
            // Arrange
            var userId = Guid.NewGuid();
            using var db = TestDbContextFactory.CreateWithData(ctx =>
            {
                ctx.Users.Add(new User 
                { 
                    Id = userId, 
                    Email = "test@example.com", 
                    DisplayName = "Test User", 
                    PasswordHash = "hash" 
                });
            });
            var handler = new GetUsersHandler(db);
            var query = new GetUsersQuery();

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().HaveCount(1);
            result[0].Id.Should().Be(userId);
            result[0].Email.Should().Be("test@example.com");
            result[0].DisplayName.Should().Be("Test User");
        }

        [Fact]
        public async Task Handle_EmptyDatabase_ShouldReturnEmptyList()
        {
            // Arrange
            using var db = TestDbContextFactory.Create();
            var handler = new GetUsersHandler(db);
            var query = new GetUsersQuery();

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task Handle_NoMatchingUsers_ShouldReturnEmptyList()
        {
            // Arrange
            using var db = TestDbContextFactory.CreateWithData(ctx =>
            {
                ctx.Users.Add(new User { Id = Guid.NewGuid(), Email = "john@example.com", DisplayName = "John", PasswordHash = "hash" });
            });
            var handler = new GetUsersHandler(db);
            var query = new GetUsersQuery("xyz");

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().BeEmpty();
        }
    }
}
