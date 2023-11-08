using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace TgCheckerApi.Models.BaseModels;

public partial class Report
{
    public int Id { get; set; }

    public int ChannelId { get; set; }

    public int UserId { get; set; }

    public DateTime? ReportTime { get; set; }

    public string? Text { get; set; }

    public string? Reason { get; set; }

    public bool? NotificationSent { get; set; }
    [JsonIgnore]
    public virtual Channel Channel { get; set; } = null!;
    [JsonIgnore]
    public virtual User User { get; set; } = null!;
}
