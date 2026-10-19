# BillingDemo

A C# solution demonstrating how to record a payment already received and allocate it across a billing account's outstanding bills.

## Overview

The solution separates billing rules, data storage, and a console demonstration into three projects:

| Project | Responsibility |
| --- | --- |
| `Billing.Core` | Account and bill models, payment rules, result types, and the repository interface. |
| `Billing.Infrastructure` | An in-memory implementation of the repository interface. |
| `Billing.Demo` | A console program that creates sample bills and records one payment. |

Payments reduce existing interest, penalty, and principal balances. The application records electronic-transfer or check payments; it does not initiate a transfer or process a check. All account and payment data lives in memory and is lost when the process exits.

## Project structure


```text
Billing/
|-- .vs/                            
|-- src/
    |-- Billing.Core.sln              
    |-- Billing.Core/
    │   |-- Billing.Core.csproj
    │   |-- packages.lock.json
    │   |-- Abstractions/
    │   │   |-- IBillingRepository.cs
    │   |-- Enums/
    │   │   |-- AccountStatus.cs
    │   │   |-- BillStatus.cs
    │   │   |-- ChargeComponent.cs
    │   │   |-- PaymentMethod.cs
    │   │   |-- PaymentOutcome.cs
    │   |-- Models/
    │   │   |-- ApplyPaymentRequest.cs
    │   │   |-- Bill.cs
    │   │   |-- BillingAccount.cs
    │   │   |-- Money.cs
    │   │   |-- PaymentAllocation.cs
    │   │   |-- PaymentCommit.cs
    │   │   |-- PaymentReceipt.cs
    │   │   |-- PaymentResult.cs
    │   |-- Services/
    │       |-- PaymentApplicationService.cs
    |-- Billing.Infrastructure/
    │   |-- Billing.Infrastructure.csproj
    │   |-- packages.lock.json
    │   |-- InMemoryBillingRepository.cs
    |-- Billing.Demo/
        |-- Billing.Demo.csproj
        |-- packages.lock.json
        |-- Program.cs
```
