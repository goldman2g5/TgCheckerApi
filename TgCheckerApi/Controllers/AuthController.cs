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
using Microsoft.EntityFrameworkCore;

namespace TgCheckerApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly TgDbContext _context;

        public AuthController(IHubContext<ChatHub> hubContext, TgDbContext context)
        {
            _hubContext = hubContext;
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage([FromQuery] string connectionId, [FromBody] SendMessagePayload payload)
        {
            User user = await _context.Users.FirstOrDefaultAsync(x => x.TelegramId == payload.UserId);
            payload.Unique_key = user.UniqueKey;

            string token = TokenUtility.CreateToken(payload);

            var response = new SendMessageResponse()
            {
                Token = token,
                Username = payload.Username,
            };

            await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveMessage", JsonConvert.SerializeObject(response));
            return Ok(token);
        }

        [HttpGet("ValidateUniqueKey/{uniqueKey}")]
        public async Task<IActionResult> ValidateUniqueKey(string uniqueKey)
        {
            var userExists = await _context.Users.AnyAsync(u => u.UniqueKey == uniqueKey);
            return userExists ? (IActionResult)Ok() : NotFound();
        }
    }
}