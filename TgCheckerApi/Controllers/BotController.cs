using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using TgCheckerApi.Websockets;
using System.Text.Json;
using TgCheckerApi.Services;

namespace TgCheckerApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BotController : ControllerBase
    {
        private readonly IHubContext<BotHub> _hubContext;
        private readonly TaskManager _taskManager;
        private readonly WebSocketService _webSocketService;


        public BotController(IHubContext<BotHub> hubContext, TaskManager taskManager, WebSocketService webSocketService)
        {
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
    }
    }
