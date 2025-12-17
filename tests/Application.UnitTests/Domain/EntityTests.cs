using System;
using System.Collections.Generic;
using Domain.Entities;
using FluentAssertions;
using Xunit;

namespace Application.UnitTests.Domain
{
    public class EntityTests
    {
        #region User Entity Tests
        
        [Fact]
        public void User_DefaultValues_ShouldBeCorrect()
        {
            var user = new User();

            user.Id.Should().Be(Guid.Empty);
            user.Email.Should().BeEmpty();
            user.PasswordHash.Should().BeEmpty();
            user.DisplayName.Should().BeEmpty();
            user.Role.Should().Be(Role.Member);
            user.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public void User_SetProperties_ShouldWorkCorrectly()
        {
            var id = Guid.NewGuid();
            var createdAt = DateTime.UtcNow.AddDays(-1);
            
            var user = new User
            {
                Id = id,
                Email = "test@example.com",
                PasswordHash = "hashedpassword",
                DisplayName = "Test User",
                Role = Role.Admin,
                CreatedAt = createdAt
            };

            user.Id.Should().Be(id);
            user.Email.Should().Be("test@example.com");
            user.PasswordHash.Should().Be("hashedpassword");
            user.DisplayName.Should().Be("Test User");
            user.Role.Should().Be(Role.Admin);
            user.CreatedAt.Should().Be(createdAt);
        }

        [Theory]
        [InlineData(Role.Member, 0)]
        [InlineData(Role.Admin, 1)]
        public void Role_Values_ShouldBeCorrect(Role role, int expectedValue)
        {
            ((int)role).Should().Be(expectedValue);
        }

        #endregion

        #region Project Entity Tests

        [Fact]
        public void Project_DefaultValues_ShouldBeCorrect()
        {
            var project = new Project();

            project.Id.Should().Be(Guid.Empty);
            project.Name.Should().BeEmpty();
            project.Description.Should().BeNull();
            project.OwnerId.Should().Be(Guid.Empty);
            project.WorkItems.Should().NotBeNull().And.BeEmpty();
            project.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public void Project_SetProperties_ShouldWorkCorrectly()
        {
            var id = Guid.NewGuid();
            var ownerId = Guid.NewGuid();
            var createdAt = DateTime.UtcNow.AddDays(-1);
            var workItems = new List<WorkItem>();

            var project = new Project
            {
                Id = id,
                Name = "Test Project",
                Description = "Test Description",
                OwnerId = ownerId,
                WorkItems = workItems,
                CreatedAt = createdAt
            };

            project.Id.Should().Be(id);
            project.Name.Should().Be("Test Project");
            project.Description.Should().Be("Test Description");
            project.OwnerId.Should().Be(ownerId);
            project.WorkItems.Should().BeSameAs(workItems);
            project.CreatedAt.Should().Be(createdAt);
        }

        #endregion

        #region Comment Entity Tests

        [Fact]
        public void Comment_DefaultValues_ShouldBeCorrect()
        {
            var comment = new Comment();

            comment.Id.Should().Be(Guid.Empty);
            comment.WorkItemId.Should().Be(Guid.Empty);
            comment.AuthorId.Should().Be(Guid.Empty);
            comment.Body.Should().BeEmpty();
            comment.MentionedUserIds.Should().NotBeNull().And.BeEmpty();
            comment.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public void Comment_SetProperties_ShouldWorkCorrectly()
        {
            var id = Guid.NewGuid();
            var workItemId = Guid.NewGuid();
            var authorId = Guid.NewGuid();
            var mentionedUserIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
            var createdAt = DateTime.UtcNow.AddDays(-1);

            var comment = new Comment
            {
                Id = id,
                WorkItemId = workItemId,
                AuthorId = authorId,
                Body = "Test comment body",
                MentionedUserIds = mentionedUserIds,
                CreatedAt = createdAt
            };

            comment.Id.Should().Be(id);
            comment.WorkItemId.Should().Be(workItemId);
            comment.AuthorId.Should().Be(authorId);
            comment.Body.Should().Be("Test comment body");
            comment.MentionedUserIds.Should().BeSameAs(mentionedUserIds);
            comment.CreatedAt.Should().Be(createdAt);
        }

        #endregion

        #region MentionNotification Entity Tests

        [Fact]
        public void MentionNotification_DefaultValues_ShouldBeCorrect()
        {
            var notification = new MentionNotification();

            notification.Id.Should().Be(Guid.Empty);
            notification.UserId.Should().Be(Guid.Empty);
            notification.CommentId.Should().Be(Guid.Empty);
            notification.WorkItemId.Should().Be(Guid.Empty);
            notification.MentionedByUserId.Should().Be(Guid.Empty);
            notification.CommentBody.Should().BeEmpty();
            notification.WorkItemTitle.Should().BeEmpty();
            notification.MentionedByName.Should().BeEmpty();
            notification.IsRead.Should().BeFalse();
            notification.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public void MentionNotification_SetProperties_ShouldWorkCorrectly()
        {
            var id = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var commentId = Guid.NewGuid();
            var workItemId = Guid.NewGuid();
            var mentionedByUserId = Guid.NewGuid();
            var createdAt = DateTime.UtcNow.AddDays(-1);

            var notification = new MentionNotification
            {
                Id = id,
                UserId = userId,
                CommentId = commentId,
                WorkItemId = workItemId,
                MentionedByUserId = mentionedByUserId,
                CommentBody = "Test comment",
                WorkItemTitle = "Test Work Item",
                MentionedByName = "Test User",
                IsRead = true,
                CreatedAt = createdAt
            };

            notification.Id.Should().Be(id);
            notification.UserId.Should().Be(userId);
            notification.CommentId.Should().Be(commentId);
            notification.WorkItemId.Should().Be(workItemId);
            notification.MentionedByUserId.Should().Be(mentionedByUserId);
            notification.CommentBody.Should().Be("Test comment");
            notification.WorkItemTitle.Should().Be("Test Work Item");
            notification.MentionedByName.Should().Be("Test User");
            notification.IsRead.Should().BeTrue();
            notification.CreatedAt.Should().Be(createdAt);
        }

        #endregion

        #region WorkItem Entity Tests

        [Fact]
        public void EpicWorkItem_DefaultValues_ShouldBeCorrect()
        {
            var epic = new EpicWorkItem();

            epic.Id.Should().Be(Guid.Empty);
            epic.ProjectId.Should().Be(Guid.Empty);
            epic.ParentId.Should().BeNull();
            epic.Parent.Should().BeNull();
            epic.Children.Should().NotBeNull().And.BeEmpty();
            epic.Title.Should().BeEmpty();
            epic.Description.Should().BeNull();
            epic.AttachmentUrl.Should().BeNull();
            epic.IsCompleted.Should().BeFalse();
            epic.AssigneeId.Should().BeNull();
            epic.Comments.Should().NotBeNull().And.BeEmpty();
            epic.CompletedAt.Should().BeNull();
            epic.Type.Should().Be(WorkItemType.Epic);
            epic.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public void UserStoryWorkItem_DefaultValues_ShouldBeCorrect()
        {
            var userStory = new UserStoryWorkItem();

            userStory.Type.Should().Be(WorkItemType.UserStory);
            userStory.Title.Should().BeEmpty();
            userStory.ParentId.Should().BeNull();
        }

        [Fact]
        public void TaskWorkItem_DefaultValues_ShouldBeCorrect()
        {
            var task = new TaskWorkItem();

            task.Type.Should().Be(WorkItemType.Task);
            task.Title.Should().BeEmpty();
            task.ParentId.Should().BeNull();
        }

        [Theory]
        [InlineData(WorkItemType.Epic, 1)]
        [InlineData(WorkItemType.UserStory, 2)]
        [InlineData(WorkItemType.Task, 3)]
        public void WorkItemType_Values_ShouldBeCorrect(WorkItemType type, int expectedValue)
        {
            ((int)type).Should().Be(expectedValue);
        }

        [Fact]
        public void WorkItem_SetAllProperties_ShouldWorkCorrectly()
        {
            var id = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var parentId = Guid.NewGuid();
            var assigneeId = Guid.NewGuid();
            var createdAt = DateTime.UtcNow.AddDays(-2);
            var completedAt = DateTime.UtcNow.AddDays(-1);
            var parent = new EpicWorkItem();
            var children = new List<WorkItem> { new TaskWorkItem() };
            var comments = new List<Comment> { new Comment() };

            var task = new TaskWorkItem
            {
                Id = id,
                ProjectId = projectId,
                ParentId = parentId,
                Parent = parent,
                Children = children,
                Title = "Test Task",
                Description = "Test Description",
                AttachmentUrl = "https://example.com/attachment.pdf",
                IsCompleted = true,
                AssigneeId = assigneeId,
                Comments = comments,
                CreatedAt = createdAt,
                CompletedAt = completedAt
            };

            task.Id.Should().Be(id);
            task.ProjectId.Should().Be(projectId);
            task.ParentId.Should().Be(parentId);
            task.Parent.Should().BeSameAs(parent);
            task.Children.Should().BeSameAs(children);
            task.Title.Should().Be("Test Task");
            task.Description.Should().Be("Test Description");
            task.AttachmentUrl.Should().Be("https://example.com/attachment.pdf");
            task.IsCompleted.Should().BeTrue();
            task.AssigneeId.Should().Be(assigneeId);
            task.Comments.Should().BeSameAs(comments);
            task.CreatedAt.Should().Be(createdAt);
            task.CompletedAt.Should().Be(completedAt);
        }

        #endregion
    }
}
