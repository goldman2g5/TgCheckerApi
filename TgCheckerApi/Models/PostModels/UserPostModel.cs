using Newtonsoft.Json;

namespace TgCheckerApi.Models
{
    public class UserPostModel : User
    {
        [System.Text.Json.Serialization.JsonIgnore]
        new public virtual ICollection<ChannelAccess> ChannelAccesses { get; set; } = new List<ChannelAccess>();
    }
}
