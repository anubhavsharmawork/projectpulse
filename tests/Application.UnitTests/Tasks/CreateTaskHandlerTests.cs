using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Tasks.Commands;
using Application.UnitTests.TestHelpers;
using Domain.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Application.UnitTests.Tasks
{
    public class CreateTaskHandlerTests
    {
        [Fact]
        public async Task Handle_ValidCommand_ShouldCreateTask()
        {
            // Arrange
            var projectId = Guid.NewGuid();
            using var db = TestDbContextFactory.Create();
            var handler = new CreateTaskHandler(db);
            var command = new CreateTaskCommand(projectId, "Test Task", "Test Description");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.TaskId.Should().NotBe(Guid.Empty);

            var task = await db.WorkItems.OfType<TaskWorkItem>().SingleOrDefaultAsync(t => t.Id == result.TaskId);
            task.Should().NotBeNull();
            task!.Title.Should().Be("Test Task");
            task.Description.Should().Be("Test Description");
            task.ProjectId.Should().Be(projectId);
            task.Type.Should().Be(WorkItemType.Task);
            task.IsCompleted.Should().BeFalse();
        }

        [Fact]
        public async Task Handle_NullDescription_ShouldCreateTaskWithNullDescription()
        {
            // Arrange
            var projectId = Guid.NewGuid();
            using var db = TestDbContextFactory.Create();
            var handler = new CreateTaskHandler(db);
            var command = new CreateTaskCommand(projectId, "Test Task", null);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            var task = await db.WorkItems.OfType<TaskWorkItem>().SingleAsync(t => t.Id == result.TaskId);
            task.Description.Should().BeNull();
        }

        [Fact]
        public async Task Handle_WithAttachmentUrl_ShouldCreateTaskWithAttachment()
        {
            // Arrange
            var projectId = Guid.NewGuid();
            using var db = TestDbContextFactory.Create();
            var handler = new CreateTaskHandler(db);
            var command = new CreateTaskCommand(projectId, "Test Task", null, "https://example.com/file.pdf");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            var task = await db.WorkItems.OfType<TaskWorkItem>().SingleAsync(t => t.Id == result.TaskId);
            task.AttachmentUrl.Should().Be("https://example.com/file.pdf");
        }

        [Fact]
        public async Task Handle_WithParentId_ShouldCreateTaskWithParent()
        {
            // Arrange
            var projectId = Guid.NewGuid();
            var parentId = Guid.NewGuid();
            using var db = TestDbContextFactory.Create();
            var handler = new CreateTaskHandler(db);
            var command = new CreateTaskCommand(projectId, "Test Task", null, null, parentId);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            var task = await db.WorkItems.OfType<TaskWorkItem>().SingleAsync(t => t.Id == result.TaskId);
            task.ParentId.Should().Be(parentId);
        }

        [Fact]
        public async Task Handle_WithoutParentId_ShouldCreateOrphanTask()
        {
            // Arrange
            var projectId = Guid.NewGuid();
            using var db = TestDbContextFactory.Create();
            var handler = new CreateTaskHandler(db);
            var command = new CreateTaskCommand(projectId, "Test Task", "Description");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            var task = await db.WorkItems.OfType<TaskWorkItem>().SingleAsync(t => t.Id == result.TaskId);
            task.ParentId.Should().BeNull();
        }

        [Fact]
        public async Task Handle_ShouldSetIsCompletedToFalse()
        {
            // Arrange
            var projectId = Guid.NewGuid();
            using var db = TestDbContextFactory.Create();
            var handler = new CreateTaskHandler(db);
            var command = new CreateTaskCommand(projectId, "Test Task", null);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            var task = await db.WorkItems.OfType<TaskWorkItem>().SingleAsync(t => t.Id == result.TaskId);
            task.IsCompleted.Should().BeFalse();
            task.CompletedAt.Should().BeNull();
        }

        [Fact]
        public async Task Handle_ShouldSetCreatedAtToNow()
        {
            // Arrange
            var projectId = Guid.NewGuid();
            using var db = TestDbContextFactory.Create();
            var handler = new CreateTaskHandler(db);
            var command = new CreateTaskCommand(projectId, "Test Task", null);
            var beforeCreation = DateTime.UtcNow;

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            var task = await db.WorkItems.OfType<TaskWorkItem>().SingleAsync(t => t.Id == result.TaskId);
            task.CreatedAt.Should().BeOnOrAfter(beforeCreation);
            task.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task Handle_MultipleTasks_ShouldCreateUniqueIds()
        {
            // Arrange
            var projectId = Guid.NewGuid();
            using var db = TestDbContextFactory.Create();
            var handler = new CreateTaskHandler(db);

            // Act
            var result1 = await handler.Handle(new CreateTaskCommand(projectId, "Task 1", null), CancellationToken.None);
            var result2 = await handler.Handle(new CreateTaskCommand(projectId, "Task 2", null), CancellationToken.None);

            // Assert
            result1.TaskId.Should().NotBe(result2.TaskId);
            (await db.WorkItems.OfType<TaskWorkItem>().CountAsync()).Should().Be(2);
        }
    }
}
