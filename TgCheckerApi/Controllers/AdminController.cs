using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TgCheckerApi.Models.BaseModels;
using TgCheckerApi.Models.GetModels;
using TgCheckerApi.Services;
using TgCheckerApi.Websockets;

namespace TgCheckerApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly IHubContext<AuthHub> _hubContext;
        private readonly TgDbContext _context;
        private readonly IMapper _mapper;
        private readonly UserService _userService;
        private readonly NotificationService _notificationService;

        public AdminController(IHubContext<AuthHub> hubContext, TgDbContext context, IMapper mapper)
        {
            _hubContext = hubContext;
            _context = context;
            _userService = new UserService(context);
            _notificationService = new NotificationService(context);
            _mapper = mapper;
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

            if (staffRecord != null)
            {
                query = query.Where(r => r.Status != "hidden");
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

        [HttpGet("ReportById/{id:int}/{telegramId:long}")]
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

        // GET: Support/All
        [HttpGet("GetAllSupports/{adminTelegramId:long}")]
        public async Task<ActionResult<IEnumerable<object>>> GetAllSupports(long adminTelegramId)
        {
            var adminRecord = await _context.Admins.FirstOrDefaultAsync(x => x.TelegramId == adminTelegramId);
            if (adminRecord == null)
            {
                return Unauthorized("You are not authorized to view support staff.");
            }

            var supports = await _context.Staff
                .Join(_context.Users, staff => staff.UserId, user => user.Id,
                      (staff, user) => new { UserId = staff.UserId, TelegramId = user.TelegramId })
                .ToListAsync();

            return Ok(supports);
        }

        [HttpPost("AddSupport/{adminTelegramId:long}/{newSupportTelegramId:long}")]
        public async Task<ActionResult> AddSupport(long adminTelegramId, long newSupportTelegramId)
        {
            // Check if the admin who is trying to add a new support staff exists
            var adminRecord = await _context.Admins.FirstOrDefaultAsync(x => x.TelegramId == adminTelegramId);
            if (adminRecord == null)
            {
                return Unauthorized("You are not authorized to add supports.");
            }

            // Check if the user to be added as support exists and is not already a support staff
            var userToBeSupport = await _context.Users.FirstOrDefaultAsync(u => u.TelegramId == newSupportTelegramId);
            if (userToBeSupport == null)
            {
                return NotFound("The user with the given Telegram ID does not exist.");
            }
            var existingSupport = await _context.Staff.FirstOrDefaultAsync(s => s.UserId == userToBeSupport.Id);
            if (existingSupport != null)
            {
                return BadRequest("This user is already a support staff member.");
            }

            // Add the user as support staff
            var newSupport = new Staff { UserId = userToBeSupport.Id };
            _context.Staff.Add(newSupport);
            await _context.SaveChangesAsync();
            return CreatedAtAction("GetAllSupports", new { id = newSupport.Id }, new { newSupport.UserId });
        }

        [HttpPost("DeleteSupport/{adminTelegramId:long}/{supportTelegramId:long}")]
        public async Task<ActionResult> DeleteSupport(long adminTelegramId, long supportTelegramId)
        {
            // Check if the admin who is trying to delete a support staff exists
            var adminRecord = await _context.Admins.FirstOrDefaultAsync(x => x.TelegramId == adminTelegramId);
            if (adminRecord == null)
            {
                return Unauthorized("You are not authorized to delete supports.");
            }

            // Check if the user to be deleted as support exists
            var userToBeRemoved = await _context.Users.FirstOrDefaultAsync(u => u.TelegramId == supportTelegramId);
            if (userToBeRemoved == null)
            {
                return NotFound("The user with the given Telegram ID does not exist.");
            }

            var existingSupport = await _context.Staff.FirstOrDefaultAsync(s => s.UserId == userToBeRemoved.Id);
            if (existingSupport == null)
            {
                return NotFound("This user is not a support staff member.");
            }

            // Delete the user from support staff
            _context.Staff.Remove(existingSupport);
            await _context.SaveChangesAsync();
            return Ok($"Support with Telegram ID: {supportTelegramId} has been successfully removed.");
        }


        [HttpGet("IsAdmin/{telegramId:long}")]
        public async Task<ActionResult<bool>> IsAdmin(long telegramId)
        {
            var isAdmin = await _userService.IsUserAdminByTelegramId(telegramId);
            return Ok(isAdmin);
        }
    }
}
