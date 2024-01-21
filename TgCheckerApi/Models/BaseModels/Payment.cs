using System;
using System.Collections.Generic;

namespace TgCheckerApi.Models.BaseModels;

public partial class Payment
{
    public Guid Id { get; set; }

    public string? Status { get; set; }

    public bool? Paid { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string? ReceiptRegistration { get; set; }

    public DateTime? CapturedAt { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public string? PaymentMethod { get; set; }

    public bool? Test { get; set; }

    public decimal? RefundedAmount { get; set; }

    public string? CancellationDetails { get; set; }

    public string? AuthorizationDetails { get; set; }

    public string? PayoutDestination { get; set; }

    public decimal? AmountValue { get; set; }

    public string? AmountCurrency { get; set; }

    public string? Description { get; set; }

    public bool? Capture { get; set; }

    public string? ConfirmationType { get; set; }

    public string? ConfirmationReturnUrl { get; set; }

    public string? ConfirmationConfirmationUrl { get; set; }

    public bool? ConfirmationEnforce { get; set; }

    public string? ConfirmationLocale { get; set; }

    public string? ConfirmationConfirmationToken { get; set; }

    public string? Metadata { get; set; }

    public string? Receipt { get; set; }

    public string? RecipientAccountId { get; set; }

    public string? RecipientGatewayId { get; set; }

    public string? PaymentToken { get; set; }

    public string? PaymentMethodId { get; set; }

    public string? PaymentMethodData { get; set; }

    public bool? SavePaymentMethod { get; set; }

    public string? ClientIp { get; set; }

    public string? Airline { get; set; }

    public string? Deal { get; set; }

    public string? MerchantCustomerId { get; set; }

    public int UserId { get; set; }

    public int ChannelId { get; set; }

    public virtual Channel Channel { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
