---
title: How invoices are paid and saved
parent: Invoices
grand_parent: Money and shops
nav_order: 5
---

# How invoices are paid and saved

- **The player pays an invoice by putting the bill into a payment machine.** The game has no other way to pay one: no bank page, computer or phone pays an invoice. The game checks the money, takes it from the player's wallet, runs the invoice's follow-up action, and removes the invoice from the open list.
  - Technical: the only path is `InvoicePaymentMachine.Select`, which calls `InvoiceController.PayInvoice`.
- **The open invoices are saved with the rest of the game.** The bill item in the player's mail or bag is a separate thing that carries the same data. Cancelling or paying an invoice does not remove that item from the mailbox or inventory.
  - Technical: `InvoiceController` is a `SavableScript`, and its public `invoices` list is saved with the global save. The item is a mail item that carries the same `ItemInvoiceData` object.
