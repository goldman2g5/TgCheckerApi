using Microsoft.AspNetCore.SignalR;

namespace TgCheckerApi.Websockets
{
    public class BotHub : Hub
    {
        public async Task SendMessage(string connection_id)
        {
            await Clients.Client(connection_id).SendAsync("ReceiveMessage", "RABOTAET");

        }

        public override async Task OnConnectedAsync()
        {
            Console.WriteLine($"User connected to BotHub.");

            await base.OnConnectedAsync();
        }

        public string GetConnectionId()
        {
            return Context.ConnectionId;
        }
    }
}
