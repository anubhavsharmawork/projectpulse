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
    public class CreateEpicHandlerTests
    {
        [Fact]
        public async Task Handle_ValidCommand_ShouldCreateEpic()
        {
            // Arrange
            var projectId = Guid.NewGuid();
            using var db = TestDbContextFactory.Create();
            var handler = new CreateEpicHandler(db);
            var command = new CreateEpicCommand(projectId, "Test Epic", "Test Description");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.EpicId.Should().NotBe(Guid.Empty);

            var epic = await db.WorkItems.OfType<EpicWorkItem>().SingleOrDefaultAsync(e => e.Id == result.EpicId);
            epic.Should().NotBeNull();
            epic!.Title.Should().Be("Test Epic");
            epic.Description.Should().Be("Test Description");
            epic.ProjectId.Should().Be(projectId);
            epic.Type.Should().Be(WorkItemType.Epic);
            epic.IsCompleted.Should().BeFalse();
        }

        [Fact]
        public async Task Handle_NullDescription_ShouldCreateEpicWithNullDescription()
        {
            // Arrange
            var projectId = Guid.NewGuid();
            using var db = TestDbContextFactory.Create();
            var handler = new CreateEpicHandler(db);
            var command = new CreateEpicCommand(projectId, "Test Epic", null);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            var epic = await db.WorkItems.OfType<EpicWorkItem>().SingleAsync(e => e.Id == result.EpicId);
            epic.Description.Should().BeNull();
        }

        [Fact]
        public async Task Handle_WithAttachmentUrl_ShouldCreateEpicWithAttachment()
        {
            // Arrange
            var projectId = Guid.NewGuid();
            using var db = TestDbContextFactory.Create();
            var handler = new CreateEpicHandler(db);
            var command = new CreateEpicCommand(projectId, "Test Epic", null, "https://example.com/file.pdf");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            var epic = await db.WorkItems.OfType<EpicWorkItem>().SingleAsync(e => e.Id == result.EpicId);
            epic.AttachmentUrl.Should().Be("https://example.com/file.pdf");
        }

        [Fact]
        public async Task Handle_ShouldSetIsCompletedToFalse()
        {
            // Arrange
            var projectId = Guid.NewGuid();
            using var db = TestDbContextFactory.Create();
            var handler = new CreateEpicHandler(db);
            var command = new CreateEpicCommand(projectId, "Test Epic", null);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            var epic = await db.WorkItems.OfType<EpicWorkItem>().SingleAsync(e => e.Id == result.EpicId);
            epic.IsCompleted.Should().BeFalse();
            epic.CompletedAt.Should().BeNull();
        }

        [Fact]
        public async Task Handle_ShouldSetCreatedAtToNow()
        {
            // Arrange
            var projectId = Guid.NewGuid();
            using var db = TestDbContextFactory.Create();
            var handler = new CreateEpicHandler(db);
            var command = new CreateEpicCommand(projectId, "Test Epic", null);
            var beforeCreation = DateTime.UtcNow;

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            var epic = await db.WorkItems.OfType<EpicWorkItem>().SingleAsync(e => e.Id == result.EpicId);
            epic.CreatedAt.Should().BeOnOrAfter(beforeCreation);
            epic.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task Handle_MultipleEpics_ShouldCreateUniqueIds()
        {
            // Arrange
            var projectId = Guid.NewGuid();
            using var db = TestDbContextFactory.Create();
            var handler = new CreateEpicHandler(db);

            // Act
            var result1 = await handler.Handle(new CreateEpicCommand(projectId, "Epic 1", null), CancellationToken.None);
            var result2 = await handler.Handle(new CreateEpicCommand(projectId, "Epic 2", null), CancellationToken.None);

            // Assert
            result1.EpicId.Should().NotBe(result2.EpicId);
            (await db.WorkItems.OfType<EpicWorkItem>().CountAsync()).Should().Be(2);
        }
    }
}
