using Quartz;
using TgCheckerApi.Services;

namespace TgCheckerApi.Job
{
    public class NotificationJob : IJob
    {
        private readonly NotificationService _notificationService;
        private readonly ILogger<NotificationJob> _logger;

        public NotificationJob(NotificationService notificationService, ILogger<NotificationJob> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("NotificationJob started at {Time}", DateTimeOffset.Now);

            JobDataMap dataMap = context.MergedJobDataMap;

            // Extract parameters from JobDataMap
            string content = dataMap.GetString("content");
            int typeId = dataMap.GetInt("typeId");
            int userId = dataMap.GetInt("userId");
            int? channelId = dataMap.ContainsKey("channelId") ? (int?)dataMap.GetInt("channelId") : null;
            bool targetTelegram = dataMap.GetBoolean("targetTelegram");
            string contentType = dataMap.GetString("contentType");
            // Retrieve the ID of the NotificationDelayedTask
            int notificationDelayedTaskId = dataMap.GetInt("notificationDelayedTaskId");

            try
            {
                // Execute notification creation logic with the extracted parameters
                await _notificationService.CreateNotificationAsync(
                    content,
                    typeId,
                    userId,
                    channelId,
                    targetTelegram,
                    contentType);

                // Here, use the NotificationService to delete the NotificationDelayedTask by its ID
                await _notificationService.DeleteNotificationDelayedTaskAsync(notificationDelayedTaskId);

                _logger.LogInformation("NotificationJob executed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred executing NotificationJob.");
            }
        }
    }
}
