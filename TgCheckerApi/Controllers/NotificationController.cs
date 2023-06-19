using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TgCheckerApi.Models;

namespace TgCheckerApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationController : ControllerBase
    {
        private readonly TgCheckerDbContext _context;

        public NotificationController(TgCheckerDbContext context)
        {
            _context = context;
        }

        // GET: api/GetNotifications
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Notification>>> GetNotifications()
        {
            // Retrieve the current date and time
            DateTime currentTime = DateTime.Now;

            // Retrieve the notifications that are ready to be sent
            var notifications = await _context.ChannelAccesses
                .Include(ca => ca.Channel)
                .Include(ca => ca.User)
                .Where(ca => ca.Channel.Notifications == true && ca.Channel.LastBump != null && ca.Channel.LastBump.Value <= currentTime)
                .Select(ca => new Notification
                {
                    ChannelAccess = ca,
                    ChannelName = ca.Channel.Name,
                    SendTime = ca.Channel.LastBump.Value <= currentTime ? currentTime : ca.Channel.LastBump.Value.AddMinutes(30),
                    TelegramUserId = (int)ca.User.TelegramId,
                    TelegramChatId = (int)ca.User.ChatId
                })
                .ToListAsync();

            return notifications;
        }



        private bool ChannelExists(int id)
        {
            return (_context.Channels?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
