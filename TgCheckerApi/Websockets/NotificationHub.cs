using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace TgCheckerApi.Websockets
{
    [Authorize]
    public class NotificationHub : Hub
    {
        public static ConcurrentDictionary<string, string> UserMap = new ConcurrentDictionary<string, string>();

        public override async Task OnConnectedAsync()
        {
            var uniqueKey = Context.User.Claims.FirstOrDefault(c => c.Type == "key")?.Value;

            if (!string.IsNullOrEmpty(uniqueKey))
            {
                UserMap[uniqueKey] = Context.ConnectionId;
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var uniqueKey = Context.User.Claims.FirstOrDefault(c => c.Type == "key")?.Value;

            if (!string.IsNullOrEmpty(uniqueKey))
            {
                UserMap.TryRemove(uniqueKey, out _);
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendToUserWithUniqueKey(string uniqueKey, string message)
        {
            if (UserMap.TryGetValue(uniqueKey, out string connectionId))
            {
                await Clients.Client(connectionId).SendAsync("ReceiveNotification", message);
            }
        }
    }
}
