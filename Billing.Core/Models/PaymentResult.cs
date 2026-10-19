using Billing.Core.Enums;

namespace Billing.Core.Models;

public sealed record PaymentResult(
    PaymentOutcome Outcome,
    PaymentReceipt? Receipt = null,
    string? Error = null)
{
    public bool IsSuccess => Outcome is PaymentOutcome.Applied or PaymentOutcome.Replayed;
}
