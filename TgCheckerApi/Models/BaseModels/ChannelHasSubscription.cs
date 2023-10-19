using System;
using System.Collections.Generic;

namespace TgCheckerApi.Models.BaseModels;

public partial class ChannelHasSubscription
{
    public int Id { get; set; }

    public int? TypeId { get; set; }

    public DateTime? Expires { get; set; }

    public int UserId { get; set; }

    public virtual SubType? Type { get; set; }

    public virtual User User { get; set; } = null!;
}
