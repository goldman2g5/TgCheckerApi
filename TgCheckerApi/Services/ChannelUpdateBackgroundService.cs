using TgCheckerApi.Events;
using TgCheckerApi.Interfaces;
using TgCheckerApi.Models.BaseModels;

namespace TgCheckerApi.Services
{
    public class ChannelUpdateBackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ChannelUpdateBackgroundService> _logger;

        private readonly Timer _timer;
        private readonly Queue<ChannelUpdateEvent> _eventQueue;
        private const int BatchSize = 1; // Adjustable batch size for indexing

        public ChannelUpdateBackgroundService(IServiceScopeFactory scopeFactory,
                                               ILogger<ChannelUpdateBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _eventQueue = new Queue<ChannelUpdateEvent>();
            _timer = new Timer(ProcessEvents, null, TimeSpan.Zero, TimeSpan.FromSeconds(5)); // Adjustable timer interval (optional)
        }


        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("ChannelUpdateBackgroundService starting...");
            // Register for domain events on DbContext
            await Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("ChannelUpdateBackgroundService stopping...");
            _timer.Dispose();
            ProcessEvents(null); // Process any remaining events before stopping
            await Task.CompletedTask;
        }

        public async Task OnChannelsSaved(List<Channel> updatedChannels)
        {
            // Add updated channels to event queue asynchronously
            await Task.Run(() => _eventQueue.Enqueue(new ChannelUpdateEvent(updatedChannels)));
            Console.WriteLine($"ПАСТЕРНАК {_eventQueue.Count}");
        }

        private async void ProcessEvents(object state)
        {
            Console.WriteLine($"ГОЙДА {_eventQueue.Count}");
            if (_eventQueue.Count < BatchSize)
            {
                return; // Not enough events to trigger batch processing
            }

            var batch = new List<Channel>();
            while (_eventQueue.Count > 0 && batch.Count < BatchSize)
            {
                batch.AddRange(_eventQueue.Dequeue().UpdatedChannels);
            }

            using (var scope = _scopeFactory.CreateScope())
            {
                var indexingService = scope.ServiceProvider.GetRequiredService<IElasticsearchIndexingService>();
                try
                {
                    Console.WriteLine("МЕДВЕДЬ\nМЕДВЕДЬ\nМЕДВЕДЬ\nМЕДВЕДЬ\nМЕДВЕДЬ\nМЕДВЕДЬ\n");
                    await indexingService.IndexChannelsAsync(batch);
                    _logger.LogError("re-indexing channels");
                }
                catch (Exception ex)
                {
                    _logger.LogError("Error indexing channels: {Message}", ex.Message);
                    // Implement error handling (e.g., retry logic, logging)
                }
            }
        }
    }
}
