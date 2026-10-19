using System.Globalization;
using Billing.Core.Enums;
using Billing.Core.Models;
using Billing.Core.Services;
using Billing.Infrastructure;

CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
var accountId = Guid.Parse("11111111-1111-1111-1111-111111111111");
var olderBill = new Bill(Guid.Parse("22222222-2222-2222-2222-222222222222"),
    "DEMO-001", new DateOnly(2026, 7, 31), 100m, 10m, 5m);
var newerBill = new Bill(Guid.Parse("33333333-3333-3333-3333-333333333333"),
    "DEMO-002", new DateOnly(2026, 8, 31), 80m, 5m, 2m);
var repository = new InMemoryBillingRepository(
    [new BillingAccount(accountId, AccountStatus.Open, 0, [newerBill, olderBill])]);
var service = new PaymentApplicationService(repository);
var request = new ApplyPaymentRequest(accountId, "payment-001", 150m,
    PaymentMethod.ElectronicTransfer, "operator");

Console.WriteLine("Billing payment demo)");
var result = await service.ApplyPaymentAsync(request);
if (!result.IsSuccess || result.Receipt is not { } receipt)
{
    Console.Error.WriteLine($"{result.Outcome}: {result.Error}");
    return 1;
}

Console.WriteLine($"Outcome: {result.Outcome}");



return 0;
