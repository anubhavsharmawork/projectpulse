using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.Projects.Commands;
using Application.UnitTests.TestHelpers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;
using Xunit;

namespace Application.UnitTests.Projects
{
    public class CreateProjectHandlerTests
    {
        private Mock<IHttpContextAccessor> CreateHttpContextAccessor(Guid? userId = null)
        {
            var mock = new Mock<IHttpContextAccessor>();
            var httpContext = new DefaultHttpContext();

            if (userId.HasValue)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString())
                };
                var identity = new ClaimsIdentity(claims, "Test");
                httpContext.User = new ClaimsPrincipal(identity);
            }

            mock.Setup(x => x.HttpContext).Returns(httpContext);
            return mock;
        }

        [Fact]
        public async Task Handle_ValidCommand_ShouldCreateProject()
        {
            // Arrange
            var ownerId = Guid.NewGuid();
            using var db = TestDbContextFactory.Create();
            var httpMock = CreateHttpContextAccessor(ownerId);
            var handler = new CreateProjectHandler(db, httpMock.Object);
            var command = new CreateProjectCommand("Test Project", "Test Description");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.ProjectId.Should().NotBe(Guid.Empty);

            var project = await db.Projects.SingleOrDefaultAsync(p => p.Id == result.ProjectId);
            project.Should().NotBeNull();
            project!.Name.Should().Be("Test Project");
            project.Description.Should().Be("Test Description");
            project.OwnerId.Should().Be(ownerId);
        }

        [Fact]
        public async Task Handle_NullDescription_ShouldCreateProjectWithNullDescription()
        {
            // Arrange
            var ownerId = Guid.NewGuid();
            using var db = TestDbContextFactory.Create();
            var httpMock = CreateHttpContextAccessor(ownerId);
            var handler = new CreateProjectHandler(db, httpMock.Object);
            var command = new CreateProjectCommand("Test Project", null);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            var project = await db.Projects.SingleAsync(p => p.Id == result.ProjectId);
            project.Name.Should().Be("Test Project");
            project.Description.Should().BeNull();
        }

        [Fact]
        public async Task Handle_NoAuthenticatedUser_ShouldUseEmptyGuidAsOwner()
        {
            // Arrange
            using var db = TestDbContextFactory.Create();
            var httpMock = new Mock<IHttpContextAccessor>();
            httpMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);
            var handler = new CreateProjectHandler(db, httpMock.Object);
            var command = new CreateProjectCommand("Test Project", null);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            var project = await db.Projects.SingleAsync(p => p.Id == result.ProjectId);
            project.OwnerId.Should().Be(Guid.Empty);
        }

        [Fact]
        public async Task Handle_InvalidUserIdClaim_ShouldUseEmptyGuidAsOwner()
        {
            // Arrange
            using var db = TestDbContextFactory.Create();
            var httpMock = new Mock<IHttpContextAccessor>();
            var httpContext = new DefaultHttpContext();
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "not-a-valid-guid")
            };
            var identity = new ClaimsIdentity(claims, "Test");
            httpContext.User = new ClaimsPrincipal(identity);
            httpMock.Setup(x => x.HttpContext).Returns(httpContext);
            
            var handler = new CreateProjectHandler(db, httpMock.Object);
            var command = new CreateProjectCommand("Test Project", null);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            var project = await db.Projects.SingleAsync(p => p.Id == result.ProjectId);
            project.OwnerId.Should().Be(Guid.Empty);
        }

        [Fact]
        public async Task Handle_ShouldSetCreatedAtToNow()
        {
            // Arrange
            var ownerId = Guid.NewGuid();
            using var db = TestDbContextFactory.Create();
            var httpMock = CreateHttpContextAccessor(ownerId);
            var handler = new CreateProjectHandler(db, httpMock.Object);
            var command = new CreateProjectCommand("Test Project", "Description");
            var beforeCreation = DateTime.UtcNow;

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            var project = await db.Projects.SingleAsync(p => p.Id == result.ProjectId);
            project.CreatedAt.Should().BeOnOrAfter(beforeCreation);
            project.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task Handle_MultipleProjects_ShouldCreateUniqueIds()
        {
            // Arrange
            var ownerId = Guid.NewGuid();
            using var db = TestDbContextFactory.Create();
            var httpMock = CreateHttpContextAccessor(ownerId);
            var handler = new CreateProjectHandler(db, httpMock.Object);

            // Act
            var result1 = await handler.Handle(new CreateProjectCommand("Project 1", null), CancellationToken.None);
            var result2 = await handler.Handle(new CreateProjectCommand("Project 2", null), CancellationToken.None);

            // Assert
            result1.ProjectId.Should().NotBe(result2.ProjectId);
            (await db.Projects.CountAsync()).Should().Be(2);
        }
    }
}
