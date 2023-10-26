using System;
using System.Collections.Generic;

namespace TgCheckerApi.Models.BaseModels;

public partial class ChannelBumpDetail
{
    public int Id { get; set; }

    public int ChannelId { get; set; }

    public int Bumps { get; set; }

    public bool? NotificationSent { get; set; }

    public DateTime? LastBump { get; set; }

    public virtual Channel Channel { get; set; } = null!;
}
