﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace TgCheckerApi.Models.BaseModels;

public partial class MonthViewsRecord
{
    public int Id { get; set; }

    public int Views { get; set; }

    public DateTime Date { get; set; }

    public DateTime Updated { get; set; }

    public int Sheet { get; set; }

    public long? LastMessageId { get; set; }
    [JsonIgnore]
    public virtual StatisticsSheet SheetNavigation { get; set; } = null!;
}
