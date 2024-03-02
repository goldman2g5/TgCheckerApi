using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using TgCheckerApi.Interfaces;
using TgCheckerApi.MiddleWare;

namespace TgCheckerApi.Websockets
{
    [BypassApiKey]
    [Authorize]
    public class NotificationHub : Hub<INotificationHub>
    {
        private readonly ILogger<NotificationHub> _logger;
        public static ConcurrentDictionary<string, string> UserMap = new ConcurrentDictionary<string, string>();

        public NotificationHub(ILogger<NotificationHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            Console.WriteLine($"User connected to NotificationHub.");

            var uniqueKey = Context.User.Claims.FirstOrDefault(c => c.Type == "key")?.Value;
            Console.WriteLine($"Unique Key: {uniqueKey}");

            if (!string.IsNullOrEmpty(uniqueKey))
            {
                UserMap[uniqueKey] = Context.ConnectionId;
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            if (exception != null)
            {
                Console.WriteLine($"{exception}, \"User disconnected due to an error.\"\"User disconnected due to an error.\"");
            }
            else
            {
                _logger.LogInformation("User disconnected normally.");
            }

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
                var clientProxy = Clients.Client(connectionId) as IClientProxy;
                if (clientProxy != null)
                {
                    await clientProxy.SendAsync("ReceiveNotification", message);
                }
            }
        }
    } 
}
