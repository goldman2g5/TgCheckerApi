using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using TgCheckerApi.Websockets;
using System.Text.Json;
using TgCheckerApi.Services;
using TgCheckerApi.Models.BaseModels;

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


        public BotController(TgDbContext context, IHubContext<BotHub> hubContext, TaskManager taskManager, WebSocketService webSocketService)
        {
            _context = context;
            _hubContext = hubContext;
            _taskManager = taskManager;
            _webSocketService = webSocketService;
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

        [HttpPost("getDailyViewsByChannel")]
        public async Task<IActionResult> CallGetDailyViewsByChannel([FromBody] DailyViewsRequest dailyViewsRequest)
        {
            var channel = await FindChannelById(dailyViewsRequest.ChannelId); // Fetching the channel by ID
            if (channel == null || string.IsNullOrEmpty(channel.Url))
            {
                return BadRequest("Channel not found or URL is missing.");
            }

            // Extracting and formatting the channel name from the URL
            var channelNameFormatted = "@" + channel.Url.Replace("https://t.me/", "");

            var parameters = new
            {
                channel_name = channelNameFormatted,
                number_of_days = dailyViewsRequest.NumberOfDays
            };

            return await _webSocketService.CallFunctionAsync("getDailyViewsByChannel", parameters, TimeSpan.FromSeconds(30));
        }

        private async Task<Channel> FindChannelById(int id)
        {
            return await _context.Channels.FindAsync(id);
        }
    }
    }
