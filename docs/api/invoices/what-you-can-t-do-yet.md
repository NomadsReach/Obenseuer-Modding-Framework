---
title: What you can't do (yet)
parent: Invoices
grand_parent: Money and shops
nav_order: 7
---

# What you can't do (yet)

These are the things the invoice API does not offer, and why.

## Limits

- You can't pay an invoice for the player from your mod. Paying only works through the game's payment machine, which also takes the bill item out of the machine and gives the player the machine's output. Paying the invoice directly would skip both steps and leave the paid bill in the player's bag. <span class="tech">(`InvoicePaymentMachine.Select` calls `InvoiceController.PayInvoice` and then the base `PutItemSelection.Select`, which takes the item out of the slot (`inputitem.TakeItem()`) and adds the output item when the option has one.)</span>
- The machine's buttons are also built inside its own panel, so a mod can't press them. For that reason there is no `Invoices.Pay` and no list of payment machines.
- You can't send a fake invoice. A fake invoice does not count as a crime when it runs out, so allowing it would let a mod send payment requests that can never cause a crime. `Create` always sends a real one. <span class="tech">(`Create` never sets the game's `isfake` flag.)</span>
