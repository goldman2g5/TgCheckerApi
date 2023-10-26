using TgCheckerApi.Models.BaseModels;
using Newtonsoft.Json;

namespace TgCheckerApi.Models.NotificationModels
{
    public class BumpNotification
    {
        [JsonIgnore]
        public ChannelAccess ChannelAccess { get; set; }

        public string ChannelName { get; set; }

        public int ChannelId { get; set; }

        public int TelegramUserId { get; set; }

        public int TelegramChatId { get; set; }

        public long TelegamChannelId { get; set; }

        public string UniqueKey { get; set; }
    }
}
