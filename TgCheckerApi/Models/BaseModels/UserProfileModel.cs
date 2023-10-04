using System.Net;
using TgCheckerApi.Models.GetModels;

namespace TgCheckerApi.Models.BaseModels
{
    public class UserProfileModel
    {
        public List<ChannelGetModel> Channels { get; set; }

        public IEnumerable<CommentGetModel> Comments { get; set; }

        public string SubType { get; set; }

        public string SubExpires { get; set; }
    }
}
