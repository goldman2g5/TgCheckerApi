using Microsoft.AspNetCore.SignalR;
using System.Text.Json;
using TgCheckerApi.Services;

namespace TgCheckerApi.Websockets
{
    public class ResultMessage
    {
        public double Result { get; set; }
    }

    public class BotHub : Hub
    {
        private readonly TaskManager _taskManager;

        public BotHub(TaskManager taskManager)
        {
            _taskManager = taskManager;
        }

        public async Task SendMessage(string json)
        {
            await Clients.All.SendAsync("ReceiveMessage", json);
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
                Console.WriteLine($"Received stream item: {item}");

                try
                {
                    var resultData = JsonSerializer.Deserialize<Dictionary<string, object>>(item);
                    if (resultData != null && resultData.TryGetValue("invocationId", out var invocationIdObj)
                        && _taskManager._pendingTasks.TryGetValue(invocationIdObj.ToString(), out var tcs))
                    {
                        tcs.SetResult(item);  // Set the result for the corresponding TaskCompletionSource
                        _taskManager._pendingTasks.Remove(invocationIdObj.ToString());  // Remove the completed task
                    }
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"JSON parsing error: {ex.Message}");
                    Console.WriteLine($"JSON content: {item}");
                }
            }
        }
    }
}
