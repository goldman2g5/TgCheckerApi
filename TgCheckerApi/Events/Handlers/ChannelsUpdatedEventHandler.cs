using TgCheckerApi.Interfaces;

namespace TgCheckerApi.Events.Handlers
{
    public class ChannelsUpdatedEventHandler : IDomainEventHandler<ChannelsUpdatedEvent>
    {
        private readonly IElasticsearchIndexingService _indexingService;

        public ChannelsUpdatedEventHandler(IElasticsearchIndexingService indexingService)
        {
            _indexingService = indexingService;
        }

        public async Task Handle(ChannelsUpdatedEvent domainEvent)
        {
            await _indexingService.IndexChannelsAsync(domainEvent.UpdatedChannels);
        }
    }
}
