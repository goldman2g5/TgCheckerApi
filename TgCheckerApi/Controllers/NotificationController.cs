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

        [HttpPost("CreateNotification")]
        public async Task<IActionResult> CreateNotification(int channelId, string content, int typeId)
        {
            try
            {
                var notification = await _notificationService.CreateNotificationAsync(channelId, content, typeId);
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



        private bool ChannelExists(int id)
        {
            return (_context.Channels?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
