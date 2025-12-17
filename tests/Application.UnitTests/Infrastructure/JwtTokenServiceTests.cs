using System;
using FluentAssertions;
using Infrastructure;
using Microsoft.Extensions.Configuration;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Xunit;

namespace Application.UnitTests.Infrastructure
{
    public class JwtTokenServiceTests
    {
        [Fact]
        public void GenerateToken_WithValidConfig_ShouldReturnValidJwt()
        {
            // Arrange
            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["JWT:Key"]).Returns("test-secret-key-at-least-32-bytes-long");
            configMock.Setup(c => c["JWT:Issuer"]).Returns("test-issuer");
            configMock.Setup(c => c["JWT:Audience"]).Returns("test-audience");
            
            var service = new JwtTokenService(configMock.Object);
            var userId = Guid.NewGuid();

            // Act
            var token = service.GenerateToken(userId, "test@example.com", "Member");

            // Assert
            token.Should().NotBeNullOrEmpty();
            
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            jwtToken.Should().NotBeNull();
        }

        [Fact]
        public void GenerateToken_ShouldContainCorrectClaims()
        {
            // Arrange
            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["JWT:Key"]).Returns("test-secret-key-at-least-32-bytes-long");
            configMock.Setup(c => c["JWT:Issuer"]).Returns("test-issuer");
            configMock.Setup(c => c["JWT:Audience"]).Returns("test-audience");
            
            var service = new JwtTokenService(configMock.Object);
            var userId = Guid.NewGuid();
            var email = "test@example.com";
            var role = "Admin";

            // Act
            var token = service.GenerateToken(userId, email, role);

            // Assert
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            
            jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == userId.ToString());
            jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == email);
            jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == role);
            jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == userId.ToString());
        }

        [Fact]
        public void GenerateToken_ShouldSetExpirationTo8Hours()
        {
            // Arrange
            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["JWT:Key"]).Returns("test-secret-key-at-least-32-bytes-long");
            
            var service = new JwtTokenService(configMock.Object);
            var beforeGeneration = DateTime.UtcNow;

            // Act
            var token = service.GenerateToken(Guid.NewGuid(), "test@example.com", "Member");

            // Assert
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            
            jwtToken.ValidTo.Should().BeCloseTo(beforeGeneration.AddHours(8), TimeSpan.FromMinutes(1));
        }

        [Fact]
        public void GenerateToken_WithShortKey_ShouldUseFallbackKey()
        {
            // Arrange
            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["JWT:Key"]).Returns("short");
            
            var service = new JwtTokenService(configMock.Object);

            // Act
            var token = service.GenerateToken(Guid.NewGuid(), "test@example.com", "Member");

            // Assert
            token.Should().NotBeNullOrEmpty();
            
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            jwtToken.Should().NotBeNull();
        }

        [Fact]
        public void GenerateToken_WithNullKey_ShouldUseFallbackKey()
        {
            // Arrange
            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["JWT:Key"]).Returns((string?)null);
            
            var service = new JwtTokenService(configMock.Object);

            // Act
            var token = service.GenerateToken(Guid.NewGuid(), "test@example.com", "Member");

            // Assert
            token.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void GenerateToken_WithEmptyKey_ShouldUseFallbackKey()
        {
            // Arrange
            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["JWT:Key"]).Returns("");
            
            var service = new JwtTokenService(configMock.Object);

            // Act
            var token = service.GenerateToken(Guid.NewGuid(), "test@example.com", "Member");

            // Assert
            token.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void GenerateToken_WithNullIssuerAndAudience_ShouldStillWork()
        {
            // Arrange
            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["JWT:Key"]).Returns("test-secret-key-at-least-32-bytes-long");
            configMock.Setup(c => c["JWT:Issuer"]).Returns((string?)null);
            configMock.Setup(c => c["JWT:Audience"]).Returns((string?)null);
            
            var service = new JwtTokenService(configMock.Object);

            // Act
            var token = service.GenerateToken(Guid.NewGuid(), "test@example.com", "Member");

            // Assert
            token.Should().NotBeNullOrEmpty();
        }

        [Theory]
        [InlineData("Member")]
        [InlineData("Admin")]
        public void GenerateToken_DifferentRoles_ShouldIncludeRoleInToken(string role)
        {
            // Arrange
            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["JWT:Key"]).Returns("test-secret-key-at-least-32-bytes-long");
            
            var service = new JwtTokenService(configMock.Object);

            // Act
            var token = service.GenerateToken(Guid.NewGuid(), "test@example.com", role);

            // Assert
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == role);
        }

        [Fact]
        public void GenerateToken_DifferentUsers_ShouldProduceDifferentTokens()
        {
            // Arrange
            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["JWT:Key"]).Returns("test-secret-key-at-least-32-bytes-long");
            
            var service = new JwtTokenService(configMock.Object);

            // Act
            var token1 = service.GenerateToken(Guid.NewGuid(), "user1@example.com", "Member");
            var token2 = service.GenerateToken(Guid.NewGuid(), "user2@example.com", "Member");

            // Assert
            token1.Should().NotBe(token2);
        }
    }
}
