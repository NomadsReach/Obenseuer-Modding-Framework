---
title: The name on the bill
parent: Invoices
grand_parent: Money and shops
nav_order: 3
---

# The name on the bill

`InvoiceInfo.Debtor` is the name printed on the bill, and it names the party that is charging the player. It is not the person who owes the money, because the player is always the one who owes: invoices are mailed to the player, and the player pays them. The bill reads "Invoice: " followed by this name.

## Things to know

- Examples from the game are a legal service fee (named by the legal action), a bank down payment (`<bank>: Debt Downpayment`), rent collateral (`Rent collateral: <resident name>`), a subscription (named by the subscription) and a fake invoice from a random name.
- Name the party that is charging when you call `Create`.
- The name is not checked, beyond not being empty. It is shown to the player and it is saved in the save file. <span class="tech">(`Debtor` is `ItemInvoiceData.debtor`.)</span>
