using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Auth.Commands;
using Application.Common.Interfaces;
using Application.Common.Security;
using Application.UnitTests.TestHelpers;
using Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace Application.UnitTests.Auth
{
    public class LoginUserHandlerTests
    {
        private readonly Mock<IConfiguration> _configMock;
        private readonly Mock<IJwtTokenService> _jwtMock;
        private const string TestSalt = "test-salt";
        private const string TestToken = "test-jwt-token";

        public LoginUserHandlerTests()
        {
            _configMock = new Mock<IConfiguration>();
            _configMock.Setup(c => c["DEMO_SALT"]).Returns(TestSalt);

            _jwtMock = new Mock<IJwtTokenService>();
            _jwtMock.Setup(j => j.GenerateToken(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(TestToken);
        }

        [Fact]
        public async Task Handle_ValidCredentials_ShouldReturnToken()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var password = "password123";
            var passwordHash = SimplePasswordHasher.Hash(password, TestSalt);
            
            using var db = TestDbContextFactory.CreateWithData(ctx =>
            {
                ctx.Users.Add(new User
                {
                    Id = userId,
                    Email = "test@example.com",
                    DisplayName = "Test User",
                    PasswordHash = passwordHash,
                    Role = Role.Member
                });
            });
            var handler = new LoginUserHandler(db, _jwtMock.Object, _configMock.Object);
            var command = new LoginUserCommand("test@example.com", password);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Token.Should().Be(TestToken);
        }

        [Fact]
        public async Task Handle_ValidCredentials_ShouldCallJwtServiceWithCorrectParameters()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var password = "password123";
            var passwordHash = SimplePasswordHasher.Hash(password, TestSalt);
            
            using var db = TestDbContextFactory.CreateWithData(ctx =>
            {
                ctx.Users.Add(new User
                {
                    Id = userId,
                    Email = "test@example.com",
                    DisplayName = "Test User",
                    PasswordHash = passwordHash,
                    Role = Role.Admin
                });
            });
            var handler = new LoginUserHandler(db, _jwtMock.Object, _configMock.Object);
            var command = new LoginUserCommand("test@example.com", password);

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert
            _jwtMock.Verify(j => j.GenerateToken(userId, "test@example.com", "Admin"), Times.Once);
        }

        [Fact]
        public async Task Handle_InvalidEmail_ShouldThrowUnauthorizedAccessException()
        {
            // Arrange
            using var db = TestDbContextFactory.CreateWithData(ctx =>
            {
                ctx.Users.Add(new User
                {
                    Id = Guid.NewGuid(),
                    Email = "existing@example.com",
                    DisplayName = "Test User",
                    PasswordHash = SimplePasswordHasher.Hash("password", TestSalt)
                });
            });
            var handler = new LoginUserHandler(db, _jwtMock.Object, _configMock.Object);
            var command = new LoginUserCommand("nonexistent@example.com", "password");

            // Act
            var act = async () => await handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Invalid credentials");
        }

        [Fact]
        public async Task Handle_InvalidPassword_ShouldThrowUnauthorizedAccessException()
        {
            // Arrange
            using var db = TestDbContextFactory.CreateWithData(ctx =>
            {
                ctx.Users.Add(new User
                {
                    Id = Guid.NewGuid(),
                    Email = "test@example.com",
                    DisplayName = "Test User",
                    PasswordHash = SimplePasswordHasher.Hash("correctPassword", TestSalt)
                });
            });
            var handler = new LoginUserHandler(db, _jwtMock.Object, _configMock.Object);
            var command = new LoginUserCommand("test@example.com", "wrongPassword");

            // Act
            var act = async () => await handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Invalid credentials");
        }

        [Fact]
        public async Task Handle_WithDefaultSalt_ShouldUseDefaultValue()
        {
            // Arrange
            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["DEMO_SALT"]).Returns((string?)null);
            
            var password = "password123";
            var passwordHash = SimplePasswordHasher.Hash(password, "demo-salt"); // default salt
            
            using var db = TestDbContextFactory.CreateWithData(ctx =>
            {
                ctx.Users.Add(new User
                {
                    Id = Guid.NewGuid(),
                    Email = "test@example.com",
                    DisplayName = "Test User",
                    PasswordHash = passwordHash,
                    Role = Role.Member
                });
            });
            var handler = new LoginUserHandler(db, _jwtMock.Object, configMock.Object);
            var command = new LoginUserCommand("test@example.com", password);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Token.Should().Be(TestToken);
        }

        [Theory]
        [InlineData(Role.Member, "Member")]
        [InlineData(Role.Admin, "Admin")]
        public async Task Handle_DifferentRoles_ShouldPassCorrectRoleToJwtService(Role role, string expectedRoleString)
        {
            // Arrange
            var userId = Guid.NewGuid();
            var password = "password123";
            var passwordHash = SimplePasswordHasher.Hash(password, TestSalt);
            
            using var db = TestDbContextFactory.CreateWithData(ctx =>
            {
                ctx.Users.Add(new User
                {
                    Id = userId,
                    Email = "test@example.com",
                    DisplayName = "Test User",
                    PasswordHash = passwordHash,
                    Role = role
                });
            });
            var handler = new LoginUserHandler(db, _jwtMock.Object, _configMock.Object);
            var command = new LoginUserCommand("test@example.com", password);

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert
            _jwtMock.Verify(j => j.GenerateToken(userId, "test@example.com", expectedRoleString), Times.Once);
        }
    }
}
