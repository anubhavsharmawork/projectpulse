using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Application.UnitTests.TestHelpers;
using Application.Workflows.Commands;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Application.UnitTests.Workflows
{
    public class UpdateProjectWorkflowHandlerTests
    {
        private static Mock<IHttpContextAccessor> CreateHttpAccessor(Guid? userId = null, bool isAdmin = false)
        {
            var mock = new Mock<IHttpContextAccessor>();
            var httpContext = new DefaultHttpContext();
            if (userId.HasValue)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString())
                };
                if (isAdmin)
                    claims.Add(new Claim(ClaimTypes.Role, "Admin"));
                httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
            }
            else
            {
                httpContext.User = new ClaimsPrincipal(new ClaimsIdentity());
            }
            mock.Setup(x => x.HttpContext).Returns(httpContext);
            return mock;
        }

        [Fact]
        public async Task Handle_ProjectNotFound_Throws()
        {
            using var db = TestDbContextFactory.Create();
            var handler = new UpdateProjectWorkflowHandler(db, CreateHttpAccessor().Object);

            await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(new UpdateProjectWorkflowCommand(Guid.NewGuid(), "N", new List<UpdateWorkflowStateDto>{ new("s",1,"#000",true,false,null,null,false)}), CancellationToken.None));
        }

        [Fact]
        public async Task Handle_UnauthorizedUser_Throws()
        {
            var ownerId = Guid.NewGuid();
            var otherUser = Guid.NewGuid();
            using var db = TestDbContextFactory.CreateWithData(ctx =>
            {
                ctx.Projects.Add(new Project { Id = Guid.NewGuid(), Name = "P", OwnerId = ownerId });
            });

            var proj = await db.Projects.FirstAsync();
            var handler = new UpdateProjectWorkflowHandler(db, CreateHttpAccessor(otherUser).Object);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.Handle(new UpdateProjectWorkflowCommand(proj.Id, "N", new List<UpdateWorkflowStateDto>{ new("s",1,"#000",true,false,null,null,false)}), CancellationToken.None));
        }

        [Fact]
        public async Task Handle_NoStates_Throws()
        {
            var owner = Guid.NewGuid();
            using var db = TestDbContextFactory.CreateWithData(ctx =>
            {
                ctx.Projects.Add(new Project { Id = Guid.NewGuid(), Name = "P", OwnerId = owner });
            });
            var proj = await db.Projects.FirstAsync();
            var handler = new UpdateProjectWorkflowHandler(db, CreateHttpAccessor(owner).Object);

            await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(new UpdateProjectWorkflowCommand(proj.Id, "N", new List<UpdateWorkflowStateDto>()), CancellationToken.None));
        }

        [Fact]
        public async Task Handle_CreatesNewWorkflow_WhenNoExistingWorkflow()
        {
            var owner = Guid.NewGuid();
            using var db = TestDbContextFactory.CreateWithData(ctx =>
            {
                ctx.Projects.Add(new Project { Id = Guid.NewGuid(), Name = "P", OwnerId = owner, DomainType = DomainType.IT });
            });
            var proj = await db.Projects.FirstAsync();

            var handler = new UpdateProjectWorkflowHandler(db, CreateHttpAccessor(owner).Object);
            var dtoStates = new List<UpdateWorkflowStateDto>
            {
                new("Open",1,"#111",true,false,new List<string>{"InProgress"}, new List<string>{"desc"}, true),
                new("InProgress",2,"#222",false,false,null,null,false)
            };

            var id = await handler.Handle(new UpdateProjectWorkflowCommand(proj.Id, "WF", dtoStates), CancellationToken.None);

            var wf = await db.Workflows.Include(w => w.States).FirstOrDefaultAsync(w => w.Id == id);
            wf.Should().NotBeNull();
            wf.Name.Should().Be("WF");
            wf.CreatedBy.Should().Be(owner.ToString());
            wf.States.Should().HaveCount(2);

            // Check allowed transitions serialized
            var open = wf.States.First(s => s.Name == "Open");
            open.RequiredFields.Should().Contain("desc");
            open.AllowedTransitions.Should().NotBeNull();
        }

        [Fact]
        public async Task Handle_SharedWorkflow_CreatesCopy()
        {
            var owner = Guid.NewGuid();
            var wfId = Guid.NewGuid();
            var otherProjectId = Guid.NewGuid();
            var currentProjectId = Guid.NewGuid();
            using var db = TestDbContextFactory.CreateWithData(ctx =>
            {
                var wf = new Workflow { Id = wfId, Name = "Shared", DomainType = DomainType.IT };
                ctx.Workflows.Add(wf);
                ctx.Projects.Add(new Project { Id = otherProjectId, Name = "Other", OwnerId = Guid.NewGuid(), WorkflowId = wfId });
                ctx.Projects.Add(new Project { Id = currentProjectId, Name = "P", OwnerId = owner, WorkflowId = wfId, DomainType = DomainType.IT });
            });

            var handler = new UpdateProjectWorkflowHandler(db, CreateHttpAccessor(owner).Object);
            var dtoStates = new List<UpdateWorkflowStateDto> { new("S",1,"#000",true,false,null,null,false) };
            var newWfId = await handler.Handle(new UpdateProjectWorkflowCommand(currentProjectId, "New", dtoStates), CancellationToken.None);

            newWfId.Should().NotBe(wfId);
            var proj = await db.Projects.FindAsync(currentProjectId);
            proj.WorkflowId.Should().Be(newWfId);

            var original = await db.Workflows.FindAsync(wfId);
            original.Should().NotBeNull();
        }

        [Fact]
        public async Task Handle_NotSharedWorkflow_UpdatesInPlace_And_DeactivatesReferencedStates()
        {
            var owner = Guid.NewGuid();
            var wfId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var stateA = Guid.NewGuid();
            var stateB = Guid.NewGuid();
            using var db = TestDbContextFactory.CreateWithData(ctx =>
            {
                var wf = new Workflow { Id = wfId, Name = "WF", DomainType = DomainType.IT };
                var sA = new WorkflowState { Id = stateA, WorkflowId = wfId, Name = "A", Order = 1, IsActive = true };
                var sB = new WorkflowState { Id = stateB, WorkflowId = wfId, Name = "B", Order = 2, IsActive = true };
                wf.States.Add(sA);
                wf.States.Add(sB);
                ctx.Workflows.Add(wf);
                // add a transition referencing sA -> sB
                ctx.WorkflowTransitions.Add(new WorkflowTransition { Id = Guid.NewGuid(), WorkItemId = Guid.NewGuid(), FromStateId = stateA, ToStateId = stateB, TransitionedByUserId = Guid.NewGuid() });
                ctx.Projects.Add(new Project { Id = projectId, Name = "P", OwnerId = owner, WorkflowId = wfId, DomainType = DomainType.IT });
            });

            var handler = new UpdateProjectWorkflowHandler(db, CreateHttpAccessor(owner).Object);
            var dtoStates = new List<UpdateWorkflowStateDto> { new("NewA",1,"#000",true,false,null,null,false), new("NewB",2,"#111",false,true,null,null,false) };
            var updatedWfId = await handler.Handle(new UpdateProjectWorkflowCommand(projectId, "WF2", dtoStates), CancellationToken.None);

            updatedWfId.Should().Be(wfId);
            var wf = await db.Workflows.Include(w => w.States.Where(s => s.IsActive)).FirstAsync(w => w.Id == wfId);
            wf.Name.Should().Be("WF2");
            // original referenced states should be deactivated
            var oldStates = await db.WorkflowStates.IgnoreQueryFilters().Where(s => s.WorkflowId == wfId).ToListAsync();
            oldStates.Should().NotBeEmpty();
            oldStates.Where(s => s.Name == "A" || s.Name == "B").All(s => s.IsActive == false).Should().BeTrue();

            // new states present
            // Navigation may include both active and inactive (historical) states
            // depending on how the context tracks entities. Assert on active
            // states only to match intended behavior: new workflow states are active.
            wf.States.Where(s => s.IsActive).Should().HaveCount(2);
            wf.States.Where(s => s.IsActive).Select(s => s.Name).Should().Contain(new[] { "NewA", "NewB" });
        }
    }
}
