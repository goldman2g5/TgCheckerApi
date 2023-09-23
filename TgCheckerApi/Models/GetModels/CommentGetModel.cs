using TgCheckerApi.Models.BaseModels;

namespace TgCheckerApi.Models.GetModels
{
    public class CommentGetModel : Comment
    {
        [System.Text.Json.Serialization.JsonIgnore]
        new public virtual Channel? Channel { get; set; }

        public List<Comment> Replies { get; set; }

        public string Username { get; set; }
    }
}
