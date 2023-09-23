using System;
using System.Collections.Generic;

namespace TgCheckerApi.Models.BaseModels;

public partial class Comment
{
    public int Id { get; set; }

    public string Content { get; set; } = null!;

    public int UserId { get; set; }

    public int ChannelId { get; set; }

    public int? ParentId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Channel Channel { get; set; } = null!;

    public virtual ICollection<Comment> InverseParent { get; set; } = new List<Comment>();

    public virtual Comment? Parent { get; set; }

    public virtual User User { get; set; } = null!;
}
