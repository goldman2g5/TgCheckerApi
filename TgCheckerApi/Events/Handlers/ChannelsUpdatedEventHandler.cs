using MediatR;
using TgCheckerApi.Events;
using TgCheckerApi.Interfaces;

public class ChannelUpdateEventHandler : INotificationHandler<ChannelUpdateEvent>
{
    private readonly IServiceScopeFactory _scopeFactory;

    public ChannelUpdateEventHandler(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task Handle(ChannelUpdateEvent notification, CancellationToken cancellationToken)
    {
        using (var scope = _scopeFactory.CreateScope())
        {
            var indexingService = scope.ServiceProvider.GetRequiredService<IElasticsearchIndexingService>();
            try
            {
                await indexingService.IndexChannelsAsync(notification.UpdatedChannels);
            }
            catch (Exception ex)
            {
                // Implement error handling (e.g., retry logic, logging)
                Console.WriteLine($"Error indexing channels: {ex.Message}");
            }
        }
    }
}
