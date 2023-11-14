using Newtonsoft.Json;
using TgCheckerApi.Models.BaseModels;

namespace TgCheckerApi.Models.GetModels
{
    public class ReportGetModel : Report
    {
        public string ChannelName { get; set; }

        public string ChannelUrl { get; set; }

        public string ChannelWebUrl { get; set; }

        public string ReporteeName { get; set; }

        public string UserTelegramChatId { get; set; }
    }
}
