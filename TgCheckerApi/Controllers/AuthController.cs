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
using System.IO;
using static TgCheckerApi.Controllers.BotController;

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
        private readonly ILogger<AuthController> _logger;
        private readonly NotificationService _notificationService;
        private readonly WebSocketService _webSocketService;
        private readonly IServiceProvider _serviceProvider;

        public AuthController(IHubContext<AuthHub> hubContext, TgDbContext context, WebSocketService webSocketService, IMapper mapper, ILogger<AuthController> logger, IServiceProvider serviceProvider)
        {
            _hubContext = hubContext;
            _context = context;
            _userService = new UserService(context);
            _notificationService = new NotificationService(context);
            _logger = logger;
            _mapper = mapper;
            _webSocketService = webSocketService;
            _serviceProvider = serviceProvider;
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
                UserId = user.Id.ToString(),
                Username = payload.Username,
            };

            await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveMessage", JsonConvert.SerializeObject(response));
            return Ok(token);
        }

        // GET: api/User

        [BypassApiKey]
        [RequiresJwtValidation]
        [HttpGet("")]
        public async Task<ActionResult<UserProfileModel>> GetMe()
        {
            try
            {
                var uniqueKeyClaim = User.FindFirst(c => c.Type == "key")?.Value;

                var user = await _userService.GetUserWithRelations(uniqueKeyClaim);

                if (user == null)
                {
                    return NotFound("User does not exist");
                }

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await UpdateUserProfileIfNeeded(user);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error updating user profile in background");
                        // Depending on your error handling policies, you might log this error or handle it differently.
                        // Be cautious about swallowing exceptions without logging or monitoring, as it can make debugging difficult.
                    }
                });

                var userProfile = new UserProfileModel
                {
                    Comments = _mapper.Map<IEnumerable<CommentUserProfileGetModel>>(user.Comments.Where(c => c.ParentId == null)).ToList(),
                    Avatar = user.Avatar,
                    UserName = user.Username,
                    UserId = user.Id
                };

                var channels = user.ChannelAccesses.Select(ca => ca.Channel).ToList();
                var mappedChannels = _mapper.Map<IEnumerable<ChannelGetModel>>(channels);
                userProfile.Channels = mappedChannels.ToList();

                return Ok(userProfile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetMe method");
                return StatusCode(500, $"Error on {DateTime.UtcNow}: {ex.Message}\n{ex.StackTrace}\n");
            }
        }

        private async Task UpdateUserProfileIfNeeded(User user)
        {

            // Check if update is needed based on LastUpdate
            if (user == null || user.LastUpdate.HasValue && (DateTime.UtcNow - user.LastUpdate.Value).TotalDays <= 1)
            {
                return;
            }

            using (var scope = _serviceProvider.CreateScope())
            {
                var scopedDbContext = scope.ServiceProvider.GetRequiredService<TgDbContext>();
                var webSocketService = scope.ServiceProvider.GetRequiredService<WebSocketService>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<AuthController>>();

                if (scopedDbContext.Entry(user).State == EntityState.Detached)
                {
                    scopedDbContext.Users.Attach(user);
                }

                try
                {
                    var parameters = new { user_id = user.TelegramId };
                    var response = await webSocketService.CallFunctionAsync("getProfilePictureAndUsername", parameters, TimeSpan.FromSeconds(30));

                    // Use the ResponseToObject<T> method to simplify deserialization
                    var profileData = webSocketService.ResponseToObject<ProfileUpdateResponse>(response);

                    if (profileData != null)
                    {
                        bool updated = false;
                        if (!string.IsNullOrEmpty(profileData.avatar))
                        {
                            user.Avatar = Convert.FromBase64String(profileData.avatar); // Update avatar
                            scopedDbContext.Entry(user).State = EntityState.Modified;
                            updated = true;
                        }

                        if (!string.IsNullOrEmpty(profileData.username))
                        {
                            user.Username = profileData.username; // Update username
                            scopedDbContext.Entry(user).State = EntityState.Modified;
                            updated = true;
                        }

                        if (updated)
                        {
                            user.LastUpdate = DateTime.UtcNow; // Update last update timestamp
                            var result = await scopedDbContext.SaveChangesAsync();
                            logger.LogInformation($"{result} entities were saved to the database.");

                        }
                    }
                }
                catch (InvalidOperationException ex)
                {
                    logger.LogError(ex, "Invalid operation when updating user profile.");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error updating user profile in background");
                    // Handle or log the error as needed
                }
            }
        }

        [HttpGet("ValidateUniqueKey/{uniqueKey}")]
        public async Task<IActionResult> ValidateUniqueKey(string uniqueKey)
        {
            var userExists = await _context.Users.AnyAsync(u => u.UniqueKey == uniqueKey);
            return userExists ? (IActionResult)Ok() : NotFound();
        }
    }
}