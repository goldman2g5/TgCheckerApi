﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace TgCheckerApi.Models.BaseModels;

public partial class Notification
{
    public int Id { get; set; }

    public int ChannelId { get; set; }

    public DateTime Date { get; set; }

    public string Content { get; set; } = null!;

    public bool IsNew { get; set; }

    public int TypeId { get; set; }
    [JsonIgnore]
    public virtual Channel Channel { get; set; } = null!;
    [JsonIgnore]
    public virtual NotificationType Type { get; set; } = null!;
}