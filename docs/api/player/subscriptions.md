---
title: Subscriptions
parent: Player
grand_parent: Player life and skills
nav_order: 7
---

# Subscriptions

A subscription is a regular service the player signs up for and pays for, for example a phone plan. Some are billed by invoice and some are taken from the bank.

`Subscribe` and `Unsubscribe` do what the game does when the player signs up or cancels, so both show the game's notification.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Subscriptions</code> <span>sign up, cancel and check</span></p>

Use `Subscribe` and `Unsubscribe` to sign the player up or cancel, and `IsActive` or `ActiveIds` to check what they have.

<div class="api-pair" markdown="1">

```csharp
public static class Subscriptions
{
    public static bool IsActive(string id);
    public static List<string> ActiveIds();
    public static bool Subscribe(string id);
    public static bool Unsubscribe(string id);
}
```

| Method | Returns | What it does |
|---|---|---|
| `IsActive(id)` | `bool` | `true` when the player has this subscription. |
| `ActiveIds()` | `List<string>` | The ids of the subscriptions the player has now. |
| `Subscribe(id)` | `bool` | Signs the player up. Returns `true` when this call did it. |
| `Unsubscribe(id)` | `bool` | Cancels the subscription. Returns `true` when the player no longer has it. |

</div>
</div>

## Things to know

- `Subscribe(id)` returns `true` only when this call signed the player up. It returns `false` when the player already has the subscription, so the game does not send its invoice twice. It also returns `false` for an id the game does not know, while a save is busy, or for a phone plan.
- Billing starts at once if the subscription costs something. `Subscribe` sends an invoice for the cost straight away, with the subscription's own payment time. Later charges follow the subscription's own interval: an invoice, or a bank withdrawal for a bank subscription. <span class="tech">(The payment time is `paymentTime` of `PlayerSubscription`.)</span>
- `Unsubscribe(id)` returns `true` when the player no longer has the subscription, and it charges nothing. The framework checks first that the player has it, because the game would show the cancel notification even for one the player does not hold. <span class="tech">(The game's `Unsubscribe()` only notifies and removes the entry.)</span>

## Limits

- A subscription is only found if the game has loaded it. The framework looks for subscriptions that are loaded in the game's memory, and the game does not say where they are stored. So a subscription that is not loaded is not found, and `Subscribe` returns `false` for it. <span class="tech">(It searches the `PlayerSubscription` assets with `Resources.FindObjectsOfTypeAll`.)</span>
- Phone plans can't be subscribed or cancelled from here. Cancelling a phone plan clears the plan list but not the phones' own plan and device number, and the game clears those only for phones in the player's inventory. A plain call would leave the other phones out of step with the plan. <span class="tech">(The fields are the device's `currentplan` and `deviceID`.)</span>
