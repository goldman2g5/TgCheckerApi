using TgCheckerApi.Models.BaseModels;

namespace TgCheckerApi.Models.GetModels
{
    public class ChannelGetModel : Channel
    {
        public List<string> Tags { get; set; }

        public string urlCut { get; set; }

        public int? subType { get; set; }

        public DateTime? SubscriptionExpirationDate { get; set; }
    }
}
