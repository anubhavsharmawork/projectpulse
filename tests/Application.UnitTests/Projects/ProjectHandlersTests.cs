using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Projects.Commands;
using Application.Projects.Queries;
using Application.UnitTests.TestHelpers;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Application.UnitTests.Projects
{
    public class ProjectHandlersTests
    {
        private static Mock<IHttpContextAccessor> CreateHttpAccessor(Guid? userId = null)
        {
            var mock = new Mock<IHttpContextAccessor>();
            var httpContext = new DefaultHttpContext();
            if (userId.HasValue)
            {
                httpContext.User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(
                    new[] { new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, userId.Value.ToString()) }, "Test"));
            }
            mock.Setup(x => x.HttpContext).Returns(httpContext);
            return mock;
        }

        [Fact]
        public async Task GetProjectConfig_Throws_WhenNotFound()
        {
            using var db = TestDbContextFactory.Create();
            var handler = new GetProjectConfigHandler(db);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => handler.Handle(new GetProjectConfigQuery(Guid.NewGuid()), CancellationToken.None));
        }

        [Fact]
        public async Task GetProjectConfig_UsesTemplateLabels_WhenValidJson()
        {
            var projectId = Guid.NewGuid();
            var labels = new System.Collections.Generic.Dictionary<string, string> { ["1"] = "One" };
            using var db = TestDbContextFactory.CreateWithData(ctx =>
            {
                var tmpl = new DomainTemplate { Id = Guid.NewGuid(), DomainType = DomainType.IT, WorkItemTypeLabels = System.Text.Json.JsonSerializer.Serialize(labels) };
                ctx.DomainTemplates.Add(tmpl);
                ctx.Projects.Add(new Project { Id = projectId, Name = "P", DomainType = DomainType.IT, TemplateId = tmpl.Id });
            });

            var handler = new GetProjectConfigHandler(db);
            var dto = await handler.Handle(new GetProjectConfigQuery(projectId), CancellationToken.None);

            dto.WorkItemTypeLabels.Should().ContainKey("1");
            dto.DomainType.Should().Be(DomainType.IT.ToString());
        }

        [Fact]
        public async Task GetProjectConfig_FallsBack_WhenTemplateInvalid()
        {
            var projectId = Guid.NewGuid();
            using var db = TestDbContextFactory.CreateWithData(ctx =>
            {
                var tmpl = new DomainTemplate { Id = Guid.NewGuid(), DomainType = DomainType.IT, WorkItemTypeLabels = "not-json" };
                ctx.DomainTemplates.Add(tmpl);
                ctx.Projects.Add(new Project { Id = projectId, Name = "P", DomainType = DomainType.IT, TemplateId = tmpl.Id });
            });

            var handler = new GetProjectConfigHandler(db);
            var dto = await handler.Handle(new GetProjectConfigQuery(projectId), CancellationToken.None);

            dto.WorkItemTypeLabels.Should().NotBeNull();
            dto.WorkItemTypeLabels.Should().ContainKey("1");
        }

        [Fact]
        public async Task CreateProject_WithDomainLinks_CreatesWorkflowAndTemplateLinks_AndRoles()
        {
            var userId = Guid.NewGuid();
            using var db = TestDbContextFactory.CreateWithData(ctx =>
            {
                var wf = new Workflow { Id = Guid.NewGuid(), DomainType = DomainType.IT, Name = "W" };
                ctx.Workflows.Add(wf);
                var tmpl = new DomainTemplate { Id = Guid.NewGuid(), DomainType = DomainType.IT };
                ctx.DomainTemplates.Add(tmpl);
            });

            var handler = new CreateProjectHandler(db, CreateHttpAccessor(userId).Object);
            var result = await handler.Handle(new CreateProjectCommand("New", null, IsPublic: false, DomainType: DomainType.IT), CancellationToken.None);

            var project = await db.Projects.FindAsync(result.ProjectId);
            project.Should().NotBeNull();
            project.DomainType.Should().Be(DomainType.IT);
            project.OwnerId.Should().Be(userId);
            (await db.ProjectRoles.CountAsync(r => r.ProjectId == project.Id)).Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task CreateProject_WithoutDomain_DoesNotCreateRoles()
        {
            using var db = TestDbContextFactory.Create();
            var handler = new CreateProjectHandler(db, CreateHttpAccessor().Object);

            var result = await handler.Handle(new CreateProjectCommand("New", null), CancellationToken.None);
            var project = await db.Projects.FindAsync(result.ProjectId);
            project.Should().NotBeNull();
            (await db.ProjectRoles.CountAsync(r => r.ProjectId == project.Id)).Should().Be(0);
        }
    }
}
