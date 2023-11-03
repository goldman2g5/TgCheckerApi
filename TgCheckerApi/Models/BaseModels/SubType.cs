using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TgCheckerApi.Models.BaseModels;

public partial class SubType
{
    public int Id { get; set; }

    public int? Price { get; set; }

    public decimal? Multiplier { get; set; }

    public string? Name { get; set; }
    [JsonIgnore]
    public virtual ICollection<ChannelHasSubscription> ChannelHasSubscriptions { get; set; } = new List<ChannelHasSubscription>();
}
