using System.Collections.Immutable;
using Billing.Core.Abstractions;
using Billing.Core.Enums;
using Billing.Core.Models;

namespace Billing.Core.Services;


public sealed class PaymentApplicationService
{
    private const int MaxCommitAttempts = 3;
    public const decimal MaximumPaymentAmount = 1_000_000m;
    private readonly IBillingRepository _repository;
    private readonly TimeProvider _timeProvider;

    public PaymentApplicationService(IBillingRepository repository, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<PaymentResult> ApplyPaymentAsync(ApplyPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var validationError = Validate(request);
        if (validationError is not null)
        {
            return new(PaymentOutcome.ValidationFailed, Error: validationError);
        }

        for (var attempt = 0; attempt < MaxCommitAttempts; attempt++)
        {
            // Check replay before current account status: an earlier receipt remains valid.
            var existing = await _repository.GetPaymentAsync(
                request.AccountId, request.IdempotencyKey, cancellationToken);
            if (existing is not null)
            {
                return ResolveReplay(request, existing);
            }

            var account = await _repository.GetAccountAsync(request.AccountId, cancellationToken);
            if (account is null)
            {
                return new(PaymentOutcome.AccountNotFound, Error: "The billing account not found.");
            }

            if (account.Status != AccountStatus.Open)
            {
                return new(PaymentOutcome.AccountClosed, Error: "Account closed.");
            }

            var remaining = request.Amount;
            var allocations = new List<PaymentAllocation>();
            var updatedBills = new List<Bill>(account.Bills.Length);

            // Policy is bill-first, then component-first within each bill. Future bills are eligible.
            foreach (var bill in account.Bills.OrderBy(bill => bill.DueDate)
                         .ThenBy(bill => bill.BillNumber, StringComparer.Ordinal)
                         .ThenBy(bill => bill.BillId))
            {
                var interest = Allocate(bill.BillId, ChargeComponent.Interest,
                    bill.InterestBalance, ref remaining, allocations);
                var penalty = Allocate(bill.BillId, ChargeComponent.Penalty,
                    bill.PenaltyBalance, ref remaining, allocations);
                var principal = Allocate(bill.BillId, ChargeComponent.Principal,
                    bill.PrincipalBalance, ref remaining, allocations);
                updatedBills.Add(bill.WithBalances(principal, penalty, interest));
            }

            var receipt = new PaymentReceipt(Guid.NewGuid(), account.AccountId,
                request.IdempotencyKey, request.Amount, request.Method, request.RecordedBy,
                _timeProvider.GetUtcNow(), allocations.ToImmutableArray(), remaining);
            var commit = new PaymentCommit(account.WithBills(updatedBills), account.Version, receipt);

            cancellationToken.ThrowIfCancellationRequested();
            if (await _repository.TryCommitPaymentAsync(commit, cancellationToken))
            {
                return new(PaymentOutcome.Applied, receipt);
            }

            // A competitor changed the snapshot or used this key. Reload and recalculate.
        }

        // The final failed commit may have been an identical competing request.
        var winner = await _repository.GetPaymentAsync(
            request.AccountId, request.IdempotencyKey, cancellationToken);
        return winner is not null
            ? ResolveReplay(request, winner)
            : new(PaymentOutcome.ConcurrencyConflict,
                Error: "The account changed repeatedly");
    }

    private static decimal Allocate(Guid billId, ChargeComponent component, decimal balance,
        ref decimal remaining, List<PaymentAllocation> allocations)
    {
        var applied = Math.Min(balance, remaining);
        if (applied > 0)
        {
            allocations.Add(new(billId, component, applied));
            remaining -= applied;
        }

        return balance - applied;
    }

    private static PaymentResult ResolveReplay(ApplyPaymentRequest request, PaymentReceipt receipt)
    {
        var matches = receipt.AccountId == request.AccountId &&
                      string.Equals(receipt.IdempotencyKey, request.IdempotencyKey, StringComparison.Ordinal) &&
                      receipt.Amount == request.Amount && receipt.Method == request.Method &&
                      string.Equals(receipt.RecordedBy, request.RecordedBy, StringComparison.Ordinal);

        return matches
            ? new(PaymentOutcome.Replayed, receipt)
            : new(PaymentOutcome.IdempotencyConflict,
                Error: "This idempotency key was already used with different payment details.");
    }

    private static string? Validate(ApplyPaymentRequest request)
    {
        if (request.AccountId == Guid.Empty)
        {
            return "An account ID is required.";
        }

        if (request.Amount <= 0 || request.Amount > MaximumPaymentAmount || !Money.IsWholeCents(request.Amount))
        {
            return "Payment amount must be greater than zero.";
        }

        if (!Enum.IsDefined(request.Method))
        {
            return "A supported payment method is required.";
        }

        if (!IsValidText(request.IdempotencyKey, 64))
        {
            return "An idempotency key of 1-64 characters is required.";
        }

        return !IsValidText(request.RecordedBy, 100)
            ? "A recorded-by identifier of 1-100 characters with no whitespace or control characters."
            : null;
    }

    private static bool IsValidText(string? value, int maxLength) =>
        !string.IsNullOrWhiteSpace(value) && value.Length <= maxLength &&
        value == value.Trim() && !value.Any(char.IsControl);
}
