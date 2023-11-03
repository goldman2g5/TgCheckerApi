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
using TgCheckerApi.MiddleWare;
using TgCheckerApi.Models.GetModels;
using TgCheckerApi.Services;
using AutoMapper;

namespace TgCheckerApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IHubContext<AuthHub> _hubContext;
        private readonly TgDbContext _context;
        private readonly IMapper _mapper;
        private readonly UserService _userService;

        public AuthController(IHubContext<AuthHub> hubContext, TgDbContext context, IMapper mapper)
        {
            _hubContext = hubContext;
            _context = context;
            _userService = new UserService(context);
            _mapper = mapper;
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

        // GET: api/User
        [HttpGet]
        [RequiresJwtValidation]
        public async Task<ActionResult<UserProfileModel>> GetMe()
        {
            var uniqueKeyClaim = User.FindFirst(c => c.Type == "key")?.Value;

            var user = await _userService.GetUserWithRelations(uniqueKeyClaim);
            Console.WriteLine(uniqueKeyClaim);

            if (user == null)
            {
                return NotFound("User does not exist");
            }

            var userProfile = new UserProfileModel
            {
                Channels = _mapper.Map<IEnumerable<ChannelGetModel>>(user.ChannelAccesses.Select(ca => ca.Channel)).ToList(),
                Comments = _mapper.Map<IEnumerable<CommentUserProfileGetModel>>(user.Comments.Where(c => c.ParentId == null)).ToList()

            };

            return userProfile;

        }

        [HttpGet("/Reports")]
        [RequiresJwtValidation]
        public async Task<ActionResult<UserProfileModel>> GetReports()
        {
            var uniqueKeyClaim = User.FindFirst(c => c.Type == "key")?.Value;

            var user = await _userService.GetUserWithRelations(uniqueKeyClaim);
            Console.WriteLine(uniqueKeyClaim);

            if (user == null)
            {
                return NotFound("User does not exist");
            }

            var userProfile = new UserProfileModel
            {
                Channels = _mapper.Map<IEnumerable<ChannelGetModel>>(user.ChannelAccesses.Select(ca => ca.Channel)).ToList(),
                Comments = _mapper.Map<IEnumerable<CommentUserProfileGetModel>>(user.Comments.Where(c => c.ParentId == null)).ToList()

            };

            return userProfile;

        }

        [HttpGet("ValidateUniqueKey/{uniqueKey}")]
        public async Task<IActionResult> ValidateUniqueKey(string uniqueKey)
        {
            var userExists = await _context.Users.AnyAsync(u => u.UniqueKey == uniqueKey);
            return userExists ? (IActionResult)Ok() : NotFound();
        }
    }
}