using API.Hubs;
using Application.Common.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace API.Services
{
    public class SignalRNotificationService : IRealTimeNotificationService
    {
        private readonly IHubContext<ProjectHub> _hubContext;

        public SignalRNotificationService(IHubContext<ProjectHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task SendMentionNotificationAsync(
            Guid userId, 
            Guid workItemId, 
            string workItemTitle, 
            string mentionedByName, 
            CancellationToken cancellationToken = default)
        {
            await _hubContext.Clients.User(userId.ToString())
                .SendAsync("Notification", new
                {
                    type = "mention",
                    workItemId,
                    workItemTitle,
                    mentionedBy = mentionedByName
                }, cancellationToken);
        }
    }
}
