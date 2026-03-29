using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using global::Domain.Entities;
using Infrastructure.Workflows;
using FluentAssertions;
using Xunit;
using Application.UnitTests.TestHelpers;

namespace Application.UnitTests.Workflows;

public class WorkflowEngineTests
{
    [Fact]
    public async Task ValidateTransitionAsync_ReturnsFalse_WhenWorkItemNotFound()
    {
        using var db = TestDbContextFactory.Create();
        var engine = new WorkflowEngine(db);

        var (ok, err) = await engine.ValidateTransitionAsync(Guid.NewGuid(), Guid.NewGuid());
        ok.Should().BeFalse();
        err.Should().Be("Work item not found");
    }

    [Fact]
    public async Task ValidateTransitionAsync_Fails_WhenRequiredFieldsMissing()
    {
        var stateId = Guid.NewGuid();
        var workItemId = Guid.NewGuid();

        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            var wfId = Guid.NewGuid();
            ctx.WorkflowStates.Add(new WorkflowState { Id = stateId, WorkflowId = wfId, Name = "Target", RequiredFields = JsonSerializer.Serialize(new List<string>{"desc"}) });

            var currentState = new WorkflowState { Id = Guid.NewGuid(), WorkflowId = wfId, Name = "Open", AllowedTransitions = JsonSerializer.Serialize(new List<Guid>{ stateId }) };
            ctx.WorkflowStates.Add(currentState);

            ctx.WorkItems.Add(new global::Domain.Entities.TaskWorkItem { Id = workItemId, ProjectId = Guid.NewGuid(), CurrentStateId = currentState.Id, CurrentState = currentState, CustomFieldValues = new List<CustomFieldValue>() });
        });

        var engine = new WorkflowEngine(db);
        var (ok, err) = await engine.ValidateTransitionAsync(workItemId, stateId);
        ok.Should().BeFalse();
        err.Should().Contain("Required fields missing");
    }

    [Fact]
    public async Task TransitionAsync_PerformsTransitionAndCreatesLog()
    {
        var wfId = Guid.NewGuid();
        var fromId = Guid.NewGuid();
        var toId = Guid.NewGuid();
        var workItemId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            var wf = new Workflow { Id = wfId };
            ctx.Workflows.Add(wf);
            var from = new WorkflowState { Id = fromId, WorkflowId = wfId, Name = "From", Order = 1 };
            var to = new WorkflowState { Id = toId, WorkflowId = wfId, Name = "To", Order = 2, IsFinal = true };
            ctx.WorkflowStates.AddRange(from, to);

            ctx.WorkItems.Add(new global::Domain.Entities.TaskWorkItem { Id = workItemId, ProjectId = Guid.NewGuid(), CurrentStateId = fromId, CurrentState = from, IsCompleted = false });
        });

        var engine = new WorkflowEngine(db);
        var transitionId = await engine.TransitionAsync(workItemId, toId, userId, "ok");

        transitionId.Should().NotBe(Guid.Empty);
        var w = db.WorkItems.First(wi => wi.Id == workItemId);
        w.CurrentStateId.Should().Be(toId);
        w.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task GetAvailableTransitionsAsync_ReturnsEmpty_WhenNoCurrentState()
    {
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.WorkItems.Add(new global::Domain.Entities.TaskWorkItem { Id = Guid.NewGuid() });
        });

        var engine = new WorkflowEngine(db);
        var list = await engine.GetAvailableTransitionsAsync(Guid.NewGuid());
        list.Should().BeEmpty();
    }

    [Fact]
    public async Task AssignInitialStateAsync_SetsStateAndCompletesIfFinal()
    {
        var wfId = Guid.NewGuid();
        var stateId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var workItemId = Guid.NewGuid();

        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            var wf = new Workflow { Id = wfId, DomainType = global::Domain.Enums.DomainType.IT };
            ctx.Workflows.Add(wf);
            var state = new WorkflowState { Id = stateId, WorkflowId = wfId, Name = "Init", IsFinal = true };
            ctx.WorkflowStates.Add(state);
            ctx.Projects.Add(new Project { Id = projectId, WorkflowId = wfId, DomainType = global::Domain.Enums.DomainType.IT });
            ctx.WorkItems.Add(new global::Domain.Entities.TaskWorkItem { Id = workItemId, ProjectId = projectId });
        });

        var engine = new WorkflowEngine(db);
        await engine.AssignInitialStateAsync(workItemId, stateId, Guid.NewGuid());

        var wi = db.WorkItems.First(w => w.Id == workItemId);
        wi.CurrentStateId.Should().Be(stateId);
        wi.IsCompleted.Should().BeTrue();
    }
}
