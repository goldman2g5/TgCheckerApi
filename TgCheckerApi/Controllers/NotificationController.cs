using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Channels;
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

        public NotificationController(TgDbContext context, NotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        

        // GET: api/GetNotifications
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TelegramNotification>>> GetNotifications()
        {
            var notifications = await _notificationService.GetBumpNotifications();

            return Ok(notifications);
        }

        [HttpPost("SendToTelegram")]
        public async Task<IActionResult> SendNotification([FromBody] List<TelegramNotification> model)
        {
            if (model == null)
            {
                return BadRequest("Notification data is required.");
            }

            try
            {
                await _notificationService.SendTelegramNotificationAsync(model);
                return Ok("Notification sent successfully.");
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
            catch (HttpRequestException httpEx)
            {
                // This block can be used to capture HTTP request errors, including 422 errors.
                // You may need to read the response content to log or return a more detailed error message.
                return StatusCode(500, $"HTTP request error: {httpEx.Message}");
            }
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
                    channelTelegramId = "@" + c.Url.Replace("https://t.me/", ""),
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
                var notification = await _notificationService.CreateNotificationAsync(payload.content, payload.typeid, payload.userid, payload.channelid);
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

        [HttpPost("SetNotificationSettings/{telegramId}")]
        public async Task<IActionResult> SetNotificationSettings(long telegramId, [FromBody] NotificationSettingDto settingsDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // First, find the user
            var user = await _context.Users.FirstOrDefaultAsync(u => u.TelegramId == telegramId);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            // Assuming one-to-one in a one-to-many structure: find or create the settings
            var settings = await _context.NotificationSettings
                                          .FirstOrDefaultAsync(ns => ns.Users.Any(u => u.TelegramId == telegramId));

            if (settings == null)
            {
                settings = new NotificationSetting
                {
                    Bump = settingsDto.Bump,
                    General = settingsDto.General,
                    Important = settingsDto.Important,
                    // Initialize other properties from settingsDto as needed
                };
                // Link the new settings to the user
                // This line assumes there's a way to link back from settings to user, adjust according to your data model
                user.NotificationSettingsNavigation = settings;
            }
            else
            {
                // If settings exist, update them
                settings.Bump = settingsDto.Bump;
                settings.General = settingsDto.General;
                settings.Important = settingsDto.Important;
                // Update other properties as needed
            }

            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpGet("GetNotificationSettings/{telegramId}")]
        public async Task<ActionResult<NotificationSettingDto>> GetNotificationSettings(long telegramId)
        {
            var settings = await _context.Users
                                         .Where(u => u.TelegramId == telegramId)
                                         .Select(u => u.NotificationSettingsNavigation)
                                         .FirstOrDefaultAsync();

            if (settings == null)
            {
                // Assuming user exists but does not have settings, create them.
                // Consider adding a check or logic to ensure the user exists to avoid orphan settings.
                settings = new NotificationSetting
                {
                    Bump = true,
                    Important = true,
                    General = true // Assuming you have a General setting, adjust accordingly
                                   // Initialize other properties as needed
                };

                // You would need to attach these settings to the user here,
                // ensuring that you either have the user entity or adjust your logic to accommodate creating and linking these settings properly.

                _context.NotificationSettings.Add(settings);
                await _context.SaveChangesAsync();
            }

            var settingsDto = new NotificationSettingDto
            {
                Bump = settings.Bump,
                Important = settings.Important,
                General = settings.General,
                // Map other properties as needed
            };

            return Ok(settingsDto);
        }

        private bool NotificationExists(int id)
        {
            return (_context.Notifications?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
