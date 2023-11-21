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
        [RequiresJwtValidation]
        public async Task<IActionResult> GetUserNotifications()
        {
            // Retrieve the user's unique key from the JWT token claims
            var uniqueKeyClaim = User.FindFirst(c => c.Type == "key")?.Value;

            if (string.IsNullOrEmpty(uniqueKeyClaim))
            {
                return Unauthorized("Unique key claim is missing from the token.");
            }

            // Retrieve user information based on the unique key
            var user = await _context.Users
                                     .SingleOrDefaultAsync(u => u.UniqueKey == uniqueKeyClaim);

            if (user == null)
            {
                return NotFound("User not found.");
            }

            // Fetch notifications for the user
            var notifications = await _context.Notifications
                                              .Where(n => n.UserId == user.Id && n.IsNew)
                                              .ToListAsync();

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
        public async Task<IActionResult> CreateNotification(int channelId, string content, int typeId, int userid)
        {
            try
            {
                var notification = await _notificationService.CreateNotificationAsync(channelId, content, typeId, userid);
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
        [HttpPost("MarkAsRead/{notificationId}")]
        public async Task<IActionResult> MarkNotificationAsRead(int notificationId)
        {
            // Find the notification by ID
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification == null)
            {
                return NotFound($"Notification with ID {notificationId} not found.");
            }

            // Mark the notification as not new
            notification.IsNew = false;

            // Save changes to the database
            try
            {
                await _context.SaveChangesAsync();
                return NoContent(); // Standard response for a successful PATCH/PUT request
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
                // Log the exception and return a 500 Internal Server Error
                // Consider logging the exception details here
                return StatusCode(500, "An error occurred while marking the notification as read");
            }
        }



        private bool NotificationExists(int id)
        {
            return (_context.Notifications?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
