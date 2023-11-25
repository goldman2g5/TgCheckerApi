using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

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

    public string? Status { get; set; }

    public int? StaffId { get; set; }
    [JsonIgnore]
    public virtual Channel Channel { get; set; } = null!;
    [JsonIgnore]
    public virtual Staff? Staff { get; set; }
    [JsonIgnore]
    public virtual User User { get; set; } = null!;
}
