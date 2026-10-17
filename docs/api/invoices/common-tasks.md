---
title: Common tasks
parent: Invoices
grand_parent: Money and shops
nav_order: 1
---

# Common tasks

Send the player a bill for 50 OC that is due in seven days. `Create` returns `true` when the invoice was issued.

```csharp
bool sent = Invoices.Create("Example Repairs", MoneyCurrency.OC, 50, 7);
```

Check whether the player can pay a bill right now. `CanPay` does not take any money.

```csharp
foreach (InvoiceInfo invoice in Invoices.All())
    Log.Info(invoice.Debtor + ": can pay " + Invoices.CanPay(invoice.Id));
```

Cancel a bill. `Cancel` returns `true` when the bill is gone.

```csharp
InvoiceInfo invoice = Invoices.Get(1);
if (invoice != null && Invoices.Cancel(invoice.Id)) Log.Info("Cancelled " + invoice.Debtor);
```
