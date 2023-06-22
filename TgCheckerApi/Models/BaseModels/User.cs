using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace TgCheckerApi.Models.BaseModels;

public partial class User
{
    public int Id { get; set; }

    public long? TelegramId { get; set; }

    public long? ChatId { get; set; }
    
    [JsonIgnore]
    public virtual ICollection<ChannelAccess> ChannelAccesses { get; set; } = new List<ChannelAccess>();
}
