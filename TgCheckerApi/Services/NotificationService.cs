using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quartz;
using System.Text;
using System.Text.Json;
using TgCheckerApi.Controllers;
using TgCheckerApi.Job;
using TgCheckerApi.Models.BaseModels;
using TgCheckerApi.Models.NotificationModels;

namespace TgCheckerApi.Services
{
    public class NotificationService
    {
        private readonly TgDbContext _context;
        private readonly IHttpClientFactory _clientFactory;
        private readonly IScheduler _scheduler;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(TgDbContext context, IHttpClientFactory clientFactory, IScheduler scheduler, ILogger<NotificationService> logger)
        {
            _context = context;
            _clientFactory = clientFactory;
            _scheduler = scheduler;
            _logger = logger;
        }

        public async Task<IEnumerable<TelegramNotification>> GetBumpNotifications()
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
                .Select(ca => new TelegramNotification
                {
                    ChannelAccess = ca,
                    ChannelName = ca.Channel.Name,
                    ChannelId = ca.Channel.Id,
                    UserId = (int)ca.UserId,
                    TelegramUserId = ca.User.TelegramId,
                    TelegramChatId = ca.User.ChatId,
                    TelegamChannelId = ca.Channel.TelegramId
                })
                .ToListAsync();


            // Update the NotificationSent property of the channels that were selected for notification
            foreach (var notification in notifications)
            {
                string content = $"Your channel {notification.ChannelName} is ready for a bump.";
                int typeId = 3;
                await CreateNotificationAsync(content, typeId, notification.UserId, notification.ChannelId,
                    targetTelegram: true,
                    contentType: "bump");
                notification.ChannelAccess.Channel.NotificationSent = true;
            }

            await _context.SaveChangesAsync();

            return notifications;
        }

        public async Task<Notification> CreateNotificationAsync(
        string content,
        int typeId,
        int userId,
        int? channelId = null, // Optional channelId
        bool targetTelegram = false,
        string contentType = null)
        {
            // Validate the notification type
            var notificationType = await _context.NotificationTypes.FirstOrDefaultAsync(nt => nt.Id == typeId);
            if (notificationType == null)
            {
                throw new ArgumentException("Invalid notification type.");
            }

            // Attempt to find the channel early if a channelId is provided
            Channel? channel = null;
            if (channelId.HasValue)
            {
                channel = await _context.Channels.FirstOrDefaultAsync(c => c.Id == channelId.Value);
                if (channel == null)
                {
                    throw new ArgumentException("Channel not found.");
                }
            }

            // Create the new notification
            var newNotification = new Notification
            {
                ChannelId = channelId, // Nullable channelId
                Content = content,
                UserId = userId,
                Date = DateTime.UtcNow,
                IsNew = true,
                TypeId = typeId
            };

            _context.Notifications.Add(newNotification);
            await _context.SaveChangesAsync();

            if (targetTelegram)
            {
                // Retrieve the user with navigation properties loaded
                var user = await _context.Users
                                         .FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                {
                    throw new ArgumentException("User not found.");
                }

                // Prepare the Telegram notification using the channel information if available
                var telegramNotification = new TelegramNotification
                {
                    ChannelName = channel?.Name ?? "General Notification",
                    ChannelId = channelId,
                    UserId = userId,
                    TelegramUserId = user.TelegramId,
                    TelegramChatId = user.ChatId,
                    TelegamChannelId = channel?.TelegramId, // Nullable and directly retrieved
                    ContentType = contentType
                };

                // Send the Telegram notification
                await SendTelegramNotificationAsync(telegramNotification);
            }

            return newNotification;
        }

