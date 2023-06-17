using Newtonsoft.Json;

namespace TgCheckerApi.Models
{
    public class ChannelPostModel : Channel
    {
        [System.Text.Json.Serialization.JsonIgnore]
        new public virtual ICollection<ChannelAccess> ChannelAccesses { get; set; } = new List<ChannelAccess>();
    }
}
