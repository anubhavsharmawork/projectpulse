namespace Application.Common.Interfaces
{
    /// <summary>
    /// Interface for sending real-time notifications to users
    /// </summary>
    public interface IRealTimeNotificationService
    {
        /// <summary>
        /// Send a mention notification to a specific user
        /// </summary>
        Task SendMentionNotificationAsync(Guid userId, Guid workItemId, string workItemTitle, string mentionedByName, CancellationToken cancellationToken = default);
    }
}
