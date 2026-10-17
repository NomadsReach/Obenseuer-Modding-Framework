---
title: Mail
parent: Economy
grand_parent: Money and shops
nav_order: 8
---

# Mail

`Mail` reads the player's mailbox and parcel locker, sends letters and packages to the player, and tells you when mail arrives. A letter goes to the mailbox and a package goes to the parcel locker.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>MailKind</code> <span>where a piece of mail goes</span></p>
<div class="api-pair" markdown="1">

```csharp
public enum MailKind
{
    Letter = 0,
    Package = 1
}
```

| Value | Meaning |
|---|---|
| `Letter` | A letter, delivered to the mailbox. |
| `Package` | A package, delivered to the parcel locker. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>MailInfo</code> <span>one piece of mail waiting in the mailbox or locker</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class MailInfo
{
    public string Name { get; }
    public MailKind Kind { get; }
    public int ItemId { get; }
    public int Count { get; }
    public int Oc { get; }
    public float Rm { get; }
    public bool IsNote { get; }
    public string Body { get; }
    public string Sender { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Name` | `string` | The name the game shows for the mail, used as the subject. |
| `Kind` | `MailKind` | Letter or package. |
| `ItemId` | `int` | The item inside. `0` when the mail holds no item. |
| `Count` | `int` | How many of the item. |
| `Oc` | `int` | The OC inside. `0` when there is none. |
| `Rm` | `float` | The RM inside. |
| `IsNote` | `bool` | `true` when the letter carries a note (text). |
| `Body` | `string` | The note's text. `""` when it is not a note. |
| `Sender` | `string` | Who an email note is from. `""` when there is none. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Mail</code> <span>read the mailbox, send mail, and hear when it arrives</span></p>
<div class="api-pair" markdown="1">

```csharp
public static class Mail
{
    public static event Action<MailReceivedArgs> Received;
    public static event Action<MailReceivedArgs> PackageReceived;
    public static event Action<MailSentArgs> Sent;
    public static bool IsAvailable { get; }
    public static List<MailInfo> Inbox(MailKind kind);
    public static bool SendItem(string mailName, int itemId, int count, MailKind kind);
    public static bool SendMoney(string mailName, MoneyCurrency currency, float amount, MailKind kind);
    public static bool SendBundle(string mailName, IList<MailLine> lines, MailKind kind, int delayMinutes);
    public static List<PendingMail> Pending();
}
```

| Method | Returns | What it does |
|---|---|---|
| `Inbox(MailKind kind)` | `List<MailInfo>` | The mail that has arrived and is waiting. |
| `SendItem(string mailName, int itemId, int count, MailKind kind)` | `bool` | Sends the player an item. |
| `SendMoney(string mailName, MoneyCurrency currency, float amount, MailKind kind)` | `bool` | Sends the player money. |
| `SendBundle(string mailName, IList<MailLine> lines, MailKind kind, int delayMinutes)` | `bool` | Sends several items in one mail. |
| `Pending()` | `List<PendingMail>` | The player's mail that is still on its way. |

| Property | Type | What it means |
|---|---|---|
| `IsAvailable` | `bool` | `false` when the mail events could not be set up. |

| Event | Gives you | When it fires |
|---|---|---|
| `Received` | `MailReceivedArgs` | A letter arrived. `args.Name` is the letter name and `args.Kind` is `Letter`. |
| `PackageReceived` | `MailReceivedArgs` | A package arrived. `args.Name` is the package name and `args.Kind` is `Package`. |
| `Sent` | `MailSentArgs` | The game sent mail out into the world. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>MailLine</code> <span>one line of a bundle</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class MailLine
{
    public int ItemId;
    public int Count;
}
```

| Property | Type | What it means |
|---|---|---|
| `ItemId` | `int` | The item. It must be above 1. |
| `Count` | `int` | How many. It must be 1 to 1000. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>MailSentArgs</code> <span>what you get when the game sends mail into the world</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class MailSentArgs
{
    public string Name { get; }
    public int ReceiverNpcId { get; }
    public int ItemId { get; }
    public int Count { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Name` | `string` | The mail's name. |
| `ReceiverNpcId` | `int` | The NPC it is for. `0` when it has no NPC receiver. |
| `ItemId` | `int` | The item inside. `0` when the mail holds no item. |
| `Count` | `int` | How many of the item. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>PendingMail</code> <span>one piece of mail still on its way to the player</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class PendingMail
{
    public string Name { get; }
    public MailKind Kind { get; }
    public int SecondsLeft { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Name` | `string` | The mail's name. |
| `Kind` | `MailKind` | Letter or package. |
| `SecondsLeft` | `int` | Seconds until it arrives. |

</div>
</div>

## Things to know

- `Inbox` lists the mail that has arrived and is still waiting. That is the letters in the mailbox or the packages in the parcel locker. It does not list mail that is still on its way, and the game clears the list when the player takes the mail. To read only letters with text, keep the ones where `IsNote` is `true`. <span class="tech">(the game clears the list in `Mailbox.TakeMail`.)</span>
- `IsNote`, `Body` and `Sender` read a letter's note. A letter that carries a note has `IsNote` set to `true` and `Body` set to the text.
- `Sender` is who an email note is from, and is `""` for any other note. Letters with an item or money, and other letters, have `IsNote` set to `false` and an empty `Body` and `Sender`. <span class="tech">(a note letter is the game's `NoteMail`, and `Sender` is the `from` of the note when the note is an email asset.)</span>
- `SendItem` and `SendMoney` send mail to the player, and the game's mail delay applies. The mail may arrive later, as game mail does. A letter goes to the mailbox and a package to the parcel locker. <span class="tech">(they use the game's `MailController.SendMail` with `Target.Player` (letter) or `Target.PlayerParcelLocker` (package).)</span>
- `SendItem`, `SendMoney` and `SendBundle` make items or money out of nothing, so use them for rewards.
- A package also makes the game send a "Parcel Notification" letter, so `Received` fires for it too.
- `Received` and `PackageReceived` fire when mail is delivered, whether at once or after the delay.
- When a save loads, the game replays the letters and packages it has stored. The framework ignores that replay, but only for the names that were in the save's received lists when the load finished, one game notification for each stored name.
- Everything else is passed on, so a letter or package that the framework or the game delivers while the save is loading is reported. <span class="tech">(they forward the game's `MailController.MailReceived` and `PackageReceived`. The game replays two frames after `MailController.OnLoadingGame` reads the save (`MailController.DelayedOnLoaded`).)</span>
- `SendBundle` sends several items as one letter or one package. Use `MailKind.Letter` for the player's mailbox or `MailKind.Package` for the parcel locker.
- The game's own timer, in game time, delivers a bundle that has a delay. The game's default delay is not used.
- `Pending` lists the player's letters and packages that are still on their way. It does not list mail for NPCs.
- `SecondsLeft` is the timer's seconds remaining. How often the game updates that number has not been confirmed.
- `Sent` tells you when the game sends mail out into the world. The game raises it for every mail sent to the world side: a drop into a player's mailbox, and scene mail senders set to the world.
- `Sent` is not raised for the player's own letters and packages, so `SendItem`, `SendMoney` and `SendBundle` never raise it. A mail with a delay raises it when the game's timer ends, not when the mailbox drops it. <span class="tech">(it forwards the game's `MailController.MailSent` for `Target.World`: `Mailbox.InsertMail` and scene mail senders whose `mailTarget` is `World` (`logic_mail_sender`, `logic_mail_bundle_sender`). A delayed mail raises it from `TimerSendMailWorld`.)</span>
- `Sent` only says that the mail was delivered to the world side. It does not say whether anything claimed the letter, and the order of handlers compared with the game's own listeners is not defined.

## Limits

- `SendItem` and `SendBundle` refuse a count outside 1 to 1000 and an item id of 1 or less. If the inventory is full, the game drops a large count into the world.
- `SendBundle` checks each line like `SendItem`, and you need at least one line. `delayMinutes` must be 1 to 525600.
- `SendBundle` returns `false` while the game is saving or loading, the same as `SendItem`. `Pending` only reads, so it always works.
- `Sent` does not fire while a save is loading. A mail the old session sent that arrives during a load is dropped on purpose, not passed into the new save.
- A delayed mail can fire at any game time, so a handler that writes money, items or quests must check `SaveData.Busy` first. `ReceiverNpcId` is `0` when the mail has no receiver, and `ItemId` and `Count` are `0` for money or other mail that holds no item.
