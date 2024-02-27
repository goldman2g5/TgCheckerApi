
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
                var notificationService = scope.ServiceProvider.GetRequiredService<NotificationService>();

                var notifications = await notificationService.GetBumpNotifications();
                List<TelegramNotification> notiList = notifications.ToList();
                Console.WriteLine($"\n\n\n\n\n\nNotification count: {notiList.Count}\n\n\n\n\n\n");

                foreach (var notification in notiList)
                {
                    string uniqueKey = notification.UniqueKey;
                    //ЗАХОДИТ СЮДА
                    Console.WriteLine(uniqueKey);
                    string message = "";

                    if (NotificationHub.UserMap.TryGetValue(uniqueKey, out string connectionId))
                    {
                        //ЗАХОДИТ СЮДА
                        Console.WriteLine($"sending to {connectionId}");
                        await _hubContext.Clients.All.SendToUserWithUniqueKey(uniqueKey, connectionId);
                        var clientProxy = _hubContext.Clients.Client(connectionId) as IClientProxy;
                        if (clientProxy != null)
                        {
                            //НЕ СЮДА
                            Console.WriteLine($"sending to {connectionId}");
                            await clientProxy.SendAsync("ReceiveNotification", message);
                        }
                    }
                }
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}