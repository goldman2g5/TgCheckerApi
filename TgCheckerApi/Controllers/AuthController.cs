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
        private readonly NotificationService _notificationService;

        public AuthController(IHubContext<AuthHub> hubContext, TgDbContext context, IMapper mapper)
        {
            _hubContext = hubContext;
            _context = context;
            _userService = new UserService(context);
            _notificationService = new NotificationService(context);
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
                UserId = user.Id,
                Username = payload.Username,
            };

            await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveMessage", JsonConvert.SerializeObject(response));
            return Ok(token);
        }

        // GET: api/User
        [HttpGet]
        [BypassApiKey]
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

        [HttpGet("Reports/{telegramId:long}")]
        public async Task<ActionResult<IEnumerable<ReportGroup>>> GetReports(long telegramId)
        {
            var adminRecord = await _context.Admins.FirstOrDefaultAsync(x => x.TelegramId == telegramId);
            var staffRecord = await _context.Staff.FirstOrDefaultAsync(x => x.User.TelegramId == telegramId);

            if (adminRecord is null && staffRecord is null)
            {
                return Unauthorized();
            }

            IQueryable<Report> query = _context.Reports.Include(r => r.Channel);

            if (adminRecord != null)
            {
                query = query.Where(r => r.Status == "hidden");
            }

            var reports = await query.ToListAsync();


            var reportGroups = reports
                .GroupBy(r => r.ChannelId)
                .Select(group => new ReportGroup
                {
                    ChannelId = (int)group.Key,
                    ChannelName = group.First().Channel.Name,
                    ChannelUrl = group.First().Channel.Url,
                    ChannelWebUrl = $"http://46.39.232.190:8063/Channel/{group.Key}",
                    LastReport = group.Max(r => r.ReportTime),
                    ReportCount = group.Count(),
                    Reports = _mapper.Map<List<ReportGetModel>>(group.ToList())
                })
                .OrderByDescending(rg => rg.ReportCount)
                .ToList();

            return Ok(reportGroups);
        }

        [HttpGet("Report/{id:int}/{telegramId:long}")]
        public async Task<ActionResult<ReportGetModel>> GetReport(int id, long telegramId)
        {
            var staffRecord = await _context.Staff.Include(s => s.User)
                                                  .FirstOrDefaultAsync(s => s.User.TelegramId == telegramId);

            if (staffRecord == null)
            {
                var adminRecord = await _context.Admins.FirstOrDefaultAsync(x => x.TelegramId == telegramId);
                if (adminRecord == null)
                {
                    return Unauthorized();
                }
            }

            var report = await _context.Reports
                .Include(r => r.Channel)
                .FirstOrDefaultAsync(r => r.Id == id);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == report.Channel.User);

            if (report == null)
            {
                return NotFound();
            }

            var reportModel = _mapper.Map<ReportGetModel>(report);

            reportModel.UserTelegramChatId = user.ChatId;

            return Ok(reportModel);
        }

        [HttpPost("CloseReport/{reportId:int}/{telegramId:long}/{status:int}")]
        public async Task<IActionResult> CloseReport(int reportId, long telegramId, int status)
        {
            var staffRecord = await _context.Staff.Include(s => s.User)
                                                  .FirstOrDefaultAsync(s => s.User.TelegramId == telegramId);

            if (staffRecord == null)
            {
                return Unauthorized();
            }

            var report = await _context.Reports.Include(r => r.Channel)
                                               .FirstOrDefaultAsync(r => r.Id == reportId);
            if (report == null)
            {
                return NotFound();
            }

            string newStatus = status == 1 ? "hidden" : "closed";
            report.Status = newStatus;
            report.StaffId = staffRecord.Id;

            if (newStatus == "hidden" && report.Channel != null)
            {
                report.Channel.Hidden = true;

                if (report.Channel.UserNavigation != null)
                {
                    string notificationContent = $"Your channel {report.Channel.Name} has been hidden due to a report. Please review the channel content.";
                    await _notificationService.CreateNotificationAsync(report.Channel.Id, notificationContent, 2, report.Channel.UserNavigation.Id);
                }
            }

            _context.Update(report);
            if (report.Channel != null)
            {
                _context.Update(report.Channel);
            }
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpPost("AdminCloseReport/{telegramId:long}/{reportId}")]
        public async Task<IActionResult> CloseReport(long telegramId, int reportId)
        {
            var adminRecord = await _context.Admins.FirstOrDefaultAsync(x => x.TelegramId == telegramId);
            if (adminRecord == null)
            {
                return Unauthorized();
            }

            var report = await _context.Reports.FirstOrDefaultAsync(r => r.Id == reportId);
            if (report == null)
            {
                return NotFound("Report not found");
            }

            report.Status = "Closed";
            await _context.SaveChangesAsync();
            return Ok("Report closed successfully");
        }

        [HttpPost("AdminDeleteChannel/{telegramId:long}/{reportId}")]
        public async Task<IActionResult> DeleteChannel(long telegramId, int reportId)
        {
            var adminRecord = await _context.Admins.FirstOrDefaultAsync(x => x.TelegramId == telegramId);
            if (adminRecord == null)
            {
                return Unauthorized();
            }

            var report = await _context.Reports.Include(r => r.Channel).FirstOrDefaultAsync(r => r.Id == reportId);
            if (report == null || report.Channel == null)
            {
                return NotFound("Report or associated channel not found");
            }

            _context.Channels.Remove(report.Channel);
            await _context.SaveChangesAsync();
            return Ok("Channel and related reports deleted successfully");
        }

        [HttpGet("IsAdmin/{telegramId:long}")]
        public async Task<ActionResult<bool>> IsAdmin(long telegramId)
        {
            var isAdmin = await _userService.IsUserAdminByTelegramId(telegramId);
            return Ok(isAdmin);
        }

        [HttpGet("ValidateUniqueKey/{uniqueKey}")]
        public async Task<IActionResult> ValidateUniqueKey(string uniqueKey)
        {
            var userExists = await _context.Users.AnyAsync(u => u.UniqueKey == uniqueKey);
            return userExists ? (IActionResult)Ok() : NotFound();
        }
    }
}