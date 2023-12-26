using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using TgCheckerApi.Websockets;
using TgCheckerApi.Services;
using TgCheckerApi.Models.BaseModels;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using TgCheckerApi.MiddleWare;
using TgCheckerApi.Models;
using TgCheckerApi.Models.BaseModels;
using TgCheckerApi.Models.DTO;
using TgCheckerApi.Models.GetModels;
using TgCheckerApi.Models.PostModels;
using TgCheckerApi.Models.PutModels;
using TgCheckerApi.Services;
using TgCheckerApi.Utility;
using TgCheckerApi.Websockets;
using System.Diagnostics.Tracing;
using Microsoft.Extensions.DependencyInjection;

namespace TgCheckerApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BotController : ControllerBase
    {
        private readonly IHubContext<BotHub> _hubContext;
        private readonly TaskManager _taskManager;
        private readonly WebSocketService _webSocketService;
        private readonly UserService _userService;
        private readonly TgDbContext _context;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<BotController> _logger;


        public BotController(TgDbContext context, IServiceProvider serviceProvider, ILogger<BotController> logger, IHubContext<BotHub> hubContext, TaskManager taskManager, WebSocketService webSocketService)
        {
            _context = context;
            _hubContext = hubContext;
            _taskManager = taskManager;
            _webSocketService = webSocketService;
            _serviceProvider = serviceProvider;
            _logger = logger;
            _userService = new UserService(context);
        }

        public class SumRequest
        {
            public double number1 { get; set; }
            public double number2 { get; set; }
        }

        [HttpPost("sumOfTwo")]
        public async Task<IActionResult> CallSumOfTwo([FromBody] SumRequest sumRequest)
        {
            var parameters = new { sumRequest.number1, sumRequest.number2 };
            return await _webSocketService.CallFunctionAsync("sumOfTwo", parameters, TimeSpan.FromSeconds(30));
        }

        public class DailyViewsRequest
        {
            public int ChannelId { get; set; }
            public int NumberOfDays { get; set; }
        }

        public class DailySukaRequest
        {
            public int ChannelId { get; set; }
        }

        [HttpPost("getSubscribersByChannel")]
        public async Task<IActionResult> CallSubscribersByChannel([FromBody] DailySukaRequest dailyViewsRequest)
        {
            var channel = await FindChannelById(dailyViewsRequest.ChannelId);
            if (channel == null || string.IsNullOrEmpty(channel.Url))
            {
                return BadRequest("Channel not found or URL is missing.");
            }

            var channelNameFormatted = "@" + channel.Url.Replace("https://t.me/", "");

            var parameters = new
            {
                channel_name = channelNameFormatted,
            };

            var response = await _webSocketService.CallFunctionAsync("getSubscribersCount", parameters, TimeSpan.FromSeconds(600));

            return Ok(response);
        }

        [BypassApiKey]
        [HttpPost("getDailyViewsByChannel")]
        public async Task<IActionResult> CallGetDailyViewsByChannel([FromBody] DailyViewsRequest dailyViewsRequest)
        {
            _logger.LogInformation("Starting CallGetDailyViewsByChannel method for ChannelId: {ChannelId}", dailyViewsRequest.ChannelId);
            var channel = await FindChannelById(dailyViewsRequest.ChannelId);
            if (channel == null || string.IsNullOrEmpty(channel.Url))
            {
                _logger.LogWarning("Channel not found or URL is missing for ChannelId: {ChannelId}", dailyViewsRequest.ChannelId);
                return BadRequest("Channel not found or URL is missing.");
            }

            var viewsData = await FetchDataFromDatabase(dailyViewsRequest, considerOutdated: true);
            bool isDataComplete = viewsData?.Count >= dailyViewsRequest.NumberOfDays;
            bool isUpdateRequired = await IsUpdateRequiredForChannel(dailyViewsRequest);

            if (isDataComplete && !isUpdateRequired)
            {
                _logger.LogInformation("Data is complete and up-to-date for ChannelId: {ChannelId}.", dailyViewsRequest.ChannelId);
            }
            else
            {
                if (isDataComplete)
                {
                    _logger.LogInformation("Data is complete but outdated for ChannelId: {ChannelId}. Initiating background update.", dailyViewsRequest.ChannelId);
                    _ = UpdateDataInBackground(dailyViewsRequest, channel, _serviceProvider);
                }
                else
                {
                    _logger.LogInformation("Data is incomplete for ChannelId: {ChannelId}. Awaiting new data from WebSocket.", dailyViewsRequest.ChannelId);
                    viewsData = await WaitForWebSocketAndUpdate(dailyViewsRequest, channel);
                }
            }

            _logger.LogInformation("Returning data for ChannelId: {ChannelId} with {DataCount} view counts", dailyViewsRequest.ChannelId, viewsData?.Count ?? 0);
            viewsData?.Reverse();
            return Ok(viewsData);
        }

        private async Task UpdateDataInBackground(DailyViewsRequest dailyViewsRequest, Channel channel, IServiceProvider services)
        {
            using (var scope = services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<TgDbContext>();
                var webSocketService = scope.ServiceProvider.GetRequiredService<WebSocketService>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<BotController>>();

                try
                {
                    // Construct parameters for WebSocket request
                    var parameters = new
                    {
                        channel_name = "@" + channel.Url.Replace("https://t.me/", ""),
                        number_of_days = dailyViewsRequest.NumberOfDays
                    };

                    // Call the WebSocket service and wait for the response
                    var response = await webSocketService.CallFunctionAsync("getDailyViewsByChannel", parameters, TimeSpan.FromSeconds(30));
                    if (response is OkObjectResult okResult && okResult.Value is string jsonString)
                    {
                        var viewsRecords = JsonConvert.DeserializeObject<List<ViewsRecord>>(jsonString);
                        if (viewsRecords != null && viewsRecords.Any())
                        {
                            // Update the database with the new records using the new DbContext instance
                            await UpdateDatabaseWithViewsRecords(viewsRecords, dailyViewsRequest.ChannelId, dbContext);
                            logger.LogInformation("Successfully updated database with new records for ChannelId: {ChannelId}", dailyViewsRequest.ChannelId);
                        }
                        else
                        {
                            logger.LogWarning("Received empty or invalid data from WebSocket for ChannelId: {ChannelId}", dailyViewsRequest.ChannelId);
                        }
                    }
                    else
                    {
                        logger.LogWarning("Invalid or no response from WebSocket service for ChannelId: {ChannelId}", dailyViewsRequest.ChannelId);
                    }
                }
                catch (JsonException jsonEx)
                {
                    logger.LogError(jsonEx, "Error in deserializing the data received from WebSocket for ChannelId: {ChannelId}", dailyViewsRequest.ChannelId);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An unexpected error occurred while updating data in the background for ChannelId: {ChannelId}", dailyViewsRequest.ChannelId);
                }
            } // The DbContext, and any other scoped services, will be disposed here
        }

        private async Task<List<int>> WaitForWebSocketAndUpdate(DailyViewsRequest request, Channel channel)
        {
            try
            {
                // Construct parameters for WebSocket request
                var parameters = new
                {
                    channel_name = "@" + channel.Url.Replace("https://t.me/", ""),
                    number_of_days = request.NumberOfDays
                };

                // Call the WebSocket service and wait for the response
                var response = await _webSocketService.CallFunctionAsync("getDailyViewsByChannel", parameters, TimeSpan.FromSeconds(30));
                if (response is OkObjectResult okResult && okResult.Value is string jsonString)
                {
                    // Deserialize the response
                    var viewsRecords = JsonConvert.DeserializeObject<List<ViewsRecord>>(jsonString);
                    if (viewsRecords != null && viewsRecords.Any())
                    {
                        // Update the database with the new records
                        await UpdateDatabaseWithViewsRecords(viewsRecords, request.ChannelId, _context);

                        // Assuming UpdateDatabaseWithViewsRecords is well-behaved and doesn't change the order,
                        // Convert the records to a list of view counts to return
                        var viewsList = viewsRecords.Select(vr => vr.Views).ToList();
                        return viewsList;
                    }
                    else
                    {
                        _logger.LogWarning("Received empty or invalid data from WebSocket.");
                    }
                }
                else
                {
                    _logger.LogWarning("Invalid or no response from WebSocket service.");
                }
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "Error in deserializing the data received from WebSocket.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while waiting for WebSocket data and updating.");
            }
            return new List<int>(); // Return an empty list if there's an error or no data
        }

        private async Task<List<int>> FetchDataFromDatabase(DailyViewsRequest dailyViewsRequest, bool considerOutdated = false)
        {
            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddDays(-dailyViewsRequest.NumberOfDays);

            // Fetch the records from the database
            var records = await _context.Channels
                                        .Where(c => c.Id == dailyViewsRequest.ChannelId)
                                        .SelectMany(c => c.StatisticsSheets)
                                        .SelectMany(ss => ss.ViewsRecords)
                                        .Where(vr => vr.Date >= startDate && vr.Date <= endDate)
                                        .OrderBy(vr => vr.Date) // Ensure the records are ordered
                                        .ToListAsync();

            if (!records.Any())
            {
                // No records found for the given date range and channel
                return null;
            }

            // Determine if any of the data is outdated
            bool isDataOutdated = records.Any(vr => DateTime.UtcNow - vr.Updated > TimeSpan.FromMinutes(1)); // Checking each record

            if (isDataOutdated && !considerOutdated)
            {
                // If outdated data is not acceptable and any of the data is outdated, return null
                return null;
            }

            // Convert the records to a list of view counts
            var viewsList = records.Select(vr => vr.Views).ToList();
            return viewsList;
        }

        private async Task UpdateDatabaseWithViewsRecords(List<ViewsRecord> viewsRecords, int channelId, TgDbContext dbContext)
        {
            var channel = await dbContext.Channels
                                        .Include(c => c.StatisticsSheets)
                                        .FirstOrDefaultAsync(c => c.Id == channelId);

            if (channel == null)
            {
                throw new Exception("Channel not found.");
            }

            var statisticsSheet = channel.StatisticsSheets.FirstOrDefault();
            if (statisticsSheet == null)
            {
                statisticsSheet = new StatisticsSheet { ChannelId = channelId };
                dbContext.StatisticsSheets.Add(statisticsSheet);
                await dbContext.SaveChangesAsync();
            }

            foreach (var record in viewsRecords)
            {
                record.Updated = DateTime.SpecifyKind(record.Updated, DateTimeKind.Utc);
                record.Date = DateTime.SpecifyKind(record.Date, DateTimeKind.Utc);

                record.Sheet = statisticsSheet.Id;
                
                var existingRecord = statisticsSheet.ViewsRecords
                                                     .FirstOrDefault(vr => vr.Date == record.Date);
                if (existingRecord != null)
                {
                    existingRecord.Views = record.Views;
                    existingRecord.LastMessageId = record.LastMessageId;
                    existingRecord.Updated = DateTime.UtcNow;
                }
                else
                {
                    record.Updated = DateTime.UtcNow;
                    statisticsSheet.ViewsRecords.Add(record);
                }

            }

            await dbContext.SaveChangesAsync();
        }

        private async Task<bool> IsUpdateRequiredForChannel(DailyViewsRequest dailyViewsRequest)
        {
            _logger.LogInformation("Starting IsUpdateRequiredForChannel method.");
            _logger.LogInformation($"Parameters: ChannelId={dailyViewsRequest.ChannelId}, NumberOfDays={dailyViewsRequest.NumberOfDays}");

            TimeSpan outdatedThreshold = TimeSpan.FromMinutes(1);
            _logger.LogInformation($"Outdated threshold set to {outdatedThreshold.TotalMinutes} minutes.");

            var today = DateTime.UtcNow.Date;
            var startDate = today.AddDays(-dailyViewsRequest.NumberOfDays);
            _logger.LogInformation($"Checking records from {startDate} to {today}.");

            var recordsInRange = await _context.Channels
                                                .Where(c => c.Id == dailyViewsRequest.ChannelId)
                                                .SelectMany(c => c.StatisticsSheets)
                                                .SelectMany(ss => ss.ViewsRecords)
                                                .Where(vr => vr.Date >= startDate && vr.Date <= today)
                                                .ToListAsync();

            var isUpdateRequired = !recordsInRange.Any() || recordsInRange.Any(vr => DateTime.UtcNow - vr.Updated > outdatedThreshold);
            _logger.LogInformation($"{recordsInRange.Count} records found in date range.");
            _logger.LogInformation($"Update required: {isUpdateRequired}");

            return isUpdateRequired;
        }


        private async Task<Channel> FindChannelById(int id)
        {
            return await _context.Channels.FindAsync(id);
        }
    }
    }
