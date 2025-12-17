using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.Comments.Commands;
using Application.UnitTests.TestHelpers;
using Domain.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;
using Xunit;

namespace Application.UnitTests.Comments
{
    public class CreateCommentHandlerTests
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
        public async Task Handle_ValidCommand_ShouldCreateComment()
        {
            // Arrange
            var authorId = Guid.NewGuid();
            var workItemId = Guid.NewGuid();
            using var db = TestDbContextFactory.Create();
            var httpMock = CreateHttpContextAccessor(authorId);
            var handler = new CreateCommentHandler(db, httpMock.Object);
            var command = new CreateCommentCommand(workItemId, "This is a test comment");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.CommentId.Should().NotBe(Guid.Empty);

            var comment = await db.Comments.SingleOrDefaultAsync(c => c.Id == result.CommentId);
            comment.Should().NotBeNull();
            comment!.Body.Should().Be("This is a test comment");
            comment.WorkItemId.Should().Be(workItemId);
            comment.AuthorId.Should().Be(authorId);
        }

        [Fact]
        public async Task Handle_EmptyBody_ShouldThrowArgumentException()
        {
            // Arrange
            var authorId = Guid.NewGuid();
            var workItemId = Guid.NewGuid();
            using var db = TestDbContextFactory.Create();
            var httpMock = CreateHttpContextAccessor(authorId);
            var handler = new CreateCommentHandler(db, httpMock.Object);
            var command = new CreateCommentCommand(workItemId, "");

            // Act
            var act = async () => await handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("Comment body is required*");
        }

        [Fact]
        public async Task Handle_WhitespaceBody_ShouldThrowArgumentException()
        {
            // Arrange
            var authorId = Guid.NewGuid();
            var workItemId = Guid.NewGuid();
            using var db = TestDbContextFactory.Create();
            var httpMock = CreateHttpContextAccessor(authorId);
            var handler = new CreateCommentHandler(db, httpMock.Object);
            var command = new CreateCommentCommand(workItemId, "   ");

            // Act
            var act = async () => await handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task Handle_ShouldTrimBody()
        {
            // Arrange
            var authorId = Guid.NewGuid();
            var workItemId = Guid.NewGuid();
            using var db = TestDbContextFactory.Create();
            var httpMock = CreateHttpContextAccessor(authorId);
            var handler = new CreateCommentHandler(db, httpMock.Object);
            var command = new CreateCommentCommand(workItemId, "  Test comment with whitespace  ");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            var comment = await db.Comments.SingleAsync(c => c.Id == result.CommentId);
            comment.Body.Should().Be("Test comment with whitespace");
        }

        [Fact]
        public async Task Handle_NoAuthenticatedUser_ShouldUseEmptyGuid()
        {
            // Arrange
            var workItemId = Guid.NewGuid();
            using var db = TestDbContextFactory.Create();
            var httpMock = new Mock<IHttpContextAccessor>();
            httpMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);
            var handler = new CreateCommentHandler(db, httpMock.Object);
            var command = new CreateCommentCommand(workItemId, "Test comment");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            var comment = await db.Comments.SingleAsync(c => c.Id == result.CommentId);
            comment.AuthorId.Should().Be(Guid.Empty);
        }

        [Fact]
        public async Task Handle_WithMentions_ShouldExtractMentionedUserIds()
        {
            // Arrange
            var authorId = Guid.NewGuid();
            var mentionedUserId = Guid.NewGuid();
            var workItemId = Guid.NewGuid();
            
            using var db = TestDbContextFactory.CreateWithData(ctx =>
            {
                ctx.Users.Add(new User
                {
                    Id = mentionedUserId,
                    Email = "john@example.com",
                    DisplayName = "johndoe",
                    PasswordHash = "hash"
                });
                ctx.WorkItems.Add(new TaskWorkItem
                {
                    Id = workItemId,
                    ProjectId = Guid.NewGuid(),
                    Title = "Test Task"
                });
            });
            
            var httpMock = CreateHttpContextAccessor(authorId);
            var handler = new CreateCommentHandler(db, httpMock.Object);
            var command = new CreateCommentCommand(workItemId, "Hey @johndoe check this out!");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            var comment = await db.Comments.SingleAsync(c => c.Id == result.CommentId);
            comment.MentionedUserIds.Should().Contain(mentionedUserId);
        }

        [Fact]
        public async Task Handle_WithMentions_ShouldCreateNotifications()
        {
            // Arrange
            var authorId = Guid.NewGuid();
            var mentionedUserId = Guid.NewGuid();
            var workItemId = Guid.NewGuid();
            
            using var db = TestDbContextFactory.CreateWithData(ctx =>
            {
                ctx.Users.Add(new User
                {
                    Id = authorId,
                    Email = "author@example.com",
                    DisplayName = "Author",
                    PasswordHash = "hash"
                });
                ctx.Users.Add(new User
                {
                    Id = mentionedUserId,
                    Email = "john@example.com",
                    DisplayName = "johndoe",
                    PasswordHash = "hash"
                });
                ctx.WorkItems.Add(new TaskWorkItem
                {
                    Id = workItemId,
                    ProjectId = Guid.NewGuid(),
                    Title = "Test Task"
                });
            });
            
            var httpMock = CreateHttpContextAccessor(authorId);
            var handler = new CreateCommentHandler(db, httpMock.Object);
            var command = new CreateCommentCommand(workItemId, "Hey @johndoe check this!");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            var notification = await db.MentionNotifications.SingleOrDefaultAsync(n => n.UserId == mentionedUserId);
            notification.Should().NotBeNull();
            notification!.MentionedByUserId.Should().Be(authorId);
            notification.WorkItemId.Should().Be(workItemId);
            notification.WorkItemTitle.Should().Be("Test Task");
            notification.MentionedByName.Should().Be("Author");
            notification.IsRead.Should().BeFalse();
        }

