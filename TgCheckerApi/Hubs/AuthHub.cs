using Microsoft.AspNetCore.SignalR;
using TgCheckerApi.MiddleWare;

namespace TgCheckerApi.Websockets
{
    [BypassApiKey]
    public class AuthHub : Hub
    {
        public async Task SendMessage(string connection_id)
        {
            await Clients.Client(connection_id).SendAsync("ReceiveMessage", "RABOTAET");

        }

        public string GetConnectionId()
        {
            return Context.ConnectionId;
        }
    }
}
