using Billing.Core.Enums;

namespace Billing.Core.Models;

/// <summary>An immutable bill snapshot. </summary>
public sealed record Bill
{
    public Bill(Guid billId, string billNumber, DateOnly dueDate,
        decimal principalBalance, decimal penaltyBalance, decimal interestBalance)
{
    if (billId == Guid.Empty)
    {
        throw new ArgumentException("A bill ID is required.", nameof(billId));
    }

    if (string.IsNullOrWhiteSpace(billNumber) || billNumber.Length > 32 ||
        billNumber != billNumber.Trim() || billNumber.Any(char.IsControl))
    {
        throw new ArgumentException("A trimmed bill number of 1-32 characters is required.", nameof(billNumber));
    }

    Money.ValidateBalance(principalBalance, nameof(principalBalance));
    Money.ValidateBalance(penaltyBalance, nameof(penaltyBalance));
    Money.ValidateBalance(interestBalance, nameof(interestBalance));

    BillId = billId;
    BillNumber = billNumber;
    DueDate = dueDate;
    PrincipalBalance = principalBalance;
    PenaltyBalance = penaltyBalance;
    InterestBalance = interestBalance;
}

public Guid BillId { get; }
public string BillNumber { get; }
public DateOnly DueDate { get; }
public decimal PrincipalBalance { get; }
public decimal PenaltyBalance { get; }
public decimal InterestBalance { get; }
public decimal TotalBalance => PrincipalBalance + PenaltyBalance + InterestBalance;
public BillStatus Status => TotalBalance == 0 ? BillStatus.Paid : BillStatus.Open;

public Bill WithBalances(decimal principal, decimal penalty, decimal interest) =>
    new(BillId, BillNumber, DueDate, principal, penalty, interest);
}
