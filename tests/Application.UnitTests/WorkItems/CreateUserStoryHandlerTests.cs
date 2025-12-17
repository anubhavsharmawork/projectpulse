using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.WorkItems.Commands;
using Application.UnitTests.TestHelpers;
using Domain.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Application.UnitTests.WorkItems
{
    public class CreateUserStoryHandlerTests
    {
        [Fact]
        public async Task Handle_ValidCommand_ShouldCreateUserStory()
        {
            // Arrange
            var projectId = Guid.NewGuid();
            using var db = TestDbContextFactory.Create();
            var handler = new CreateUserStoryHandler(db);
            var command = new CreateUserStoryCommand(projectId, "Test User Story", "Test Description");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.UserStoryId.Should().NotBe(Guid.Empty);

            var userStory = await db.WorkItems.OfType<UserStoryWorkItem>().SingleOrDefaultAsync(s => s.Id == result.UserStoryId);
            userStory.Should().NotBeNull();
            userStory!.Title.Should().Be("Test User Story");
            userStory.Description.Should().Be("Test Description");
            userStory.ProjectId.Should().Be(projectId);
            userStory.Type.Should().Be(WorkItemType.UserStory);
            userStory.IsCompleted.Should().BeFalse();
        }

        [Fact]
        public async Task Handle_NullDescription_ShouldCreateUserStoryWithNullDescription()
        {
            // Arrange
            var projectId = Guid.NewGuid();
            using var db = TestDbContextFactory.Create();
            var handler = new CreateUserStoryHandler(db);
            var command = new CreateUserStoryCommand(projectId, "Test User Story", null);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            var userStory = await db.WorkItems.OfType<UserStoryWorkItem>().SingleAsync(s => s.Id == result.UserStoryId);
            userStory.Description.Should().BeNull();
        }

        [Fact]
        public async Task Handle_WithAttachmentUrl_ShouldCreateUserStoryWithAttachment()
        {
            // Arrange
            var projectId = Guid.NewGuid();
            using var db = TestDbContextFactory.Create();
            var handler = new CreateUserStoryHandler(db);
            var command = new CreateUserStoryCommand(projectId, "Test User Story", null, "https://example.com/file.pdf");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            var userStory = await db.WorkItems.OfType<UserStoryWorkItem>().SingleAsync(s => s.Id == result.UserStoryId);
            userStory.AttachmentUrl.Should().Be("https://example.com/file.pdf");
        }

        [Fact]
        public async Task Handle_WithParentId_ShouldCreateUserStoryWithParent()
        {
            // Arrange
            var projectId = Guid.NewGuid();
            var parentId = Guid.NewGuid();
            using var db = TestDbContextFactory.Create();
            var handler = new CreateUserStoryHandler(db);
            var command = new CreateUserStoryCommand(projectId, "Test User Story", null, null, parentId);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            var userStory = await db.WorkItems.OfType<UserStoryWorkItem>().SingleAsync(s => s.Id == result.UserStoryId);
            userStory.ParentId.Should().Be(parentId);
        }

        [Fact]
        public async Task Handle_WithoutParentId_ShouldCreateOrphanUserStory()
        {
            // Arrange
            var projectId = Guid.NewGuid();
            using var db = TestDbContextFactory.Create();
            var handler = new CreateUserStoryHandler(db);
            var command = new CreateUserStoryCommand(projectId, "Test User Story", "Description");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            var userStory = await db.WorkItems.OfType<UserStoryWorkItem>().SingleAsync(s => s.Id == result.UserStoryId);
            userStory.ParentId.Should().BeNull();
        }

        [Fact]
        public async Task Handle_ShouldSetIsCompletedToFalse()
        {
            // Arrange
            var projectId = Guid.NewGuid();
            using var db = TestDbContextFactory.Create();
            var handler = new CreateUserStoryHandler(db);
            var command = new CreateUserStoryCommand(projectId, "Test User Story", null);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            var userStory = await db.WorkItems.OfType<UserStoryWorkItem>().SingleAsync(s => s.Id == result.UserStoryId);
            userStory.IsCompleted.Should().BeFalse();
            userStory.CompletedAt.Should().BeNull();
        }

        [Fact]
        public async Task Handle_ShouldSetCreatedAtToNow()
        {
            // Arrange
            var projectId = Guid.NewGuid();
            using var db = TestDbContextFactory.Create();
            var handler = new CreateUserStoryHandler(db);
            var command = new CreateUserStoryCommand(projectId, "Test User Story", null);
            var beforeCreation = DateTime.UtcNow;

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            var userStory = await db.WorkItems.OfType<UserStoryWorkItem>().SingleAsync(s => s.Id == result.UserStoryId);
            userStory.CreatedAt.Should().BeOnOrAfter(beforeCreation);
            userStory.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task Handle_MultipleUserStories_ShouldCreateUniqueIds()
        {
            // Arrange
            var projectId = Guid.NewGuid();
            using var db = TestDbContextFactory.Create();
            var handler = new CreateUserStoryHandler(db);

            // Act
            var result1 = await handler.Handle(new CreateUserStoryCommand(projectId, "Story 1", null), CancellationToken.None);
            var result2 = await handler.Handle(new CreateUserStoryCommand(projectId, "Story 2", null), CancellationToken.None);

            // Assert
            result1.UserStoryId.Should().NotBe(result2.UserStoryId);
            (await db.WorkItems.OfType<UserStoryWorkItem>().CountAsync()).Should().Be(2);
        }
    }
}
