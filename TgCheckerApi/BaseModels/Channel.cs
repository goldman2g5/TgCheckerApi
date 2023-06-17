﻿using System;
using System.Collections.Generic;

namespace TgCheckerApi.BaseModels;

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

    public virtual ICollection<ChannelAccess> ChannelAccesses { get; set; } = new List<ChannelAccess>();
}