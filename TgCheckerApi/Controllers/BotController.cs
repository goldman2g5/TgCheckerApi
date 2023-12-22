using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using TgCheckerApi.Websockets;
using TgCheckerApi.Services;
using TgCheckerApi.Models.BaseModels;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;

namespace TgCheckerApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BotController : ControllerBase
    {
        private readonly IHubContext<BotHub> _hubContext;
        private readonly TaskManager _taskManager;
        private readonly WebSocketService _webSocketService;
        private readonly TgDbContext _context;
        private readonly ILogger<BotController> _logger;


        public BotController(TgDbContext context, ILogger<BotController> logger, IHubContext<BotHub> hubContext, TaskManager taskManager, WebSocketService webSocketService)
        {
            _context = context;
            _hubContext = hubContext;
            _taskManager = taskManager;
            _webSocketService = webSocketService;
            _logger = logger;
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

        [HttpPost("getDailyViewsByChannel")]
        public async Task<IActionResult> CallGetDailyViewsByChannel([FromBody] DailyViewsRequest dailyViewsRequest)
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
                            await UpdateDatabaseWithViewsRecords(viewsRecords, dailyViewsRequest.ChannelId);
                            return Ok(viewsRecords);
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
                    return response; // Propagate other types of responses.
                }
            }
            catch (JsonException ex)
            {
                //_logger.LogError(ex, "JSON deserialization error.");
                return BadRequest("Error processing the result data.");
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "An unexpected error occurred.");
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
                record.Sheet = statisticsSheet.Id;

                var existingRecord = statisticsSheet.ViewsRecords
                                                     .FirstOrDefault(vr => vr.Date == record.Date);
                if (existingRecord != null)
                {
                    existingRecord.Views = record.Views;
                    existingRecord.LastMessageId = record.LastMessageId;
                    // Convert to UTC if not already
                    existingRecord.Updated = DateTime.Now;
                }
                else
                {
                    // Ensure new records are in UTC
                    record.Updated = DateTime.Now;
                    statisticsSheet.ViewsRecords.Add(record);
                }
            }

            await _context.SaveChangesAsync();
        }


        private async Task<Channel> FindChannelById(int id)
        {
            return await _context.Channels.FindAsync(id);
        }
    }
    }
