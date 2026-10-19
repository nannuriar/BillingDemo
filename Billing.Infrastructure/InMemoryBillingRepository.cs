using Billing.Core.Abstractions;
using Billing.Core.Enums;
using Billing.Core.Models;

namespace Billing.Infrastructure;

public sealed class InMemoryBillingRepository : IBillingRepository
{
    private readonly object _gate = new();
    private readonly Dictionary<Guid, BillingAccount> _accounts = new();
    private readonly Dictionary<Guid, Dictionary<string, PaymentReceipt>> _payments = new();

    public InMemoryBillingRepository(IEnumerable<BillingAccount> accounts)
    {
        ArgumentNullException.ThrowIfNull(accounts);

        foreach (var account in accounts)
        {
            ArgumentNullException.ThrowIfNull(account);
            if (!_accounts.TryAdd(account.AccountId, account))
            {
                throw new ArgumentException("Accounts must have unique account IDs.", nameof(accounts));
            }

            _payments.Add(account.AccountId, new Dictionary<string, PaymentReceipt>(StringComparer.Ordinal));
        }
    }

    public Task<BillingAccount?> GetAccountAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _accounts.TryGetValue(accountId, out var account);
            return Task.FromResult<BillingAccount?>(account);
        }
    }

    public Task<PaymentReceipt?> GetPaymentAsync(
        Guid accountId,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);

        lock (_gate)
        {
            cancellationToken.ThrowIfCancellationRequested();
            PaymentReceipt? receipt = null;
            if (_payments.TryGetValue(accountId, out var accountPayments))
            {
                accountPayments.TryGetValue(idempotencyKey, out receipt);
            }

            return Task.FromResult(receipt);
        }
    }

    public Task<bool> TryCommitPaymentAsync(
        PaymentCommit commit,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(commit);
        ArgumentNullException.ThrowIfNull(commit.UpdatedAccount);
        ArgumentNullException.ThrowIfNull(commit.Receipt);

        lock (_gate)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var updatedAccount = commit.UpdatedAccount;
            if (!_accounts.TryGetValue(updatedAccount.AccountId, out var currentAccount)
                || currentAccount.Version != commit.ExpectedVersion)
            {
                return Task.FromResult(false);
            }

            var receipt = commit.Receipt;
            ArgumentException.ThrowIfNullOrWhiteSpace(receipt.IdempotencyKey);
            var accountPayments = _payments[updatedAccount.AccountId];
            if (accountPayments.ContainsKey(receipt.IdempotencyKey))
            {
                return Task.FromResult(false);
            }

            ValidateCommit(currentAccount, commit);

            // Observe cancellation again immediately before the single critical-section commit.
            cancellationToken.ThrowIfCancellationRequested();
            _accounts[updatedAccount.AccountId] = updatedAccount;
            accountPayments.Add(receipt.IdempotencyKey, receipt);
            return Task.FromResult(true);
        }
    }

    private static void ValidateCommit(BillingAccount current, PaymentCommit commit)
    {
        var updated = commit.UpdatedAccount;
        var receipt = commit.Receipt;

        if (commit.ExpectedVersion == long.MaxValue
            || updated.Version != commit.ExpectedVersion + 1
            || updated.Status != current.Status
            || receipt.AccountId != current.AccountId
            || receipt.PaymentId == Guid.Empty
            || receipt.Amount <= 0m
            || !IsWholeCents(receipt.Amount)
            || receipt.UnappliedAmount < 0m
            || !IsWholeCents(receipt.UnappliedAmount)
            || !Enum.IsDefined(receipt.Method)
            || string.IsNullOrWhiteSpace(receipt.RecordedBy)
            || receipt.Allocations.IsDefault
            || updated.Bills.Length != current.Bills.Length)
        {
            throw InvalidCommit();
        }

        var currentBills = current.Bills.ToDictionary(bill => bill.BillId);
        var allocated = new Dictionary<(Guid BillId, ChargeComponent Component), decimal>();
        var appliedAmount = 0m;

        foreach (var allocation in receipt.Allocations)
        {
            if (allocation is null
                || allocation.Amount <= 0m
                || !IsWholeCents(allocation.Amount)
                || !Enum.IsDefined(allocation.Component)
                || !currentBills.ContainsKey(allocation.BillId))
            {
                throw InvalidCommit();
            }

            var key = (allocation.BillId, allocation.Component);
            allocated.TryGetValue(key, out var priorAmount);
            allocated[key] = priorAmount + allocation.Amount;
            appliedAmount += allocation.Amount;
        }

        if (appliedAmount + receipt.UnappliedAmount != receipt.Amount)
        {
            throw InvalidCommit();
        }

        var seenBills = new HashSet<Guid>();
        foreach (var bill in updated.Bills)
        {
            if (!seenBills.Add(bill.BillId)
                || !currentBills.TryGetValue(bill.BillId, out var previous)
                || bill.BillNumber != previous.BillNumber
                || bill.DueDate != previous.DueDate
                || bill.PrincipalBalance < 0m
                || bill.PenaltyBalance < 0m
                || bill.InterestBalance < 0m
                || bill.PrincipalBalance != previous.PrincipalBalance - GetAllocated(bill.BillId, ChargeComponent.Principal)
                || bill.PenaltyBalance != previous.PenaltyBalance - GetAllocated(bill.BillId, ChargeComponent.Penalty)
                || bill.InterestBalance != previous.InterestBalance - GetAllocated(bill.BillId, ChargeComponent.Interest))
            {
                throw InvalidCommit();
            }
        }

        decimal GetAllocated(Guid billId, ChargeComponent component) =>
            allocated.TryGetValue((billId, component), out var amount) ? amount : 0m;
    }

    private static bool IsWholeCents(decimal amount) => amount == decimal.Round(amount, 2);

    private static ArgumentException InvalidCommit() =>
        new("The payment commit is inconsistent.", "commit");
}
