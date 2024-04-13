using TgCheckerApi.Interfaces;
using TgCheckerApi.Models.BaseModels;

namespace TgCheckerApi.Events
{
    public class ChannelsUpdatedEvent : IDomainEvent
    {
        public List<Channel> UpdatedChannels { get; }

        public ChannelsUpdatedEvent(List<Channel> updatedChannels)
        {
            UpdatedChannels = updatedChannels;
        }
    }

}