        [Fact]
        public async Task Handle_AuthorMentionsSelf_ShouldNotCreateNotification()
        {
            // Arrange
            var authorId = Guid.NewGuid();
            var workItemId = Guid.NewGuid();
            
            using var db = TestDbContextFactory.CreateWithData(ctx =>
            {
                ctx.Users.Add(new User
                {
                    Id = authorId,
                    Email = "author@example.com",
                    DisplayName = "author",
                    PasswordHash = "hash"
                });
                ctx.WorkItems.Add(new TaskWorkItem
                {
                    Id = workItemId,
                    ProjectId = Guid.NewGuid(),
                    Title = "Test Task"
                });
            });
            
            var httpMock = CreateHttpContextAccessor(authorId);
            var handler = new CreateCommentHandler(db, httpMock.Object);
            var command = new CreateCommentCommand(workItemId, "Note to @author self");

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert
            var notifications = await db.MentionNotifications.ToListAsync();
            notifications.Should().BeEmpty();
        }

        [Fact]
        public async Task Handle_NoMentions_ShouldNotCreateNotifications()
        {
            // Arrange
            var authorId = Guid.NewGuid();
            var workItemId = Guid.NewGuid();
            using var db = TestDbContextFactory.Create();
            var httpMock = CreateHttpContextAccessor(authorId);
            var handler = new CreateCommentHandler(db, httpMock.Object);
            var command = new CreateCommentCommand(workItemId, "A simple comment without mentions");

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert
            var notifications = await db.MentionNotifications.ToListAsync();
            notifications.Should().BeEmpty();
        }

        [Fact]
        public async Task Handle_LongComment_ShouldTruncateNotificationBody()
        {
            // Arrange
            var authorId = Guid.NewGuid();
            var mentionedUserId = Guid.NewGuid();
            var workItemId = Guid.NewGuid();
            var longComment = "@johndoe " + new string('a', 200);
            
            using var db = TestDbContextFactory.CreateWithData(ctx =>
            {
                ctx.Users.Add(new User
                {
                    Id = authorId,
                    Email = "author@example.com",
                    DisplayName = "Author",
                    PasswordHash = "hash"
                });
                ctx.Users.Add(new User
                {
                    Id = mentionedUserId,
                    Email = "john@example.com",
                    DisplayName = "johndoe",
                    PasswordHash = "hash"
                });
                ctx.WorkItems.Add(new TaskWorkItem
                {
                    Id = workItemId,
                    ProjectId = Guid.NewGuid(),
                    Title = "Test Task"
                });
            });
            
            var httpMock = CreateHttpContextAccessor(authorId);
            var handler = new CreateCommentHandler(db, httpMock.Object);
            var command = new CreateCommentCommand(workItemId, longComment);

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert
            var notification = await db.MentionNotifications.SingleAsync(n => n.UserId == mentionedUserId);
            notification.CommentBody.Length.Should().Be(103); // 100 + "..."
            notification.CommentBody.Should().EndWith("...");
        }

        [Fact]
        public async Task Handle_QuotedMention_ShouldExtractDisplayName()
        {
            // Arrange
            var authorId = Guid.NewGuid();
            var mentionedUserId = Guid.NewGuid();
            var workItemId = Guid.NewGuid();
            
            using var db = TestDbContextFactory.CreateWithData(ctx =>
            {
                ctx.Users.Add(new User
                {
                    Id = mentionedUserId,
                    Email = "john@example.com",
                    DisplayName = "John Doe",
                    PasswordHash = "hash"
                });
                ctx.WorkItems.Add(new TaskWorkItem
                {
                    Id = workItemId,
                    ProjectId = Guid.NewGuid(),
                    Title = "Test Task"
                });
            });
            
            var httpMock = CreateHttpContextAccessor(authorId);
            var handler = new CreateCommentHandler(db, httpMock.Object);
            var command = new CreateCommentCommand(workItemId, "Hey @\"John Doe\" can you review?");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            var comment = await db.Comments.SingleAsync(c => c.Id == result.CommentId);
            comment.MentionedUserIds.Should().Contain(mentionedUserId);
        }

        [Fact]
        public async Task Handle_ShouldSetCreatedAtToNow()
        {
            // Arrange
            var authorId = Guid.NewGuid();
            var workItemId = Guid.NewGuid();
            using var db = TestDbContextFactory.Create();
            var httpMock = CreateHttpContextAccessor(authorId);
            var handler = new CreateCommentHandler(db, httpMock.Object);
            var command = new CreateCommentCommand(workItemId, "Test comment");
            var beforeCreation = DateTime.UtcNow;

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            var comment = await db.Comments.SingleAsync(c => c.Id == result.CommentId);
            comment.CreatedAt.Should().BeOnOrAfter(beforeCreation);
            comment.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }
    }
}
