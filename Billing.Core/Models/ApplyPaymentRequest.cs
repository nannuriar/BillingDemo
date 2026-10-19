using Billing.Core.Enums;

namespace Billing.Core.Models;

/// <summary>Records an already received payment</summary>
public sealed record ApplyPaymentRequest(
    Guid AccountId,
    string IdempotencyKey,
    decimal Amount,
    PaymentMethod Method,
    string RecordedBy);
