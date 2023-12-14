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

        public async Task ReceiveStream(IAsyncEnumerable<string> stream, string param)
        {
            await foreach (var item in stream)
            {
                // Process each item from the stream
                Console.WriteLine($"Received stream item: {item}");
                // You can also forward this to other clients or handle it as needed
            }
        }
    }
}
