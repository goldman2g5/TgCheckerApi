using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TgCheckerApi.MiddleWare;
using TgCheckerApi.Models;
using TgCheckerApi.Models.BaseModels;
using TgCheckerApi.Models.NotificationModels;
using TgCheckerApi.Models.PostModels;
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

        [HttpGet("UserNotifications")]
        [BypassApiKey]
        [RequiresJwtValidation]
        public async Task<IActionResult> GetUserNotifications()
        {
            var uniqueKeyClaim = User.FindFirst(c => c.Type == "key")?.Value;

            if (string.IsNullOrEmpty(uniqueKeyClaim))
            {
                return Unauthorized();
            }

            var user = await _context.Users
                                     .SingleOrDefaultAsync(u => u.UniqueKey == uniqueKeyClaim);

            if (user == null)
            {
                return NotFound("User not found.");
            }

            var notifications = await _context.Notifications
                                              .Where(n => n.UserId == user.Id)
                                              .ToListAsync();
            if (notifications == null)
            {
                return NoContent();
            }


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

        [HttpPost("CreateNotification")]
        public async Task<IActionResult> CreateNotification(CreateNotificationPostModel payload)
        {
            try
            {
                var notification = await _notificationService.CreateNotificationAsync(payload.channelid, payload.content, payload.typeid, payload.userid);
                return Ok(notification);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch
            {
                // Handle other potential exceptions
                return StatusCode(500, "An error occurred while creating the notification");
            }
        }

        // POST: api/Notification/MarkAsRead
        [BypassApiKey]
        [RequiresJwtValidation]
        [HttpPost("MarkAsRead/{notificationId}")]
        public async Task<IActionResult> MarkNotificationAsRead(int notificationId)
        {
            var uniqueKeyClaim = User.FindFirst(c => c.Type == "key")?.Value;

            if (string.IsNullOrEmpty(uniqueKeyClaim))
            {
                return Unauthorized();
            }

            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification == null)
            {
                return NotFound($"Notification with ID {notificationId} not found.");
            }

            var user = await _context.Users
                                     .SingleOrDefaultAsync(u => u.UniqueKey == uniqueKeyClaim);

            if (user.Id != notification.UserId)
            {
                return Unauthorized();
            }

            // Mark the notification as not new
            notification.IsNew = false;

            // Save changes to the database
            try
            {
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!NotificationExists(notificationId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while marking the notification as read");
            }
        }

        [BypassApiKey]
        [RequiresJwtValidation]
        [HttpPost("MarkAsReadByType/{typeId}")]
        public async Task<IActionResult> MarkNotificationsAsReadByType(int typeId)
        {
            var uniqueKeyClaim = User.FindFirst(c => c.Type == "key")?.Value;

            if (string.IsNullOrEmpty(uniqueKeyClaim))
            {
                return Unauthorized();
            }

            var user = await _context.Users.SingleOrDefaultAsync(u => u.UniqueKey == uniqueKeyClaim);
            if (user == null)
            {
                return Unauthorized();
            }

            var notifications = await _context.Notifications
                                              .Where(n => n.TypeId == typeId && n.UserId == user.Id && n.IsNew)
                                              .ToListAsync();

            if (!notifications.Any())
            {
                return NotFound("No new notifications found for the specified type.");
            }

            notifications.ForEach(n => n.IsNew = false);

            // Save changes to the database
            try
            {
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while marking the notifications as read");
            }
        }



        private bool NotificationExists(int id)
        {
            return (_context.Notifications?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
