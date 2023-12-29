using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace TgCheckerApi.Models.BaseModels;

public partial class Payment
{
    public int Id { get; set; }

    public int SubscriptionTypeId { get; set; }

    public int Duration { get; set; }

    public bool AutoRenewal { get; set; }

    public int Discount { get; set; }

    public int ChannelId { get; set; }

    public string ChannelName { get; set; } = null!;

    public int UserId { get; set; }

    public string Username { get; set; } = null!;

    public DateTime? Expires { get; set; }

    public string Status { get; set; } = null!;

    public int Price { get; set; }
    [JsonIgnore]
    public virtual Channel Channel { get; set; } = null!;
    [JsonIgnore]
    public virtual SubType SubscriptionType { get; set; } = null!;
    [JsonIgnore]
    public virtual User User { get; set; } = null!;
}
