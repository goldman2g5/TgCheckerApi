using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace TgCheckerApi.Models.BaseModels;

public partial class Channel
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public string? Description { get; set; }

    public int? Members { get; set; }

    public byte[]? Avatar { get; set; }

    public int? User { get; set; }

    public bool? Notifications { get; set; }

    public int? Bumps { get; set; }

    public DateTime? LastBump { get; set; }

    public long? TelegramId { get; set; }

    public bool? NotificationSent { get; set; }

    public bool? PromoPost { get; set; }

    public TimeOnly? PromoPostTime { get; set; }

    public int? PromoPostInterval { get; set; }

    public bool? PromoPostSent { get; set; }

    public DateTime? PromoPostLast { get; set; }

    public string? Language { get; set; }

    public string? Flag { get; set; }

    [JsonIgnore]
    public virtual ICollection<ChannelAccess> ChannelAccesses { get; set; } = new List<ChannelAccess>();
    [JsonIgnore]
    public virtual ICollection<ChannelHasSubscription> ChannelHasSubscriptions { get; set; } = new List<ChannelHasSubscription>();
    [JsonIgnore]
    public virtual ICollection<ChannelHasTag> ChannelHasTags { get; set; } = new List<ChannelHasTag>();
}
