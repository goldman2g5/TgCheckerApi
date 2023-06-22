using Newtonsoft.Json;
using TgCheckerApi.Models.BaseModels;

namespace TgCheckerApi.Models
{
    public class UserPostModel : User
    {
        [System.Text.Json.Serialization.JsonIgnore]
        new public virtual ICollection<ChannelAccess> ChannelAccesses { get; set; } = new List<ChannelAccess>();
    }
}
