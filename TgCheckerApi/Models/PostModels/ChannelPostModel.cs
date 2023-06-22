using Newtonsoft.Json;
using TgCheckerApi.Models.BaseModels;

namespace TgCheckerApi.Models
{
    public class ChannelPostModel : Channel
    {
        [System.Text.Json.Serialization.JsonIgnore]
        new public virtual ICollection<ChannelAccess> ChannelAccesses { get; set; } = new List<ChannelAccess>();
    }
}
