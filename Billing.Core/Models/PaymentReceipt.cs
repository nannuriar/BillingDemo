using System.Collections.Immutable;
using Billing.Core.Enums;

namespace Billing.Core.Models;

/// <summary>The record of the payment.</summary>
public sealed record PaymentReceipt(
    Guid PaymentId,
    Guid AccountId,
    string IdempotencyKey,
    decimal Amount,
    PaymentMethod Method,
    string RecordedBy,
    DateTimeOffset RecordedAtUtc,
    ImmutableArray<PaymentAllocation> Allocations,
    decimal UnappliedAmount)
{
    public decimal AppliedAmount => Amount - UnappliedAmount;
}
