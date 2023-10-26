using Microsoft.AspNetCore.SignalR;

namespace TgCheckerApi.Websockets
{
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
