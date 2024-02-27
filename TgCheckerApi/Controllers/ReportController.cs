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
    public class ReportController : ControllerBase
    {
        private readonly IHubContext<AuthHub> _hubContext;
        private readonly TgDbContext _context;
        private readonly IMapper _mapper;
        private readonly UserService _userService;
        private readonly NotificationService _notificationService;

        public ReportController(IHubContext<AuthHub> hubContext, TgDbContext context, IMapper mapper, NotificationService notificationService)
        {
            _hubContext = hubContext;
            _context = context;
            _userService = new UserService(context);
            _notificationService = notificationService;
            _mapper = mapper;
        }

        [HttpGet("ActiveReports/{telegramId:long}")]
        public async Task<ActionResult<IEnumerable<ReportGroup>>> GetActiveReports(long telegramId)
        {
            // Check if the user is an admin or a support staff
            var adminRecord = await _context.Admins.FirstOrDefaultAsync(x => x.TelegramId == telegramId);
            var staffRecord = await _context.Staff.Include(s => s.User)
                                                  .FirstOrDefaultAsync(s => s.User.TelegramId == telegramId);

            // Unauthorized if not an admin or support staff
            if (adminRecord == null && staffRecord == null)
            {
                return Unauthorized();
            }

            // Query for active reports (not hidden, closed, or unresolved)
            var activeReports = await _context.Reports
                .Include(r => r.Channel)
                .Where(r => r.Status != "hidden" && r.Status != "closed" && r.Status != "unresolved")
                .ToListAsync();

            // Grouping reports by ChannelId and mapping to ReportGroup with updated Web URL
            var reportGroups = activeReports
                .GroupBy(r => r.ChannelId)
                .Select(group => new ReportGroup
                {
                    ChannelId = group.Key,
                    ChannelName = group.First().Channel.Name,
                    ChannelUrl = group.First().Channel.Url,
                    ChannelWebUrl = $"https://tgsearch.info/Channel/{group.Key}",
                    LastReport = group.Max(r => r.ReportTime),
                    ReportCount = group.Count(),
                    Reports = _mapper.Map<List<ReportGetModel>>(group.ToList())
                })
                .OrderByDescending(rg => rg.ReportCount)
                .ToList();

            return Ok(reportGroups);
        }

        [HttpPost("HideChannelByReport/{telegramId:long}/{reportId:int}")]
        public async Task<IActionResult> HideChannelByReport(long telegramId, int reportId)
        {
            // Check if the user is an admin or a support staff
            var adminRecord = await _context.Admins.FirstOrDefaultAsync(x => x.TelegramId == telegramId);
            var staffRecord = await _context.Staff.Include(s => s.User)
                                                  .FirstOrDefaultAsync(s => s.User.TelegramId == telegramId);

            // Unauthorized if not an admin or support staff
            if (adminRecord == null && staffRecord == null)
            {
                return Unauthorized();
            }

            // Retrieve the report based on reportId
            var report = await _context.Reports.Include(r => r.Channel).FirstOrDefaultAsync(r => r.Id == reportId);
            if (report == null || report.Channel == null)
            {
                return NotFound("Report or associated channel not found");
            }

            // Set the status of all reports for this channel to "hidden"
            var relatedReports = await _context.Reports.Where(r => r.ChannelId == report.ChannelId).ToListAsync();
            foreach (var relReport in relatedReports)
            {
                relReport.Status = "hidden";
            }

            // Set the Hidden property of the channel to true
            report.Channel.Hidden = true;

            // Save changes to the database
            _context.UpdateRange(relatedReports);
            _context.Update(report.Channel);
            await _context.SaveChangesAsync();

            return Ok("Channel and its reports are now hidden");
        }

        [HttpPost("MarkReportUnresolved/{telegramId:long}/{reportId:int}")]
        public async Task<IActionResult> MarkReportUnresolved(long telegramId, int reportId)
        {
            // Check if the user is an admin or a support staff
            var adminRecord = await _context.Admins.FirstOrDefaultAsync(x => x.TelegramId == telegramId);
            var staffRecord = await _context.Staff.Include(s => s.User)
                                                  .FirstOrDefaultAsync(s => s.User.TelegramId == telegramId);

            // Unauthorized if not an admin or support staff
            if (adminRecord == null && staffRecord == null)
            {
                return Unauthorized();
            }

            // Retrieve the report based on reportId
            var report = await _context.Reports.FirstOrDefaultAsync(r => r.Id == reportId);
            if (report == null)
            {
                return NotFound("Report not found");
            }

            // Set the status of the report to "unresolved"
            report.Status = "unresolved";
            _context.Update(report);
            await _context.SaveChangesAsync();

            return Ok("Report marked as unresolved");
        }

        [HttpGet("HiddenReports/{telegramId:long}")]
        public async Task<ActionResult<IEnumerable<ReportGroup>>> GetHiddenReports(long telegramId)
        {
            // Verify if the request is from an admin
            var adminRecord = await _context.Admins.FirstOrDefaultAsync(x => x.TelegramId == telegramId);

            if (adminRecord == null)
            {
                return Unauthorized();
            }

            // Query for reports with 'hidden' status
            var hiddenReports = await _context.Reports
                .Include(r => r.Channel)
                .Where(r => r.Status == "hidden")
                .ToListAsync();

            // Grouping reports by ChannelId and mapping to ReportGroup
            var reportGroups = hiddenReports
                .GroupBy(r => r.ChannelId)
                .Select(group => new ReportGroup
                {
                    ChannelId = group.Key,
                    ChannelName = group.First().Channel.Name,
                    ChannelUrl = group.First().Channel.Url,
                    ChannelWebUrl = $"https://tgsearch.info/Channel/{group.Key}",
                    LastReport = group.Max(r => r.ReportTime),
                    ReportCount = group.Count(),
                    Reports = _mapper.Map<List<ReportGetModel>>(group.ToList())
                })
                .OrderByDescending(rg => rg.ReportCount)
                .ToList();

            return Ok(reportGroups);
        }

        [HttpPost("CloseReport/{telegramId:long}/{reportId:int}")]
        public async Task<IActionResult> CloseReport(long telegramId, int reportId)
        {
            // Check if the user is an admin or a support staff
            var isAdmin = await _context.Admins.AnyAsync(x => x.TelegramId == telegramId);
            var isSupport = await _context.Staff.Include(s => s.User)
                                                .AnyAsync(s => s.User.TelegramId == telegramId);

            // Unauthorized if not an admin or support staff
            if (!isAdmin && !isSupport)
            {
                return Unauthorized();
            }

            // Retrieve the report based on reportId
            var report = await _context.Reports.FirstOrDefaultAsync(r => r.Id == reportId);
            if (report == null)
            {
                return NotFound("Report not found");
            }

            // Set the status of the report to "closed"
            report.Status = "closed";
            _context.Update(report);
            await _context.SaveChangesAsync();

            return Ok("Report closed successfully");
        }

        [HttpGet("UnresolvedReports/{telegramId:long}")]
        public async Task<ActionResult<IEnumerable<ReportGroup>>> GetUnresolvedReports(long telegramId)
        {
            // Verify if the request is from an admin
            var adminRecord = await _context.Admins.FirstOrDefaultAsync(x => x.TelegramId == telegramId);

            if (adminRecord == null)
            {
                return Unauthorized();
            }

            // Query for reports with 'unresolved' status
            var unresolvedReports = await _context.Reports
                .Include(r => r.Channel)
                .Where(r => r.Status == "unresolved")
                .ToListAsync();

            // Grouping reports by ChannelId and mapping to ReportGroup
            var reportGroups = unresolvedReports
                .GroupBy(r => r.ChannelId)
                .Select(group => new ReportGroup
                {
                    ChannelId = group.Key,
                    ChannelName = group.First().Channel.Name,
                    ChannelUrl = group.First().Channel.Url,
                    ChannelWebUrl = $"https://tgsearch.info/Channel/{group.Key}",
                    LastReport = group.Max(r => r.ReportTime),
                    ReportCount = group.Count(),
                    Reports = _mapper.Map<List<ReportGetModel>>(group.ToList())
                })
                .OrderByDescending(rg => rg.ReportCount)
                .ToList();

            return Ok(reportGroups);
        }
    }
}
