using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Text.Json;
using TgCheckerApi.Controllers;
using TgCheckerApi.Websockets;

namespace TgCheckerApi.Services
{
    public class WebSocketService
    {
        private readonly IHubContext<BotHub> _hubContext;
        private readonly TaskManager _taskManager;
        private Dictionary<string, TaskCompletionSource<string>> _pendingTasks = new Dictionary<string, TaskCompletionSource<string>>();
        private readonly ILogger<BotController> _logger;

        public WebSocketService(IHubContext<BotHub> hubContext, ILogger<BotController> logger, TaskManager taskManager)
        {
            _hubContext = hubContext;
            _taskManager = taskManager;
            _logger = logger;
        }

        public async Task<IActionResult> CallFunctionAsync(string functionName, object parameters, TimeSpan timeout)
        {
            var invocationId = Guid.NewGuid().ToString();
            var tcs = new TaskCompletionSource<string>();
            _taskManager._pendingTasks[invocationId] = tcs;

            var message = new
            {
                invocationId,
                functionName,
                parameters
            };
            string jsonString = JsonSerializer.Serialize(message);

            await _hubContext.Clients.All.SendAsync("ReceiveMessage", jsonString);

            var resultTask = await Task.WhenAny(tcs.Task, Task.Delay(timeout));
            if (resultTask == tcs.Task)
            {
                var resultJson = await tcs.Task;
                try
                {
                    using var jsonDoc = JsonDocument.Parse(resultJson);
                    if (jsonDoc.RootElement.TryGetProperty("data", out var dataElement))
                    {
                        var dataObject = ConvertJsonElement(dataElement);
                        return new OkObjectResult(dataObject);
                    }
                    return new BadRequestObjectResult("Invalid result format");
                }
                catch (JsonException)
                {
                    return new BadRequestObjectResult("Error parsing the result");
                }
            }

            return new BadRequestObjectResult("Timeout waiting for the result");
        }

        public T ResponseToObject<T>(IActionResult response)
        {
            if (response is OkObjectResult okResult && okResult.Value is string jsonString)
            {
                try
                {
                    return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(jsonString);
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogError(jsonEx, $"Error deserializing WebSocket response to type {typeof(T)}.");
                    throw;
                }
            }
            else
            {
                _logger.LogWarning("Response is not in expected 'OkObjectResult' format or does not contain a string value.");
                throw new InvalidOperationException("Invalid response format.");
            }
        }

        private object ConvertJsonElement(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    return element.EnumerateObject().ToDictionary(property => property.Name, property => ConvertJsonElement(property.Value));
                case JsonValueKind.Array:
                    return element.EnumerateArray().Select(ConvertJsonElement).ToList();
                case JsonValueKind.String:
                    return element.GetString();
                case JsonValueKind.Number:
                    return element.GetDouble(); // or use appropriate numeric type
                case JsonValueKind.True:
                case JsonValueKind.False:
                    return element.GetBoolean();
                case JsonValueKind.Null:
                    return null;
                default:
                    throw new InvalidOperationException("Unsupported JsonValueKind.");
            }
        }
    }
}
