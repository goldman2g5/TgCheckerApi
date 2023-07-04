using TgCheckerApi.Models.BaseModels;

namespace TgCheckerApi.Models.GetModels
{
    public class ChannelGetModel : Channel
    {
        public List<string> Tags { get; set; }
    }
}
