using System;
using System.Collections.Generic;

namespace TgCheckerApi.Models.BaseModels;

public partial class StatisticsSheet
{
    public int Id { get; set; }

    public int ChannelId { get; set; }

    public virtual Channel Channel { get; set; } = null!;

    public virtual ICollection<ViewsRecord> ViewsRecords { get; set; } = new List<ViewsRecord>();
}
