﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace TgCheckerApi.Models.BaseModels;

public partial class SubscribersRecord
{
    public int Id { get; set; }

    public int Subscribers { get; set; }

    public DateTime Date { get; set; }

    public int Sheet { get; set; }
    [JsonIgnore]
    public virtual StatisticsSheet SheetNavigation { get; set; } = null!;
}
