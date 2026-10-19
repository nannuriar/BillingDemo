using System.Collections.Immutable;
using Billing.Core.Enums;

namespace Billing.Core.Models;

public sealed record BillingAccount
{
    public BillingAccount(Guid accountId, AccountStatus status, long version, IEnumerable<Bill> bills)
    {
        if (accountId == Guid.Empty)
        {
            throw new ArgumentException("An account ID is required.", nameof(accountId));
        }

        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status));
        }

        ArgumentOutOfRangeException.ThrowIfNegative(version);
        ArgumentNullException.ThrowIfNull(bills);
        var snapshot = bills.ToImmutableArray();

        if (snapshot.Any(bill => bill is null) ||
            snapshot.Select(bill => bill.BillId).Distinct().Count() != snapshot.Length ||
            snapshot.Select(bill => bill.BillNumber).Distinct(StringComparer.Ordinal).Count() != snapshot.Length)
        {
            throw new ArgumentException("Bills must be non-null with unique IDs and numbers per account.", nameof(bills));
        }

        AccountId = accountId;
        Status = status;
        Version = version;
        Bills = snapshot;
    }

    public Guid AccountId { get; }
    public AccountStatus Status { get; }
    public long Version { get; }
    public ImmutableArray<Bill> Bills { get; }

    public BillingAccount WithBills(IEnumerable<Bill> bills) =>
        new(AccountId, Status, checked(Version + 1), bills);
}
