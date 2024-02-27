using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using TgCheckerApi.Models.BaseModels;
using TgCheckerApi.Models.NotificationModels;

namespace TgCheckerApi.Services
{
    public class NotificationService
    {
        private readonly TgDbContext _context;
        private readonly IHttpClientFactory _clientFactory;

        public NotificationService(TgDbContext context, IHttpClientFactory clientFactory)
        {
            _context = context;
            _clientFactory = clientFactory;
        }

        public async Task<IEnumerable<TelegramNotification>> GetBumpNotifications()
        {
            // Retrieve the current date and time
            DateTime currentTime = DateTime.Now;
            int timeToNotify = 240;

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
                .Select(ca => new TelegramNotification
                {
                    ChannelAccess = ca,
                    ChannelName = ca.Channel.Name,
                    ChannelId = ca.Channel.Id,
                    UserId = (int)ca.UserId,
                    TelegramUserId = ca.User.TelegramId,
                    TelegramChatId = ca.User.ChatId,
                    TelegamChannelId = ca.Channel.TelegramId,
                    UniqueKey = ca.User.UniqueKey
                })
                .ToListAsync();


            // Update the NotificationSent property of the channels that were selected for notification
            foreach (var notification in notifications)
            {
                string content = $"Your channel {notification.ChannelName} is ready for a bump.";
                int typeId = 3;
                await CreateNotificationAsync(notification.ChannelId, content, typeId, notification.UserId);
                notification.ChannelAccess.Channel.NotificationSent = true;
            }

            await _context.SaveChangesAsync();

            return notifications;
        }

        public async Task<Notification> CreateNotificationAsync(int channelId, string content, int typeId, int userid)
        {

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
                Date = DateTime.UtcNow,
                IsNew = true,
                TypeId = typeId
            };

            _context.Notifications.Add(newNotification);
            await _context.SaveChangesAsync();

            return newNotification;
        }

        public async Task SendTelegramNotificationAsync(List<TelegramNotification> notificationModels)
        {
            // Convert TelegramNotification models to TelegramNotificationPostModel
            var postModels = ConvertToPostModels(notificationModels);

            var httpClient = _clientFactory.CreateClient("MyClient");
            var jsonContent = JsonSerializer.Serialize(postModels);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            Console.WriteLine("ZIEG HAIL\nZIEG HAIL\nZIEG HAIL\nZIEG HAIL\nZIEG HAIL\nZIEG HAIL\nZIEG HAIL\nZIEG HAIL\nZIEG HAIL\nZIEG HAIL\n");
            Console.WriteLine(jsonContent);

            string url = "http://127.0.0.1:8000/send_notifications";
            var response = await httpClient.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                // Handle error
                throw new InvalidOperationException($"Error sending notifications: {response}");
            }
        }

        private List<TelegramNotificationPostModel> ConvertToPostModels(List<TelegramNotification> models)
        {
            var postModels = new List<TelegramNotificationPostModel>();
            foreach (var model in models)
            {
                var postModel = new TelegramNotificationPostModel
                {
                    ChannelName = model.ChannelName,
                    ChannelId = model.ChannelId,
                    UserId = model.UserId,
                    TelegramUserId = model.TelegramUserId,
                    TelegramChatId = model.TelegramChatId,
                    TelegamChannelId = model.TelegamChannelId, // Consider correcting the typo to "TelegramChannelId"
                    ContentType = model.ContentType,
                    UniqueKey = model.UniqueKey
                };
                postModels.Add(postModel);
            }
            return postModels;
        }


    }
}
