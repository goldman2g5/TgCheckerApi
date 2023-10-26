
using Microsoft.AspNetCore.SignalR;
using TgCheckerApi.Interfaces;
using TgCheckerApi.Models.BaseModels;
using TgCheckerApi.Models.NotificationModels;
using TgCheckerApi.Services;
using TgCheckerApi.Websockets;

public class NotificationTask : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<NotificationHub, INotificationHub> _hubContext;

    public NotificationTask(IServiceScopeFactory scopeFactory, IHubContext<NotificationHub, INotificationHub> hubContext)
    {
        _scopeFactory = scopeFactory;
        _hubContext = hubContext;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<TgDbContext>();
                var notificationService = new NotificationService(dbContext);

                var notifications = await notificationService.GetBumpNotifications();
                List<BumpNotification> notiList = notifications.ToList();
                Console.WriteLine($"\n\n\n\n\n\nNotification count: {notiList.Count}\n\n\n\n\n\n");

                // Send notifications to users
                foreach (var notification in notiList)
                {
                    string uniqueKey = notification.UniqueKey;
                    string message = "Your custom notification message"; // Define your message

                    // Send message to the specific user
                    if (NotificationHub.UserMap.TryGetValue(uniqueKey, out string connectionId))
                    {
                        var clientProxy = _hubContext.Clients.Client(connectionId) as IClientProxy;
                        if (clientProxy != null)
                        {
                            await clientProxy.SendAsync("ReceiveNotification", message);
                        }
                    }
                }
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // Run every minute (or adjust as needed).
        }
    }
}