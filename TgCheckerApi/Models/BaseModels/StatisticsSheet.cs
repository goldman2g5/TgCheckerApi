using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace TgCheckerApi.Models.BaseModels;

public partial class StatisticsSheet
{
    public int Id { get; set; }

    public int ChannelId { get; set; }
    [JsonIgnore]
    public virtual Channel Channel { get; set; } = null!;
    [JsonIgnore]
    public virtual ICollection<ViewsRecord> ViewsRecords { get; set; } = new List<ViewsRecord>();
}
