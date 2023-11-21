using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace TgCheckerApi.Models.BaseModels;

public partial class User
{
    public int Id { get; set; }

    public long? TelegramId { get; set; }

    public long? ChatId { get; set; }

    public string Username { get; set; } = null!;

    public byte[]? Avatar { get; set; }

    public string? UniqueKey { get; set; }
    [JsonIgnore]
    public virtual ICollection<ChannelAccess> ChannelAccesses { get; set; } = new List<ChannelAccess>();
    [JsonIgnore]
    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
    [JsonIgnore]
    public virtual ICollection<Report> Reports { get; set; } = new List<Report>();
    [JsonIgnore]
    public virtual ICollection<Staff> Staff { get; set; } = new List<Staff>();
}
