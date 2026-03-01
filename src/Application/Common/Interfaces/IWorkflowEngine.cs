namespace Application.Common.Interfaces
{
    public interface IWorkflowEngine
    {
        /// <summary>
        /// Validates whether a transition from the current state to the target state is allowed.
        /// Returns (isValid, errorMessage).
        /// </summary>
        Task<(bool IsValid, string? Error)> ValidateTransitionAsync(
            Guid workItemId,
            Guid targetStateId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes a state transition on a work item. Validates first, then updates and logs.
        /// </summary>
        Task<Guid> TransitionAsync(
            Guid workItemId,
            Guid targetStateId,
            Guid userId,
            string? comment = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the list of states the work item can currently transition to.
        /// </summary>
        Task<List<AvailableTransitionDto>> GetAvailableTransitionsAsync(
            Guid workItemId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Assigns an initial workflow state to a work item that has no current state.
        /// Validates the target state belongs to the project's workflow.
        /// </summary>
        Task AssignInitialStateAsync(
            Guid workItemId,
            Guid targetStateId,
            Guid userId,
            CancellationToken cancellationToken = default);
    }

    public record AvailableTransitionDto(
        Guid StateId,
        string StateName,
        string Color,
        bool IsFinal,
        List<string> RequiredFields);
}
