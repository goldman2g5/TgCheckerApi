using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Text;
using System.Security.Cryptography;
using System.Text.Json;
using TgCheckerApi.Controllers;
using TgCheckerApi.Websockets;
using System.Net.Http;
using TgCheckerApi.Interfaces;

namespace TgCheckerApi.Services
{
    public class WebSocketService : IStatisticsService
    {
        private readonly IHubContext<BotHub> _hubContext;
        private readonly TaskManager _taskManager;
        private readonly ILogger<BotController> _logger;
        private HttpClient _httpClient;

        public WebSocketService(IHubContext<BotHub> hubContext, ILogger<BotController> logger, TaskManager taskManager, IHttpClientFactory httpClientFactory)
        {
            _hubContext = hubContext;
            _taskManager = taskManager;
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();
        }

        public async Task<IActionResult> CallFunctionAsync(string functionName, object parameters, TimeSpan timeout)
        {
            var requestHash = GenerateRequestHash(functionName, parameters);
            _logger.LogInformation($"Received request for {functionName}. Request hash: {requestHash}");

            if (_taskManager._requestCache.TryGetValue(requestHash, out var cachedTask))
            {
                _logger.LogInformation($"Request found in cache. Hash: {requestHash}");
                return await AwaitCachedTask(cachedTask, timeout);
            }

            var invocationId = Guid.NewGuid().ToString();
            var tcs = new TaskCompletionSource<string>();
            _taskManager._pendingTasks[invocationId] = tcs;
            _taskManager._requestCache[requestHash] = tcs;

            var message = new
            {
                invocationId,
                functionName,
                parameters
            };
            string jsonString = JsonSerializer.Serialize(message);

            await _hubContext.Clients.All.SendAsync("ReceiveMessage", jsonString);

            var result = await AwaitAndRemoveFromCache(tcs, requestHash, timeout);
            return result;
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

        private string GenerateRequestHash(string functionName, object parameters)
        {
            var serializedParams = Newtonsoft.Json.JsonConvert.SerializeObject(parameters);

            var inputString = $"{functionName}:{serializedParams}";

            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(inputString);
                var hash = sha256.ComputeHash(bytes);

                var stringBuilder = new StringBuilder();
                foreach (var b in hash)
                {
                    stringBuilder.Append(b.ToString("x2"));
                }
                _logger.LogDebug($"Generated hash for request. Function: {functionName}, Hash: {stringBuilder.ToString()}");
                return stringBuilder.ToString();
            }
        }

        private async Task<IActionResult> AwaitCachedTask(TaskCompletionSource<string> cachedTask, TimeSpan timeout)
        {
            var resultTask = await Task.WhenAny(cachedTask.Task, Task.Delay(timeout));

            if (resultTask == cachedTask.Task)
            {
                try
                {
                    var resultJson = await cachedTask.Task;
                    using var jsonDoc = JsonDocument.Parse(resultJson);
                    if (jsonDoc.RootElement.TryGetProperty("data", out var dataElement))
                    {
                        var dataObject = ConvertJsonElement(dataElement);
                        _logger.LogInformation("Cached task completed successfully.");
                        return new OkObjectResult(dataObject);
                    }
                    return new BadRequestObjectResult("Invalid result format");
                }
                catch (JsonException)
                {
                    return new BadRequestObjectResult("Error parsing the result");
                }
            }
            else
            {
                _logger.LogWarning("Cached task timed out.");
                return new BadRequestObjectResult("Timeout waiting for the result");
            }
        }

        private async Task<IActionResult> AwaitAndRemoveFromCache(TaskCompletionSource<string> tcs, string requestHash, TimeSpan timeout)
        {
            var resultTask = await Task.WhenAny(tcs.Task, Task.Delay(timeout));
            _taskManager._requestCache.Remove(requestHash); // Remove from cache

            if (resultTask == tcs.Task)
            {
                try
                {
                    var resultJson = await tcs.Task;
                    using var jsonDoc = JsonDocument.Parse(resultJson);
                    if (jsonDoc.RootElement.TryGetProperty("data", out var dataElement))
                    {
                        var dataObject = ConvertJsonElement(dataElement);
                        _logger.LogInformation($"New task completed successfully. Hash: {requestHash}");
                        return new OkObjectResult(dataObject);
                    }
                    return new BadRequestObjectResult("Invalid result format");
                }
                catch (JsonException)
                {
                    return new BadRequestObjectResult("Error parsing the result");
                }
            }
            else
            {
                _logger.LogWarning($"New task timed out. Hash: {requestHash}");
                return new BadRequestObjectResult("Timeout waiting for the result");
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
                    return element.GetDouble();
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
