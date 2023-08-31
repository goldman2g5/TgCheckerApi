using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using TgCheckerApi.Models;
using TgCheckerApi.Models.BaseModels;
using TgCheckerApi.Websockets;
using TgCheckerApi.Utility;


namespace TgCheckerApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IHubContext<ChatHub> _hubContext;

        public AuthController(IHubContext<ChatHub> hubContext)
        {
            _hubContext = hubContext;
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage([FromQuery] string connectionId, [FromBody] SendMessagePayload payload)
        {
                string token = TokenUtility.CreateToken(payload);

                var response = new SendMessageResponse()
                {
                    Token = token,
                    Username = payload.Username,
                    UserId = payload.UserId
                };

                await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveMessage", JsonConvert.SerializeObject(response));
                return Ok(token);
        }
    }
}