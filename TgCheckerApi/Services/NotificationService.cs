using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TgCheckerApi.Models.BaseModels;
using TgCheckerApi.Models.NotificationModels;

namespace TgCheckerApi.Services
{
    public class NotificationService
    {
        private readonly TgDbContext _context;

        public NotificationService(TgDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<BumpNotification>> GetBumpNotifications()
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
                .Select(ca => new BumpNotification
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


    }
}
