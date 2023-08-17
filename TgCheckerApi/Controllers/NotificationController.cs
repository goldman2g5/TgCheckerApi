using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TgCheckerApi.Models;
using TgCheckerApi.Models.BaseModels;

namespace TgCheckerApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationController : ControllerBase
    {
        private readonly TgDbContext _context;

        public NotificationController(TgDbContext context)
        {
            _context = context;
        }

        // GET: api/GetNotifications
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Notification>>> GetNotifications()
        {
            // Retrieve the current date and time
            DateTime currentTime = DateTime.Now;
            int timeToNotify = 1;

            // Retrieve the notifications that are ready to be sent
            var notifications = await _context.ChannelAccesses
                .Include(ca => ca.Channel)
                .Include(ca => ca.User)
                .Where(ca =>
                    ca.Channel.Notifications == true &&
                    ca.Channel.NotificationSent != true && // Check if the notification hasn't been sent yet
                    ca.Channel.LastBump != null &&
                    ca.Channel.LastBump.Value <= currentTime &&
                    currentTime >= ca.Channel.LastBump.Value.AddMinutes(timeToNotify)) // Check if the current time is past the send time
                .Select(ca => new Notification
                {
                    ChannelAccess = ca,
                    ChannelName = ca.Channel.Name,
                    ChannelId = ca.Channel.Id,
                    SendTime = ca.Channel.LastBump.Value <= currentTime ? currentTime : ca.Channel.LastBump.Value.AddMinutes(timeToNotify),
                    TelegramUserId = (int)ca.User.TelegramId,
                    TelegramChatId = (int)ca.User.ChatId,
                    TelegamChannelId = (long)ca.Channel.TelegramId
                })
                .ToListAsync();

            // Update the NotificationSent property of the channels that were selected for notification
            foreach (var notification in notifications)
            {
                notification.ChannelAccess.Channel.NotificationSent = true;
            }

            await _context.SaveChangesAsync();

            return notifications;
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



        private bool ChannelExists(int id)
        {
            return (_context.Channels?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
