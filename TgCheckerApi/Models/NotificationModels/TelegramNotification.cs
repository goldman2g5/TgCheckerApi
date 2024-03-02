using TgCheckerApi.Models.BaseModels;
using Newtonsoft.Json;

namespace TgCheckerApi.Models.NotificationModels
{
    public class TelegramNotification
    {
        [JsonIgnore]
        public ChannelAccess? ChannelAccess { get; set; }

        public string ChannelName { get; set; }

        public int? ChannelId { get; set; }

        public int UserId { get; set; }

        public long? TelegramUserId { get; set; }

        public long? TelegramChatId { get; set; }

        public long? TelegamChannelId { get; set; }

        public string ContentType { get; set; }
    }
}
