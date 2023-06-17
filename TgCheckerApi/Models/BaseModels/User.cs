using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace TgCheckerApi.Models;

public partial class User
{
    public int Id { get; set; }

    public long? TelegramId { get; set; }

    public long? ChatId { get; set; }
    [JsonIgnore]
    public virtual ICollection<ChannelAccess> ChannelAccesses { get; set; } = new List<ChannelAccess>();
}
