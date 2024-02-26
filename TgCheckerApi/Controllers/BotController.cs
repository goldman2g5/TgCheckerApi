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
using TgCheckerApi.Interfaces;

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
        private readonly BotControllerService _botService;


        public BotController(TgDbContext context, IServiceProvider serviceProvider, ILogger<BotController> logger, IHubContext<BotHub> hubContext, TaskManager taskManager, WebSocketService webSocketService, BotControllerService botService)
        {
            _context = context;
            _hubContext = hubContext;
            _taskManager = taskManager;
            _webSocketService = webSocketService;
            _serviceProvider = serviceProvider;
            _logger = logger;
            _userService = new UserService(context);
            _botService = botService;
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
                var response = await _webSocketService.CallFunctionAsync("getBroadcastStats", parameters, TimeSpan.FromSeconds(30));

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

            var monthViewsData = await _botService.FetchMonthDataFromDatabase(monthViewsRequest);
            bool isDataComplete = monthViewsData?.Count >= monthViewsRequest.Months;
            bool isUpdateRequired = await _botService.IsUpdateRequiredForMonth(monthViewsRequest);

            if (isDataComplete && !isUpdateRequired)
            {
                _logger.LogInformation("Data is complete and up-to-date for ChannelId: {ChannelId}.", monthViewsRequest.ChannelId);
            }
            else
            {
                if (isDataComplete)
                {
                    _logger.LogInformation("First month data is outdated for ChannelId: {ChannelId}. Initiating background update.", monthViewsRequest.ChannelId);
                    _ = _botService.UpdateMonthDataInBackground(monthViewsRequest, channel, _serviceProvider, updateOnlyFirstMonth: true);
                }
                else
                {
                    _logger.LogInformation("Data is incomplete for ChannelId: {ChannelId}. Awaiting new data from WebSocket.", monthViewsRequest.ChannelId);
                    monthViewsData = await _botService.WaitForWebSocketAndUpdateForMonth(monthViewsRequest, channel);
                }
            }

            //var viewsList = monthViewsData?.Select(record => (double)record.Views).ToList();

            monthViewsData.Reverse();
            var viewsList = monthViewsData.Select(x => new { x.Views, x.Date });

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
                var response = await _webSocketService.CallFunctionAsync("get_subscribers_count_batch", parameters, TimeSpan.FromSeconds(300));
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

        public class SubscriberHistoryItem
        {
            public DateTime Date { get; set; }
            public double Subscribers { get; set; }
        }

        [BypassApiKey]
        [HttpPost("SubscribersHistory")]
        public async Task<ActionResult<List<SubscriberHistoryItem>>> GetSubscribersHistoryAsync([FromBody] SubHistoryRequest dailyViewsRequest)
        {
            DateTime startDate = DateTime.UtcNow;
            DateTime endDate = DateTime.UtcNow;
            startDate = startDate.ToUniversalTime();
            endDate = endDate.ToUniversalTime();

            if (dailyViewsRequest.Months != null & dailyViewsRequest.Months > 0)
            {
                startDate = DateTime.UtcNow.AddMonths((int)-dailyViewsRequest.Months).ToUniversalTime();
                startDate = new DateTime(startDate.Year, startDate.Month, 1).ToUniversalTime(); // Set to the first day of the month

                var bebra = await _botService.CalculateDailySubscribersHistory(startDate, endDate, dailyViewsRequest.ChannelId);

                var monthlyTotals = bebra
                    .Select((value, index) => new SubscriberHistoryItem { Date = startDate.AddMonths(index / 30).ToUniversalTime(), Subscribers = value })
                    .GroupBy(x => new { x.Date.Year, x.Date.Month })
                    .Select(g => new SubscriberHistoryItem
                    {
                        Date = new DateTime(g.Key.Year, g.Key.Month, 1).ToUniversalTime(),
                        Subscribers = g.Average(x => x.Subscribers)
                    })
                    .Skip(1) // Skip the first result if necessary
                    .ToList();

                return monthlyTotals;
            }
            else
            {
                startDate = DateTime.UtcNow.AddDays(-dailyViewsRequest.NumberOfDays);
                var dailySubscribers = await _botService.CalculateDailySubscribersHistory(startDate, endDate, dailyViewsRequest.ChannelId);

                var dailySubscribersWithDates = dailySubscribers
                    .Select((subscribers, index) => new SubscriberHistoryItem { Date = startDate.AddDays(index).ToUniversalTime(), Subscribers = subscribers })
                    .ToList();

                return dailySubscribersWithDates;
            }
        }

        [BypassApiKey]
        [HttpPost("getDailyViewsByChannel")]
        public async Task<IActionResult> CallGetDailyViewsByChannel([FromBody] DailyViewsRequest dailyViewsRequest)
        {
            _logger.LogInformation("Starting CallGetDailyViewsByChannel method for ChannelId: {ChannelId}", dailyViewsRequest.ChannelId);

            if (dailyViewsRequest.NumberOfDays == 31)
            {
                var today = DateTime.Today;
                var daysInMonth = DateTime.DaysInMonth(today.Year, today.Month);
                dailyViewsRequest.NumberOfDays = daysInMonth;

                _logger.LogInformation("Adjusted NumberOfDays to {NumberOfDays} for the current month.", dailyViewsRequest.NumberOfDays);
            }

            var channel = await FindChannelById(dailyViewsRequest.ChannelId);
            if (channel == null || string.IsNullOrEmpty(channel.Url))
            {
                _logger.LogWarning("Channel not found or URL is missing for ChannelId: {ChannelId}", dailyViewsRequest.ChannelId);
                return BadRequest("Channel not found or URL is missing.");
            }

            var viewsData = await _botService.FetchDataFromDatabase(dailyViewsRequest, considerOutdated: true);
            bool isDataComplete = viewsData?.Count >= dailyViewsRequest.NumberOfDays;
            bool isUpdateRequired = await _botService.IsUpdateRequiredForChannel(dailyViewsRequest);

            if (isDataComplete && !isUpdateRequired)
            {
                _logger.LogInformation("Data is complete and up-to-date for ChannelId: {ChannelId}.", dailyViewsRequest.ChannelId);
            }
            else
            {
                if (isDataComplete)
                {
                    _logger.LogInformation("Data is complete but outdated for ChannelId: {ChannelId}. Initiating background update.", dailyViewsRequest.ChannelId);
                    _ = _botService.UpdateDataInBackground(dailyViewsRequest, channel, _serviceProvider);
                }
                else
                {
                    _logger.LogInformation("Data is incomplete for ChannelId: {ChannelId}. Awaiting new data from WebSocket.", dailyViewsRequest.ChannelId);
                    viewsData = await _botService.WaitForWebSocketAndUpdate(dailyViewsRequest, channel);
                }
            }

            _logger.LogInformation("Returning data for ChannelId: {ChannelId} with {DataCount} view counts", dailyViewsRequest.ChannelId, viewsData?.Count ?? 0);
            viewsData.Reverse();
            var viewsList = viewsData.Select(x => new { x.Views, x.Date });
            return Ok(viewsList);
        }        

        private async Task<Channel> FindChannelById(int id)
        {
            return await _context.Channels.FindAsync(id);
        }
    }
    }
