namespace Billing.Core.Enums;

public enum PaymentOutcome
{
    Applied = 1,
    Replayed = 2,
    ValidationFailed = 3,
    AccountNotFound = 4,
    AccountClosed = 5,
    IdempotencyConflict = 6,
    ConcurrencyConflict = 7
}
