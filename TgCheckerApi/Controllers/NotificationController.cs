using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TgCheckerApi.Models;
using TgCheckerApi.Models.BaseModels;
using TgCheckerApi.Models.NotificationModels;
using TgCheckerApi.Services;

namespace TgCheckerApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationController : ControllerBase
    {
        private readonly TgDbContext _context;
        private readonly NotificationService _notificationService;

        public NotificationController(TgDbContext context)
        {
            _context = context;
            _notificationService = new NotificationService(_context);
        }

        // GET: api/GetNotifications
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BumpNotification>>> GetNotifications()
        {
            var notifications = await _notificationService.GetBumpNotifications();

            return Ok(notifications);
        }

        [HttpGet("GetPromoPosts")]
        public ActionResult<IEnumerable<PromoPost>> GetEligiblePromoPosts()
        {
            DateTime currentTime = DateTime.Now;
            TimeOnly currentTimeOnly = new TimeOnly(currentTime.Hour, currentTime.Minute, currentTime.Second);

            var eligibleChannels = _context.Channels
                .Where(c => c.PromoPost == true &&
                            (c.PromoPostTime == null || c.PromoPostTime <= currentTimeOnly) &&
                            (c.PromoPostLast == null || c.PromoPostLast.Value.AddDays(c.PromoPostInterval ?? 0) <= currentTime))
                .ToList();

            var eligiblePromoPosts = eligibleChannels
                .Select(c => new PromoPost
                {
                    channelId = c.Id.ToString(),
                    channelTelegramId = c.TelegramId.ToString(),
                    channelTelegramName = c.Name.ToString()
                })
                .ToList();

            foreach (var channel in eligibleChannels)
            {
                channel.PromoPostLast = currentTime;
            }

            _context.SaveChanges();

            return Ok(eligiblePromoPosts);
        }

        [HttpGet("GetReportNotifications")]
        public async Task<IActionResult> GetReportNotifications()
        {
            // Step 1: Retrieve Reports
            var reports = await _context.Reports
                .Where(r => r.NotificationSent == false || r.NotificationSent == null)
                .Include(r => r.Channel)
                // Assuming 'User' is navigable from 'Report'
                .Include(r => r.User)
                .ToListAsync();

            // Step 2: Prepare Notification Data
            var notifications = reports.Select(report => new ReportNotificationSupport
            {
                ReportId = report.Id,
                ReporteeName = report.User.Username,
                ReporteeId = report.UserId,
                ChannelName = report.Channel.Name, // Assuming 'Channel' has a 'Name' property
                ChannelId = report.ChannelId,
                // Step 3: Get Chat IDs of Staff Members
                Targets = _context.Staff
                          .Include(s => s.User)
                          .Select(s => s.User.ChatId)
                          .ToList() // Get all staff member Chat IDs
            }).ToList();

            // Update Report Status (Set NotificationSent to true)
            //reports.ForEach(report => report.NotificationSent = true);
            //_context.Reports.UpdateRange(reports);
            //await _context.SaveChangesAsync();

            // Return the list of notifications
            return Ok(notifications);
        }



        private bool ChannelExists(int id)
        {
            return (_context.Channels?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
