using Billing.Core.Enums;

namespace Billing.Core.Models;

public sealed record PaymentAllocation(Guid BillId, ChargeComponent Component, decimal Amount);
