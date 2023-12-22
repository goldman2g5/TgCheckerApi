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
        private readonly ILogger<BotController> _logger;


        public BotController(TgDbContext context, ILogger<BotController> logger, IHubContext<BotHub> hubContext, TaskManager taskManager, WebSocketService webSocketService)
        {
            _context = context;
            _hubContext = hubContext;
            _taskManager = taskManager;
            _webSocketService = webSocketService;
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

            var response = await _webSocketService.CallFunctionAsync("getSubscribersCount", parameters, TimeSpan.FromSeconds(30));

            return Ok(response);
        }

        [RequiresJwtValidation]
        [BypassApiKey]
        [HttpPost("getDailyViewsByChannel")]
        public async Task<IActionResult> CallGetDailyViewsByChannel([FromBody] DailyViewsRequest dailyViewsRequest)
        {
            Console.WriteLine("JOPA");
            var uniqueKeyClaim = User.FindFirst(c => c.Type == "key")?.Value;

            var user = await _userService.GetUserWithRelations(uniqueKeyClaim);

            if (user == null)
            {
                return Unauthorized();
            }

            var channel = await FindChannelById(dailyViewsRequest.ChannelId);
            if (channel == null || string.IsNullOrEmpty(channel.Url))
            {
                return BadRequest("Channel not found or URL is missing.");
            }

            if (!_userService.UserHasAccessToChannel(user, channel))
            {
                return Unauthorized();
            }

            bool isUpdateRequired = await IsUpdateRequiredForChannel(dailyViewsRequest);
            if (!isUpdateRequired)
            {
                _logger.LogInformation("Data is up-to-date. Fetching current data from database.");

                // Fetch and return the existing data from the database
                var existingData = await _context.Channels
                                                 .Where(c => c.Id == dailyViewsRequest.ChannelId)
                                                 .SelectMany(c => c.StatisticsSheets)
                                                 .SelectMany(ss => ss.ViewsRecords)
                                                 .Where(vr => vr.Date >= DateTime.UtcNow.AddDays(-dailyViewsRequest.NumberOfDays) && vr.Date <= DateTime.UtcNow)
                                                 .Select(vr => vr.Views)
                                                 .ToListAsync();
                existingData.Reverse();
                return Ok(existingData);  // Return the actual data
            }

            var channelNameFormatted = "@" + channel.Url.Replace("https://t.me/", "");

            var parameters = new
            {
                channel_name = channelNameFormatted,
                number_of_days = dailyViewsRequest.NumberOfDays
            };

            try
            {
                var response = await _webSocketService.CallFunctionAsync("getDailyViewsByChannel", parameters, TimeSpan.FromSeconds(30));

                if (response is OkObjectResult okResult)
                {
                    if (okResult.Value is string jsonString)
                    {
                        var viewsRecords = JsonConvert.DeserializeObject<List<ViewsRecord>>(jsonString);
                        if (viewsRecords != null)
                        {
                            var viewsList = viewsRecords.Select(record => record.Views).ToList();

                            await UpdateDatabaseWithViewsRecords(viewsRecords, dailyViewsRequest.ChannelId);
                            viewsList.Reverse();
                            return Ok(viewsList);
                            //return Ok(new { ViewsRecords = viewsRecords, ViewsList = viewsList });
                        }
                        else
                        {
                            return BadRequest("Failed to deserialize the data.");
                        }
                    }
                    else
                    {
                        return BadRequest("Invalid response format.");
                    }
                }
                else
                {
                    return response;
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON deserialization error.");
                return BadRequest("Error processing the result data.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred.");
                return StatusCode(500, "Internal server error");
            }
        }

        private async Task UpdateDatabaseWithViewsRecords(List<ViewsRecord> viewsRecords, int channelId)
        {
            var channel = await _context.Channels
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
                _context.StatisticsSheets.Add(statisticsSheet);
                await _context.SaveChangesAsync();
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
                    existingRecord.Updated = DateTime.UtcNow; // Set Updated to UtcNow
                }
                else
                {
                    record.Updated = DateTime.UtcNow; // Set Updated to UtcNow before adding
                    statisticsSheet.ViewsRecords.Add(record);
                }

            }

            await _context.SaveChangesAsync();
        }

        private async Task<bool> IsUpdateRequiredForChannel(DailyViewsRequest dailyViewsRequest)
        {
            _logger.LogInformation("Starting IsUpdateRequiredForChannel method.");
            _logger.LogInformation($"Parameters: ChannelId={dailyViewsRequest.ChannelId}, NumberOfDays={dailyViewsRequest.NumberOfDays}");

            TimeSpan outdatedThreshold = TimeSpan.FromMinutes(1); // 5 minutes for testing
            _logger.LogInformation($"Outdated threshold set to {outdatedThreshold.TotalMinutes} minutes.");

            var today = DateTime.UtcNow.Date;
            var startDate = today.AddDays(-dailyViewsRequest.NumberOfDays);
            _logger.LogInformation($"Checking records from {startDate} to {today}.");

            // Directly access the ViewsRecords through the Channel's navigational properties
            var recordsInRange = await _context.Channels
                                                .Where(c => c.Id == dailyViewsRequest.ChannelId)
                                                .SelectMany(c => c.StatisticsSheets)
                                                .SelectMany(ss => ss.ViewsRecords)
                                                .Where(vr => vr.Date >= startDate && vr.Date <= today)
                                                .ToListAsync();

            // Update is required if there are no records in the specified range or if any record is outdated
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
