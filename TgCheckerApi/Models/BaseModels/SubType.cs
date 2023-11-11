﻿using System;
using System.Collections.Generic;

namespace TgCheckerApi.Models.BaseModels;

public partial class SubType
{
    public int Id { get; set; }

    public int? Price { get; set; }

    public decimal? Multiplier { get; set; }

    public string? Name { get; set; }

    public virtual ICollection<ChannelHasSubscription> ChannelHasSubscriptions { get; set; } = new List<ChannelHasSubscription>();
}
