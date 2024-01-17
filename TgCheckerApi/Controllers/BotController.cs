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
using static TgCheckerApi.Controllers.BotController;

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
        
        public class MonthViewsRequest
        {
            public int ChannelId { get; set; }
            public int Months { get; set; }
        }

        public class BroadcastStatsRequest
        {
            public int ChannelId { get; set; }
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

        public class DailySubRequest
        {
            public List<int>? ChannelId { get; set; }
            public bool AllChannels { get; set; }
        }

        public class SubHistoryRequest
        {
            public int ChannelId { get; set; }
            public int NumberOfDays { get; set; }
            public int? Months { get; set; } // Number of months for which to calculate the average
        }

        [BypassApiKey]
        [HttpPost("getBroadcastStats")]
        public async Task<IActionResult> CallGetBroadcastStats([FromBody] BroadcastStatsRequest statsRequest)
        {
            var channel = await FindChannelById(statsRequest.ChannelId);

            if (channel == null || string.IsNullOrEmpty(channel.Url))
            {
                return BadRequest("Channel not found or URL is missing.");
            }

            _logger.LogInformation("Starting CallGetBroadcastStats method for ChannelUsername: {ChannelUsername}", channel.TelegramId);

            if (channel.TelegramId == null)
            {
                return BadRequest("Channel username is missing.");
            }

            try
            {
                // Prepare parameters for WebSocket call
                var parameters = new { channel_username = channel.TelegramId };

                // Calling the WebSocket function
                var response = await _webSocketService.CallFunctionAsync("getBroadcastStats", parameters, TimeSpan.FromSeconds(600));

                if (response == null)
                {
                    _logger.LogWarning("No response received for ChannelUsername: {ChannelUsername}", channel.TelegramId);
                    return NotFound("No statistics found.");
                }

                _logger.LogInformation("Returning broadcast statistics for ChannelUsername: {ChannelUsername}", channel.TelegramId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching broadcast statistics for ChannelUsername: {ChannelUsername}", channel.TelegramId);
                return StatusCode(500, "Internal server error.");
            }
        }

        [BypassApiKey]
        [HttpPost("getMonthlyViews")]
        public async Task<IActionResult> CallGetMonthViews([FromBody] MonthViewsRequest monthViewsRequest)
        {
            _logger.LogInformation("Starting CallGetMonthViews method for ChannelId: {ChannelId}", monthViewsRequest.ChannelId);

            var channel = await FindChannelById(monthViewsRequest.ChannelId);
            if (channel == null || string.IsNullOrEmpty(channel.Url))
            {
                return BadRequest("Channel not found or URL is missing.");
            }

            var monthViewsData = await FetchMonthDataFromDatabase(monthViewsRequest);
            bool isDataComplete = monthViewsData?.Count >= monthViewsRequest.Months;
            bool isUpdateRequired = await IsUpdateRequiredForMonth(monthViewsRequest);

            if (isDataComplete && !isUpdateRequired)
            {
                _logger.LogInformation("Data is complete and up-to-date for ChannelId: {ChannelId}.", monthViewsRequest.ChannelId);
            }
            else
            {
                if (isDataComplete)
                {
                    _logger.LogInformation("First month data is outdated for ChannelId: {ChannelId}. Initiating background update.", monthViewsRequest.ChannelId);
                    _ = UpdateMonthDataInBackground(monthViewsRequest, channel, _serviceProvider, updateOnlyFirstMonth: true);
                }
                else
                {
                    _logger.LogInformation("Data is incomplete for ChannelId: {ChannelId}. Awaiting new data from WebSocket.", monthViewsRequest.ChannelId);
                    monthViewsData = await WaitForWebSocketAndUpdateForMonth(monthViewsRequest, channel);
                }
            }

            var viewsList = monthViewsData?.Select(record => (double)record.Views).ToList();


            _logger.LogInformation("Returning data for ChannelId: {ChannelId} with {DataCount} month view counts", monthViewsRequest.ChannelId, monthViewsData?.Count ?? 0);
            return Ok(viewsList);
        }

        public class ProfileUpdateResponse
        {
            public string avatar { get; set; }
            public string username { get; set; }
        }

        [BypassApiKey]
        [RequiresJwtValidation]
        [HttpGet("UpdateUserProfile")]
        public async Task<IActionResult> UpdateUserProfile()
        {
            var uniqueKeyClaim = User.FindFirst(c => c.Type == "key")?.Value;
            var user = await _userService.GetUserWithRelations(uniqueKeyClaim);

            if (user == null)
            {
                return NotFound("User not found.");
            }

            var parameters = new { user_id = user.TelegramId };
            try
            {
                var response = await _webSocketService.CallFunctionAsync("getProfilePictureAndUsername", parameters, TimeSpan.FromSeconds(30));
                if (response is OkObjectResult okResult && okResult.Value is string jsonString)
                {
                    var profileData = JsonConvert.DeserializeObject<ProfileUpdateResponse>(jsonString);

                    if (profileData != null)
                    {
                        var avatarBase64 = profileData.avatar;
                        var username = profileData.username;

                        if (!string.IsNullOrEmpty(avatarBase64))
                        {
                            user.Avatar = Convert.FromBase64String(avatarBase64); // Update avatar
                        }

                        if (!string.IsNullOrEmpty(username))
                        {
                            user.Username = username; // Update username
                        }

                        // Save changes in the database
                        await _context.SaveChangesAsync();

                        return Ok(profileData);
                    }
                    else
                    {
                        return BadRequest("Invalid profile data.");
                    }
                }
                else
                {
                    return StatusCode(500, "Invalid or no response from WebSocket service.");
                }
            }
            catch (Exception ex)
            {
                // Log the exception and return an error response
                return StatusCode(500, "Internal server error: " + ex.Message);
            }
        }

        [HttpPost("getSubscribersByChannels")]
        public async Task<IActionResult> CallSubscribersByChannels([FromBody] DailySubRequest dailySubRequest)
        {
            List<long> telegramIds;

            if (dailySubRequest.AllChannels)
            {
                telegramIds = await _context.Channels
                                            .Where(channel => channel.TelegramId != null)
                                            .Select(channel => channel.TelegramId.Value)
                                            .ToListAsync();

                dailySubRequest.ChannelId = await _context.Channels
                                                  .Where(channel => channel.TelegramId != null)
                                                  .Select(channel => channel.Id) // Select the database channel ID
                                                  .ToListAsync();
            }
            else
            {
                if (dailySubRequest == null || dailySubRequest.ChannelId == null || !dailySubRequest.ChannelId.Any())
                {
                    return BadRequest("Request must contain at least one channel ID.");
                }

                telegramIds = dailySubRequest.ChannelId
                                             .Select(id => _context.Channels.Find(id))
                                             .Where(channel => channel?.TelegramId != null)
                                             .Select(channel => channel.TelegramId.Value)
                                             .ToList();
            }

            if (!telegramIds.Any())
            {
                return BadRequest("No valid channels found.");
            }

            var results = new List<object>();
            var parameters = new { channel_ids = telegramIds };

            try
            {
                var response = await _webSocketService.CallFunctionAsync("get_subscribers_count_batch", parameters, TimeSpan.FromSeconds(600));
                var subscribersCountDict = _webSocketService.ResponseToObject<Dictionary<long, int>>(response);

                if (subscribersCountDict != null && subscribersCountDict.Any())
                {
                    foreach (var channelId in dailySubRequest.ChannelId)
                    {
                        var channel = await _context.Channels.Include(c => c.StatisticsSheets)
                                                            .FirstOrDefaultAsync(c => c.Id == channelId);
                        if (channel == null || !subscribersCountDict.ContainsKey(channel.TelegramId.Value))
                        {
                            continue; // Skip if the channel is not found or no data for it
                        }

                        var statisticsSheet = channel.StatisticsSheets.FirstOrDefault();
                        if (statisticsSheet == null)
                        {
                            statisticsSheet = new StatisticsSheet { ChannelId = channelId };
                            _context.StatisticsSheets.Add(statisticsSheet);
                            await _context.SaveChangesAsync(); // Ensure the StatisticsSheet is saved before trying to use it
                        }

                        var today = DateTime.UtcNow.Date;
                        // Check for existing record for today
                        var existingRecord = await _context.SubscribersRecords
                                                           .FirstOrDefaultAsync(sr => sr.Sheet == statisticsSheet.Id && sr.Date.Date == today);
                        if (existingRecord != null)
                        {
                            // Skip adding a new record if one already exists for today
                            continue;
                        }

                        var subscribersCount = subscribersCountDict[channel.TelegramId.Value];
                        var subscribersRecord = new SubscribersRecord
                        {
                            Subscribers = subscribersCount,
                            Date = DateTime.UtcNow,
                            Sheet = statisticsSheet.Id
                        };

                        _context.SubscribersRecords.Add(subscribersRecord);
                    }

                    await _context.SaveChangesAsync(); // Save all records at once after the loop
                    results.Add(subscribersCountDict);
                }
                else
                {
                    _logger.LogWarning("No subscriber data received for channels.");
                }
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "Error in deserializing the data received from WebSocket.");
                return BadRequest($"JSON Error: {jsonEx.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while processing the channels.");
                return BadRequest($"Error: {ex.Message}");
            }

            return Ok(results);
        }

        [BypassApiKey]
        [HttpPost("SubscribersHistory")]
        public async Task<ActionResult<List<double>>> GetSubscribersHistoryAsync([FromBody] SubHistoryRequest dailyViewsRequest)
        {
            DateTime startDate;
            DateTime endDate = DateTime.UtcNow;

            if (dailyViewsRequest.Months != null & dailyViewsRequest.Months > 0)
            {
                startDate = DateTime.UtcNow.AddMonths((int)-dailyViewsRequest.Months);
                startDate = new DateTime(startDate.Year, startDate.Month, 1); // Set to the first day of the month

                List<double> bebra = await CalculateDailySubscribersHistory(startDate, endDate, dailyViewsRequest.ChannelId);

                var monthlyTotals = bebra
                   .Select((value, index) => new { Value = value, Month = startDate.AddMonths(index / 30).Month }) // Adjust index to month
                   .GroupBy(x => x.Month)
                   .Select(g => g.Average(x => x.Value)) // Only return the sum (or average)
                   .ToList();

                return monthlyTotals; // This now matches the expected return type
            }
            else
            {
                startDate = DateTime.UtcNow.AddDays(-dailyViewsRequest.NumberOfDays);
                return await CalculateDailySubscribersHistory(startDate, endDate, dailyViewsRequest.ChannelId);
            }
        }

        private async Task<List<double>> CalculateDailySubscribersHistory(DateTime startDate, DateTime endDate, int channelId)
        {
            // Create a complete sequence of dates
            var allDates = Enumerable.Range(0, 1 + (endDate - startDate).Days)
                                     .Select(offset => startDate.AddDays(offset))
                                     .ToList();

            // Retrieve the relevant subscriber records
            var subscriberRecords = await _context.SubscribersRecords
                .Where(sr => sr.SheetNavigation.ChannelId == channelId && sr.Date >= startDate && sr.Date <= endDate)
                .ToListAsync();

            // Convert records to a dictionary for faster lookup
            var recordDict = subscriberRecords.ToDictionary(sr => sr.Date.Date, sr => (double)sr.Subscribers);

            // Prepare a list to hold the final subscriber history
            var subscriberHistory = new List<double>();

            for (int i = 0; i < allDates.Count; i++)
            {
                DateTime currentDate = allDates[i];
                double? calculatedSubscribers = null;

                if (recordDict.TryGetValue(currentDate.Date, out double subscribers))
                {
                    calculatedSubscribers = subscribers;
                }
                else
                {
                    // Calculate average of neighbors if current date is missing
                    calculatedSubscribers = CalculateAverageOfNeighbors(allDates, recordDict, subscriberHistory, i);
                }

                subscriberHistory.Add(calculatedSubscribers ?? 0); // Handle nulls by defaulting to 0
            }

            return subscriberHistory;
        }

        private async Task<List<double>> CalculateMonthlyAverageSubscribers(DateTime startDate, DateTime endDate, int channelId)
        {
            var subscriberRecords = await _context.SubscribersRecords
                .Where(sr => sr.SheetNavigation.ChannelId == channelId && sr.Date >= startDate && sr.Date <= endDate)
                .ToListAsync();

            var monthlyAverages = new List<double>();

            for (var date = startDate; date <= endDate; date = date.AddMonths(1))
            {
                var monthEnd = date.AddMonths(1).AddDays(-1);
                var monthlyRecords = subscriberRecords.Where(sr => sr.Date >= date && sr.Date <= monthEnd).ToList();

                double monthlyAverage = 0;
                if (monthlyRecords.Any())
                {
                    monthlyAverage = monthlyRecords.Average(sr => sr.Subscribers);
                }

                monthlyAverages.Add(monthlyAverage);
            }

            return monthlyAverages;
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
                        channel_name = channel.TelegramId,
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
                    channel_name = channel.TelegramId,
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

        private async Task<List<long>> CollectTelegramIds(IEnumerable<int> channelIds)
        {
            var telegramIds = new List<long>();

            foreach (var id in channelIds)
            {
                var channel = await _context.Channels.FindAsync(id);
                if (channel?.TelegramId != null)
                {
                    telegramIds.Add(channel.TelegramId.Value);
                }
            }

            return telegramIds;
        }

        private async Task<StatisticsSheet> EnsureStatisticsSheetExists(int channelId)
        {
            var channel = await _context.Channels.Include(c => c.StatisticsSheets)
                                                .FirstOrDefaultAsync(c => c.Id == channelId);

            var statisticsSheet = channel?.StatisticsSheets.FirstOrDefault();
            if (statisticsSheet == null && channel != null)
            {
                statisticsSheet = new StatisticsSheet { ChannelId = channelId };
                _context.StatisticsSheets.Add(statisticsSheet);
                await _context.SaveChangesAsync();
            }

            return statisticsSheet;
        }

        private async Task<bool> AddSubscriberRecordAsync(int subscribersCount, int sheetId)
        {
            var today = DateTime.UtcNow.Date;
            var existingRecord = await _context.SubscribersRecords
                                               .FirstOrDefaultAsync(sr => sr.Sheet == sheetId && sr.Date.Date == today);

            if (existingRecord != null)
            {
                _logger.LogInformation($"A subscriber record for sheet {sheetId} already exists for today. Skipping creation.");
                return false;  // Indicate that no new record was added
            }

            var subscribersRecord = new SubscribersRecord
            {
                Subscribers = subscribersCount,
                Date = DateTime.UtcNow,
                Sheet = sheetId
            };

            await _context.SubscribersRecords.AddAsync(subscribersRecord);
            return true; // Indicate that a new record was added
        }

        private double? CalculateAverageOfNeighbors(List<DateTime> allDates, Dictionary<DateTime, double> recordDict, List<double> subscriberHistory, int currentIndex)
        {
            double? prevValue = currentIndex > 0 ? subscriberHistory[currentIndex - 1] : null; // Previous day's subscribers

            // Find next non-null value
            double? nextValue = null;
            for (int j = currentIndex + 1; j < allDates.Count; j++)
            {
                if (recordDict.TryGetValue(allDates[j].Date, out double nextSubscribers))
                {
                    nextValue = nextSubscribers;
                    break;
                }
            }

            // Return the average of the neighbors if both are available
            if (prevValue.HasValue && nextValue.HasValue)
            {
                return (prevValue.Value + nextValue.Value) / 2;
            }
            else
            {
                // Return whichever neighbor is available, or null if neither is
                return prevValue ?? nextValue;
            }
        }

        private async Task<List<MonthViewsRecord>> FetchMonthDataFromDatabase(MonthViewsRequest monthViewsRequest)
        {
            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddMonths(-monthViewsRequest.Months);

            // Fetch the records from the database
            var records = await _context.Channels
                                        .Where(c => c.Id == monthViewsRequest.ChannelId)
                                        .SelectMany(c => c.StatisticsSheets)
                                        .SelectMany(ss => ss.MonthViewsRecords)
                                        .Where(mvr => mvr.Date >= startDate && mvr.Date <= endDate)
                                        .OrderBy(mvr => mvr.Date) // Ensure the records are ordered by date
                                        .ToListAsync();

            if (!records.Any())
            {
                // No records found for the given date range and channel
                return new List<MonthViewsRecord>();
            }

            return records;
        }

        private async Task<bool> IsUpdateRequiredForMonth(MonthViewsRequest monthViewsRequest)
        {
            _logger.LogInformation("Starting IsUpdateRequiredForMonth method.");
            _logger.LogInformation($"Parameters: ChannelId={monthViewsRequest.ChannelId}, Months={monthViewsRequest.Months}");

            TimeSpan outdatedThreshold = TimeSpan.FromMinutes(1); // Define your threshold for outdated data
            _logger.LogInformation($"Outdated threshold set to {outdatedThreshold.TotalMinutes} minutes.");

            var latestMonth = DateTime.UtcNow.AddMonths(-1);
            var year = latestMonth.Year;
            var month = latestMonth.Month;

            var latestRecord = await _context.Channels
                                             .Where(c => c.Id == monthViewsRequest.ChannelId)
                                             .SelectMany(c => c.StatisticsSheets)
                                             .SelectMany(ss => ss.MonthViewsRecords)
                                             .Where(mvr => mvr.Date.Year == year && mvr.Date.Month == month)
                                             .OrderByDescending(mvr => mvr.Date)
                                             .FirstOrDefaultAsync();

            if (latestRecord == null)
            {
                _logger.LogInformation("No records found for the latest month.");
                return true; // No data for the latest month, update required
            }

            var isUpdateRequired = DateTime.UtcNow - latestRecord.Updated > outdatedThreshold;
            _logger.LogInformation($"Update required for the latest month: {isUpdateRequired}");

            return isUpdateRequired;
        }

        private async Task UpdateMonthDataInBackground(MonthViewsRequest monthViewsRequest, Channel channel, IServiceProvider services, bool updateOnlyFirstMonth)
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
                        channel_id = channel.TelegramId,
                        months = updateOnlyFirstMonth ? 1 : monthViewsRequest.Months
                    };

                    // Call the WebSocket service and wait for the response
                    var response = await webSocketService.CallFunctionAsync("getMonthlyViews", parameters, TimeSpan.FromSeconds(30));
                    if (response is OkObjectResult okResult && okResult.Value is string jsonString)
                    {
                        var monthViewsRecords = JsonConvert.DeserializeObject<List<MonthViewsRecord>>(jsonString);
                        if (monthViewsRecords != null && monthViewsRecords.Any())
                        {
                            // Update the database with the new records using the new DbContext instance
                            await UpdateDatabaseWithMonthViewsRecords(monthViewsRecords, monthViewsRequest.ChannelId, dbContext, updateOnlyFirstMonth);
                            logger.LogInformation("Successfully updated database with new month views records for ChannelId: {ChannelId}", monthViewsRequest.ChannelId);
                        }
                        else
                        {
                            logger.LogWarning("Received empty or invalid data from WebSocket for ChannelId: {ChannelId}", monthViewsRequest.ChannelId);
                        }
                    }
                    else
                    {
                        logger.LogWarning("Invalid or no response from WebSocket service for ChannelId: {ChannelId}", monthViewsRequest.ChannelId);
                    }
                }
                catch (JsonException jsonEx)
                {
                    logger.LogError(jsonEx, "Error in deserializing the data received from WebSocket for ChannelId: {ChannelId}", monthViewsRequest.ChannelId);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An unexpected error occurred while updating month views data in the background for ChannelId: {ChannelId}", monthViewsRequest.ChannelId);
                }
            }
        }

        private async Task UpdateDatabaseWithMonthViewsRecords(List<MonthViewsRecord> monthViewsRecords, int channelId, TgDbContext dbContext, bool updateOnlyFirstMonth)
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

            foreach (var record in monthViewsRecords)
            {
                // Ensure the record dates are handled correctly (e.g., UTC)
                record.Updated = DateTime.SpecifyKind(record.Updated, DateTimeKind.Utc);
                record.Date = DateTime.SpecifyKind(record.Date, DateTimeKind.Utc);

                record.Sheet = statisticsSheet.Id;

                var existingRecord = statisticsSheet.MonthViewsRecords
                                                    .FirstOrDefault(mvr => mvr.Date == record.Date);
                if (existingRecord != null)
                {
                    // Update existing record
                    existingRecord.Views = record.Views;
                    existingRecord.LastMessageId = record.LastMessageId;
                    existingRecord.Updated = DateTime.UtcNow;
                }
                else
                {
                    // Add new record
                    record.Updated = DateTime.UtcNow;
                    statisticsSheet.MonthViewsRecords.Add(record);
                }

                if (updateOnlyFirstMonth)
                    break; // Update only the first record if specified
            }

            await dbContext.SaveChangesAsync();
        }

        private async Task<List<MonthViewsRecord>> WaitForWebSocketAndUpdateForMonth(MonthViewsRequest request, Channel channel)
        {
            try
            {
                // Construct parameters for WebSocket request
                var parameters = new
                {
                    channel_id = channel.TelegramId,
                    months = request.Months
                };

                // Call the WebSocket service and wait for the response
                var response = await _webSocketService.CallFunctionAsync("getMonthlyViews", parameters, TimeSpan.FromSeconds(600));
                if (response is OkObjectResult okResult && okResult.Value is string jsonString)
                {
                    // Deserialize the response
                    var monthViewsRecords = JsonConvert.DeserializeObject<List<MonthViewsRecord>>(jsonString);
                    if (monthViewsRecords != null && monthViewsRecords.Any())
                    {
                        // Update the database with the new records
                        await UpdateDatabaseWithMonthViewsRecords(monthViewsRecords, request.ChannelId, _context, false);

                        // Convert the records to a list of month views to return
                        return monthViewsRecords;
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
            return new List<MonthViewsRecord>(); // Return an empty list if there's an error or no data
        }


        private async Task<Channel> FindChannelById(int id)
        {
            return await _context.Channels.FindAsync(id);
        }
    }
    }
