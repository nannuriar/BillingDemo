using Billing.Core.Models;

namespace Billing.Core.Abstractions;


public interface IBillingRepository
{
    Task<BillingAccount?> GetAccountAsync(Guid accountId, CancellationToken cancellationToken = default);

    Task<PaymentReceipt?> GetPaymentAsync(Guid accountId, string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<bool> TryCommitPaymentAsync(PaymentCommit commit, CancellationToken cancellationToken = default);
}
