﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Text.Json;
using TgCheckerApi.Websockets;

namespace TgCheckerApi.Services
{
    public class WebSocketService
    {
        private readonly IHubContext<BotHub> _hubContext;
        private readonly TaskManager _taskManager;
        private Dictionary<string, TaskCompletionSource<string>> _pendingTasks = new Dictionary<string, TaskCompletionSource<string>>();


        public WebSocketService(IHubContext<BotHub> hubContext, TaskManager taskManager)
        {
            _hubContext = hubContext;
            _taskManager = taskManager;
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
                        // Convert JsonElement to a type that can be serialized by Newtonsoft.Json
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
