using System;
using TgCheckerApi.Models.BaseModels;
using TgCheckerApi.Services;
using TgCheckerApi.Models.BaseModels;
using TgCheckerApi.Models.NotificationModels;

public class NotificationTask : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public NotificationTask(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<TgDbContext>();
                var notificationService = new NotificationService(dbContext); // Create service instance here.

                var notifications = await notificationService.GetBumpNotifications();
                List<BumpNotification> notiList = notifications.ToList();
                Console.WriteLine($"\n\n\n\n\n\nNotification count: {notiList.Count}\n\n\n\n\n\n");
                // Your logic to fetch and send notifications goes here.
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // Run every minute (or adjust as needed).
        }
    }
}
