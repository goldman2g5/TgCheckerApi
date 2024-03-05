using Quartz;
using TgCheckerApi.Services;

namespace TgCheckerApi.Job
{
    [DisallowConcurrentExecution]
    public class LoadDelayedNotificationsJob : IJob
    {
        private readonly NotificationService _notificationService;
        private readonly ILogger<LoadDelayedNotificationsJob> _logger;

        public LoadDelayedNotificationsJob(NotificationService notificationService, ILogger<LoadDelayedNotificationsJob> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("LoadDelayedNotificationsJob started at {Time}", DateTimeOffset.Now);

            try
            {
                // Logic to load and schedule NotificationDelayedTask instances
                // You might need a method in NotificationService to fetch and schedule all NotificationDelayedTasks
                await _notificationService.LoadAndScheduleAllDelayedTasks();

                _logger.LogInformation("All NotificationDelayedTasks are loaded and scheduled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading NotificationDelayedTasks.");
            }
        }
    }
}
