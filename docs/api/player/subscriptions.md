---
title: Subscriptions
parent: Player
grand_parent: Player life and skills
nav_order: 7
---

# Subscriptions

A subscription is a regular service the player signs up for and pays for, for example a phone plan. Some are billed by invoice and some are taken from the bank.

```csharp
public static class Subscriptions
{
    public static bool IsActive(string id);       // true when the player has this subscription
    public static List<string> ActiveIds();       // the ids of the subscriptions the player has now
    public static bool Subscribe(string id);      // signs the player up; true when this call did it
    public static bool Unsubscribe(string id);    // cancels the subscription; true when the player no longer has it
}
```

`Subscribe` and `Unsubscribe` do what the game does when the player signs up or cancels, so both show the game's notification.

- **`Subscribe(id)` returns `true` only when this call signed the player up.** It returns `false` when the player already has the subscription, so the game does not send its invoice twice. It also returns `false` for an id the game does not know, while a save is busy, or for a phone plan.
- **Billing starts at once if the subscription costs something.** `Subscribe` sends an invoice for the cost straight away, with the subscription's own payment time. Later charges follow the subscription's own interval: an invoice, or a bank withdrawal for a bank subscription.
  - Technical: the payment time is `paymentTime` of `PlayerSubscription`.
- **`Unsubscribe(id)` returns `true` when the player no longer has the subscription, and it charges nothing.** The framework checks first that the player has it, because the game would show the cancel notification even for one the player does not hold.
  - Technical: the game's `Unsubscribe()` only notifies and removes the entry.
- **A subscription is only found if the game has loaded it.** The framework looks for subscriptions that are loaded in the game's memory, and the game does not say where they are stored. So a subscription that is not loaded is not found, and `Subscribe` returns `false` for it.
  - Technical: it searches the `PlayerSubscription` assets with `Resources.FindObjectsOfTypeAll`.
- **Phone plans can't be subscribed or cancelled from here.** Cancelling a phone plan clears the plan list but not the phones' own plan and device number, and the game clears those only for phones in the player's inventory. A plain call would leave the other phones out of step with the plan.
  - Technical: the fields are the device's `currentplan` and `deviceID`.
