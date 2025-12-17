using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Diagnostics.CodeAnalysis;

namespace API.Hubs
{
    [ExcludeFromCodeCoverage]
    [Authorize]
    public class ProjectHub : Hub
    {
        public async Task JoinProject(string projectId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"project-{projectId}");
        }

        public async Task TaskUpdate(string projectId, object payload)
        {
            await Clients.Group($"project-{projectId}").SendAsync("TaskUpdated", payload);
        }

        public async Task Notify(string userId, object payload)
        {
            await Clients.User(userId).SendAsync("Notification", payload);
        }
    }
}
