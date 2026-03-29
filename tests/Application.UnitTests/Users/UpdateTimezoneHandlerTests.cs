using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Application.UnitTests.TestHelpers;
using Application.Users.Commands;
using Domain.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace Application.UnitTests.Users
{
    public class UpdateTimezoneHandlerTests
    {
        private static Mock<IHttpContextAccessor> CreateHttpAccessor(Guid? userId = null)
        {
            var mock = new Mock<IHttpContextAccessor>();
            var httpContext = new DefaultHttpContext();
            if (userId.HasValue)
            {
                httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()) }, "Test"));
            }
            mock.Setup(x => x.HttpContext).Returns(httpContext);
            return mock;
        }

        [Fact]
        public async Task Handle_UpdatesTimezone_ForExistingUser()
        {
            var userId = Guid.NewGuid();
            using var db = TestDbContextFactory.CreateWithData(ctx =>
            {
                ctx.Users.Add(new User { Id = userId, Email = "u@example.com", UserName = "u", PasswordHash = "h" });
            });

            var handler = new UpdateTimezoneHandler(db, CreateHttpAccessor(userId).Object);
            await handler.Handle(new UpdateTimezoneCommand("America/New_York", -300), CancellationToken.None);

            var user = db.Users.Find(userId);
            user.TimeZoneId.Should().Be("America/New_York");
            user.TimeZoneOffset.Should().Be(-300);
        }

        [Fact]
        public async Task Handle_NoUser_ThrowsInvalidOperation()
        {
            using var db = TestDbContextFactory.Create();
            var handler = new UpdateTimezoneHandler(db, CreateHttpAccessor(Guid.NewGuid()).Object);
            var act = async () => await handler.Handle(new UpdateTimezoneCommand("x", 0), CancellationToken.None);
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*User not found*");
        }

        [Fact]
        public async Task Handle_NoUserId_ThrowsUnauthorized()
        {
            using var db = TestDbContextFactory.Create();
            var handler = new UpdateTimezoneHandler(db, CreateHttpAccessor().Object);
            var act = async () => await handler.Handle(new UpdateTimezoneCommand("x", 0), CancellationToken.None);
            await act.Should().ThrowAsync<UnauthorizedAccessException>();
        }
    }
}
