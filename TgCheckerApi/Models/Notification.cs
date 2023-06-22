using TgCheckerApi.Models.BaseModels;
using Newtonsoft.Json;

namespace TgCheckerApi.Models
{
    public class Notification
    {
        [JsonIgnore]
        public ChannelAccess ChannelAccess { get; set; }

        public string ChannelName { get; set; }

        public int ChannelId { get; set; }

        public DateTime SendTime { get; set; }

        public int TelegramUserId { get; set; }
        
        
        public int TelegramChatId { get; set; }

        public long TelegamChannelId { get; set; }

    }
}
