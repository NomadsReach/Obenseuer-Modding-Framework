---
title: Invoice senders in the level
parent: Invoices
grand_parent: Money and shops
nav_order: 6
---

# Invoice senders in the level

Some levels have an invoice sender: a trigger that sends a bill when the player does something. `FireSender` sets one off from your mod.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Invoices</code> <span>set off a level's invoice sender</span></p>

This is the class you start from. `FireSender` sets off the invoice sender with that save id in the level, and returns `true` when it adds a new invoice. The bill it sends is a normal one, listed by `Invoices.All()` on the [listing and sending bills](listing-and-sending-bills.md) page.

<div class="api-pair" markdown="1">

```csharp
public static class Invoices
{
    public static bool FireSender(string id);
}
```

| Method | Returns | What it does |
|---|---|---|
| `FireSender(id)` | `bool` | Sets off the invoice sender with this id. `true` when a new invoice was issued. |

</div>
</div>

## Things to know

- `FireSender(id)` runs an invoice sender that is placed in the scene. The `id` is the sender's save id, or its place in the scene when it has none. A sender that is switched off in the scene is not found. <span class="tech">(The sender is a `logic_invoice_sender`. The id is its save GUID or its scene path.)</span>
- The scene decides the bill. The level sets the name on the bill, the amount, the currency, the days until due and the mail delay. A sender that is turned off in the level sends nothing. <span class="tech">(It calls the sender's own `SendInvoice()`. The scene fields are `invoiceTitle`, `dueAmount`, the currency, `paymentTime` (in days) and `delay` (used when `useDelay` is set). A sender with `disabled` set sends nothing.)</span>
- `FireSender` returns `true` when a new invoice was issued. It returns `false` in the same cases as `Create`: the game is not ready, a load or save is running, or the mail controller does not exist. It also returns `false` for an empty id or one that no sender has.
- Each call sends one more invoice. The level's own trigger sets off the same sender, so firing it twice sends two bills.
- The bill is a normal invoice, not a fake one. If the player does not pay it before it runs out, the game counts it as a crime. `Issued` fires for it.

## Limits

- Rent collateral does not come from an invoice sender. The tenement code makes that bill itself, so you can't set it off with `FireSender`. <span class="tech">(`TenementController` calls the money overload of `CreateInvoice`. `logic_invoice_sender` is the only caller outside the game's own code that uses the item overload.)</span>
