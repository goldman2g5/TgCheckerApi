using TgCheckerApi.Models.BaseModels;
using Newtonsoft.Json;

namespace TgCheckerApi.Models
{
    public class CommentPostModel : Comment
    {
        [System.Text.Json.Serialization.JsonIgnore]
        new public virtual Channel? Channel { get; set; } = null!;
        [System.Text.Json.Serialization.JsonIgnore]
        new public virtual ICollection<Comment>? InverseParent { get; set; } = new List<Comment>();
        [System.Text.Json.Serialization.JsonIgnore]
        new public virtual Comment? Parent { get; set; }
        [System.Text.Json.Serialization.JsonIgnore]
        new public virtual User? User { get; set; } = null!;


    }
}
