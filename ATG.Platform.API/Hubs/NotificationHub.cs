using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ATG.Platform.API.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    public static string UserGroup(Guid userId) => $"user:{userId}";

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId is not null && Guid.TryParse(userId, out var id))
            await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(id));

        await base.OnConnectedAsync();
    }
}
