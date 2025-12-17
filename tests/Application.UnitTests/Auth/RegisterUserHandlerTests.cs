using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.Auth.Commands;
using Application.Common.Security;
using Application.UnitTests.TestHelpers;
using Domain.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace Application.UnitTests.Auth
{
    public class RegisterUserHandlerTests
    {
        private readonly Mock<IConfiguration> _configMock;
        private const string TestSalt = "test-salt";

        public RegisterUserHandlerTests()
        {
            _configMock = new Mock<IConfiguration>();
            _configMock.Setup(c => c["DEMO_SALT"]).Returns(TestSalt);
        }

        [Fact]
        public async Task Handle_ValidCommand_ShouldCreateUser()
        {
            // Arrange
            using var db = TestDbContextFactory.Create();
            var handler = new RegisterUserHandler(db, _configMock.Object);
            var command = new RegisterUserCommand("test@example.com", "password123", "Test User");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.UserId.Should().NotBe(Guid.Empty);

            var user = await db.Users.SingleOrDefaultAsync(u => u.Id == result.UserId);
            user.Should().NotBeNull();
            user!.Email.Should().Be("test@example.com");
            user.DisplayName.Should().Be("Test User");
            user.Role.Should().Be(Role.Member);
        }

        [Fact]
        public async Task Handle_ValidCommand_ShouldHashPassword()
        {
            // Arrange
            using var db = TestDbContextFactory.Create();
            var handler = new RegisterUserHandler(db, _configMock.Object);
            var password = "password123";
            var command = new RegisterUserCommand("test@example.com", password, "Test User");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            var user = await db.Users.SingleAsync(u => u.Id == result.UserId);
            user.PasswordHash.Should().NotBe(password);
            user.PasswordHash.Should().NotBeNullOrEmpty();
            
            // Verify the password can be verified with the hasher
            SimplePasswordHasher.Verify(password, TestSalt, user.PasswordHash).Should().BeTrue();
        }

        [Fact]
        public async Task Handle_DuplicateEmail_ShouldThrowInvalidOperationException()
        {
            // Arrange
            using var db = TestDbContextFactory.CreateWithData(ctx =>
            {
                ctx.Users.Add(new User
                {
                    Id = Guid.NewGuid(),
                    Email = "existing@example.com",
                    DisplayName = "Existing User",
                    PasswordHash = "hash"
                });
            });
            var handler = new RegisterUserHandler(db, _configMock.Object);
            var command = new RegisterUserCommand("existing@example.com", "password123", "New User");

            // Act
            var act = async () => await handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Email already registered");
        }

        [Fact]
        public async Task Handle_WithDefaultSalt_ShouldUseDefaultValue()
        {
            // Arrange
            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["DEMO_SALT"]).Returns((string?)null);
            
            using var db = TestDbContextFactory.Create();
            var handler = new RegisterUserHandler(db, configMock.Object);
            var command = new RegisterUserCommand("test@example.com", "password123", "Test User");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.UserId.Should().NotBe(Guid.Empty);
            
            var user = await db.Users.SingleAsync(u => u.Id == result.UserId);
            // When DEMO_SALT is null, it should use "demo-salt" as fallback
            SimplePasswordHasher.Verify("password123", "demo-salt", user.PasswordHash).Should().BeTrue();
        }

        [Fact]
        public async Task Handle_ShouldSetCreatedAtToNow()
        {
            // Arrange
            using var db = TestDbContextFactory.Create();
            var handler = new RegisterUserHandler(db, _configMock.Object);
            var command = new RegisterUserCommand("test@example.com", "password123", "Test User");
            var beforeCreation = DateTime.UtcNow;

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            var user = await db.Users.SingleAsync(u => u.Id == result.UserId);
            user.CreatedAt.Should().BeOnOrAfter(beforeCreation);
            user.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task Handle_MultipleUsers_ShouldCreateUniqueIds()
        {
            // Arrange
            using var db = TestDbContextFactory.Create();
            var handler = new RegisterUserHandler(db, _configMock.Object);

            // Act
            var result1 = await handler.Handle(new RegisterUserCommand("user1@example.com", "pass1", "User 1"), CancellationToken.None);
            var result2 = await handler.Handle(new RegisterUserCommand("user2@example.com", "pass2", "User 2"), CancellationToken.None);

            // Assert
            result1.UserId.Should().NotBe(result2.UserId);
            (await db.Users.CountAsync()).Should().Be(2);
        }
    }
}
