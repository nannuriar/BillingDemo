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
