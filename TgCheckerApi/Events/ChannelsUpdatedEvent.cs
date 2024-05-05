using TgCheckerApi.Interfaces;
using TgCheckerApi.Models.BaseModels;

namespace TgCheckerApi.Events
{
    public class ChannelUpdateEvent : MediatR.INotification
    {
        public List<Channel> UpdatedChannels { get; }

        public ChannelUpdateEvent(List<Channel> updatedChannels)
        {
            UpdatedChannels = updatedChannels;
        }
    }

}
