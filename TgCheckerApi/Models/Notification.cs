

namespace TgCheckerApi.Models
{
    public class Notification
    {
        public ChannelAccess ChannelAccess { get; set; }

        public DateTime SendTime { get; set; }

        public int TelegramUserId { get; set; }

        public int TelegramChatId { get; set; }

    }
}