        public async Task<NotificationDelayedTask> CreateNotificationDelayedTaskAsync(
            string content,
            int typeId,
            int userId,
            DateTime scheduledTime,
            bool targetTelegram = false,
            string contentType = null,
            int? channelId = null)
        {

            // Validate the notification type
            var notificationType = await _context.NotificationTypes.FirstOrDefaultAsync(nt => nt.Id == typeId);
            if (notificationType == null)
            {
                throw new ArgumentException("Invalid notification type.");
            }

            Channel? channel = null;
            if (channelId.HasValue)
            {
                channel = await _context.Channels.FirstOrDefaultAsync(c => c.Id == channelId.Value);
                if (channel == null)
                {
                    throw new ArgumentException("Channel not found.");
                }
            }

            // Create the new delayed notification task
            var newDelayedTask = new NotificationDelayedTask
            {
                ChannelId = channelId,
                Content = content,
                UserId = userId,
                Date = scheduledTime,
                TypeId = typeId,
                ContentType = contentType ?? "default", // Assuming a default content type
                TargetTelegram = targetTelegram
            };

            _context.NotificationDelayedTasks.Add(newDelayedTask);
            await _context.SaveChangesAsync();

            // Schedule the notification
            await ScheduleNotificationAsync(
                content,
                typeId,
                userId,
                scheduledTime,
                newDelayedTask.Id,
                targetTelegram,
                contentType,
                channelId
                );

            return newDelayedTask;
        }

        public async Task LoadAndScheduleAllDelayedTasks()
        {
            var delayedTasks = await _context.NotificationDelayedTasks.ToListAsync();

            foreach (var task in delayedTasks)
            {
                try
                {
                    await ScheduleNotificationAsync(
                        task.Content,
                        task.TypeId,
                        task.UserId,
                        task.Date,
                        task.Id,
                        task.TargetTelegram,
                        task.ContentType,
                        task.ChannelId);

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load and schedule a delayed notification task. Task ID: {TaskId}", task.Id);
                }
            }
        }

        public async Task ScheduleNotificationAsync(
        string content,
        int typeId,
        int userId,
        DateTime scheduleAt,
        int notificationDelayedTaskId,
        bool targetTelegram = false,
        string contentType = null,
        int? channelId = null
        )
        {

            var jobData = new JobDataMap
            {
                {"content", content},
                {"typeId", typeId},
                {"userId", userId},
                {"targetTelegram", targetTelegram},
                {"contentType", contentType},
                {"channelId", channelId.HasValue ? channelId.Value : 0}, // 0 or some default to signify no channel
                {"notificationDelayedTaskId", notificationDelayedTaskId} // Ensure the value is passed here
            };

            IJobDetail job = JobBuilder.Create<NotificationJob>()
                .WithIdentity($"NotificationJob-{Guid.NewGuid()}", "NotificationGroup")
                .UsingJobData(jobData)
                .Build();

            ITrigger trigger = TriggerBuilder.Create()
                .WithIdentity($"NotificationTrigger-{Guid.NewGuid()}", "NotificationGroup")
                .StartAt(scheduleAt) // Use the specific DateTime when the notification should be sent
            .Build();

            await _scheduler.ScheduleJob(job, trigger);
        }

        public async Task DeleteNotificationDelayedTaskAsync(int taskId)
        {
            var task = await _context.NotificationDelayedTasks.FindAsync(taskId);
            if (task != null)
            {
                _context.NotificationDelayedTasks.Remove(task);
                await _context.SaveChangesAsync();
            }
        }

        public async Task SendTelegramNotificationAsync(TelegramNotification notificationModel)
        {
            await SendTelegramNotificationAsync(new List<TelegramNotification> { notificationModel });
        }

        public async Task SendTelegramNotificationAsync(List<TelegramNotification> notificationModels)
        {
            // Convert TelegramNotification models to TelegramNotificationPostModel
            var postModels = ConvertToPostModels(notificationModels);

            var httpClient = _clientFactory.CreateClient("MyClient");
            var jsonContent = JsonSerializer.Serialize(postModels);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

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
                    ContentType = model.ContentType
                };
                postModels.Add(postModel);
            }
            return postModels;
        }


    }
}
