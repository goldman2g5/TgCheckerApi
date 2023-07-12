using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using TgCheckerApi.Websockets;

namespace TgCheckerApi.Controllers
{
    
    [Route("api/[controller]")]
    [ApiController]
    public class MessageController : ControllerBase
    {
        private readonly IHubContext<ChatHub> _hubContext;

        public MessageController(IHubContext<ChatHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public class SendMessagePayload
        {
            public string Token { get; set; }
            public string Username { get; set; }
            public int UserId { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage([FromQuery] string connectionId, [FromBody] SendMessagePayload payload)
        {
            await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveMessage", JsonConvert.SerializeObject(payload));
            return Ok();
        }
    }
}
