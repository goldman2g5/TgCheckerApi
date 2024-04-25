using Microsoft.EntityFrameworkCore;
using TgCheckerApi.Interfaces;
using TgCheckerApi.Models.BaseModels;

namespace TgCheckerApi.Services
{
    public class ElasticsearchIndexInitializer : IHostedService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public ElasticsearchIndexInitializer(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<TgDbContext>();
                var indexingService = scope.ServiceProvider.GetRequiredService<IElasticsearchIndexingService>();

                await indexingService.RecreateIndexAsync();

                var channels = await dbContext.Channels.ToListAsync(cancellationToken);
                if (channels.Any())
                {
                    await indexingService.IndexChannelsAsync(channels);
                }
                //await indexingService.InitializeIndicesAsync();



            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
