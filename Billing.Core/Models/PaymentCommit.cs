namespace Billing.Core.Models;

/// <summary>Balances and the receipt form one operation.</summary>
public sealed record PaymentCommit(
    BillingAccount UpdatedAccount,
    long ExpectedVersion,
    PaymentReceipt Receipt);
