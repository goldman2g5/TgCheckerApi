using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace TgCheckerApi.Models.BaseModels;

public partial class Payment
{
    public Guid Id { get; set; }

    public string? Status { get; set; }

    public bool? Paid { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? CapturedAt { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public string? PaymentMethod { get; set; }

    public decimal? AmountValue { get; set; }

    public string? AmountCurrency { get; set; }

    public string? Description { get; set; }

    public bool? Capture { get; set; }

    public string? ClientIp { get; set; }

    public int UserId { get; set; }

    public int ChannelId { get; set; }

    public string? FullJson { get; set; }

    public string? CaptureJson { get; set; }

    public int SubtypeId { get; set; }

    public int Duration { get; set; }
    [JsonIgnore]
    public virtual Channel Channel { get; set; } = null!;
    [JsonIgnore]
    public virtual SubType Subtype { get; set; } = null!;
    [JsonIgnore]
    public virtual User User { get; set; } = null!;
}
