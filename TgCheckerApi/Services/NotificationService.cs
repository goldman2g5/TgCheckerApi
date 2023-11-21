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
                    ca.Channel.NotificationSent != true &&
                    ca.Channel.LastBump != null &&
                    ca.Channel.LastBump.Value <= currentTime &&
                    currentTime >= ca.Channel.LastBump.Value.AddMinutes(timeToNotify))
                .Select(ca => new BumpNotification
                {
                    ChannelAccess = ca,
                    ChannelName = ca.Channel.Name,
                    ChannelId = ca.Channel.Id,
                    TelegramUserId = (int)ca.User.TelegramId,
                    TelegramChatId = (int)ca.User.ChatId,
                    TelegamChannelId = (long)ca.Channel.TelegramId,
                    UniqueKey = ca.User.UniqueKey
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

        public async Task<Notification> CreateNotificationAsync(int channelId, string content, int typeId, int userid)
        {
            // Optional: Validate if the provided TypeId exists in the NotificationType table
            var notificationType = await _context.NotificationTypes
                .FirstOrDefaultAsync(nt => nt.Id == typeId);
            if (notificationType == null)
            {
                throw new ArgumentException("Invalid notification type.");
            }

            var newNotification = new Notification
            {
                ChannelId = channelId,
                Content = content,
                UserId = userid,
                Date = DateTime.UtcNow, // Assuming you want to set the current time as the notification date
                IsNew = true, // Assuming a new notification is always set to IsNew = true
                TypeId = typeId
            };

            _context.Notifications.Add(newNotification);
            await _context.SaveChangesAsync();

            return newNotification;
        }


    }
}
