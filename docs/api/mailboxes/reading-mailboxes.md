---
title: Reading mailboxes
parent: Mailboxes
grand_parent: Phones, mail and media
nav_order: 2
---

# Reading mailboxes

This is the full list of what the API gives you. Text in brackets at the end of a note is extra detail for experienced modders, and you can skip it on a first read.

Each mailbox is a `MailboxInfo`, a snapshot taken when you asked. The snapshot does not update by itself, so ask again for fresh values.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Mailboxes</code> <span>read mailboxes, take mail, and the Used event</span></p>

Call `All()` or `Get(id)` to find mailboxes, and `TakeMail(id)` to give the player their waiting mail. Subscribe to `Used` to hear when a box is used.

<div class="api-pair" markdown="1">

```csharp
public static class Mailboxes
{
    public static List<MailboxInfo> All();
    public static MailboxInfo Get(string id);
    public static bool TakeMail(string id);
    public static bool IsAvailable { get; }
    public static event Action<MailboxUsedArgs> Used;
}
```

| Method | Returns | What it does |
|---|---|---|
| `All()` | `List<MailboxInfo>` | Every mailbox in the scene the player is in. |
| `Get(id)` | `MailboxInfo` | The mailbox with this id. `null` when none has it. |
| `TakeMail(id)` | `bool` | Gives the player their waiting mail. Returns `true` when the inbox is empty afterwards. |

| Property | Type | What it means |
|---|---|---|
| `IsAvailable` | `bool` | `false` when the framework could not set up the `Used` event. |

| Event | Gives you | When it fires |
|---|---|---|
| `Used` | `MailboxUsedArgs` | The player has used a mailbox. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>MailboxMode</code> <span>receive or send</span></p>

`MailboxInfo.Mode` and `MailboxUsedArgs.Mode` give you this. It says whether the box receives mail or sends it.

<div class="api-pair" markdown="1">

```csharp
public enum MailboxMode
{
    Receive = 0,
    Send = 1
}
```

| Value | Meaning |
|---|---|
| `Receive` | The player takes their waiting mail from this box. |
| `Send` | The player posts mail in this box. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>MailboxInfo</code> <span>a snapshot of one mailbox</span></p>

`All()` and `Get(id)` give you one of these. `Count` is the number the game shows for that kind of box, and it belongs to the player, not the box.

<div class="api-pair" markdown="1">

```csharp
public sealed class MailboxInfo
{
    public string Id { get; }
    public MailboxMode Mode { get; }
    public int Count { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | The id to pass to `Get` and `TakeMail`. |
| `Mode` | `MailboxMode` | Whether the box receives or sends. |
| `Count` | `int` | The mail count the game shows on the box. See Things to know. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>MailboxUsedArgs</code> <span>what Used tells you</span></p>

`Used` gives you one of these when the player uses a box that has something to do. `Id` says which box it was.

<div class="api-pair" markdown="1">

```csharp
public sealed class MailboxUsedArgs
{
    public string Id { get; }
    public MailboxMode Mode { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | The mailbox's id, the same as `MailboxInfo.Id`. |
| `Mode` | `MailboxMode` | Whether the box receives or sends. |

</div>
</div>

## Things to know

- A mailbox's id is the number the game saves it under, or a path through the scene when it has none. Use the id with `Get` and `TakeMail`. <span class="tech">(`Id` is the box's `GUID`, else the GUID of the `SavableScript` on its object, else a path counted from the scene root.)</span>
- `All()` lists the mailboxes that are switched on in the scene the player is in. A mailbox that is switched off is not listed. `Get(id)` gives back one, or `null` when no box has that id. <span class="tech">(It searches the active scene with `FindObjectsByType` and skips inactive objects.)</span>
- `Count` is the number the game shows on the box, and it is the same for every box of the same kind. The number belongs to the player, not to the box. For a `Receive` box it is the letters waiting in the player's inbox, the number in "Take N mail". For a `Send` box it is the mail the player carries that they can post, the number in "Drop N mail". <span class="tech">(For `Receive` it is `MailController.GetMailAmount`. For `Send` it is the inventory slots with mailable data, plus `TaskItemsManager.MailCount`.)</span>
- `Used` fires when the player uses a box that has something to do. That is a receive box with mail waiting, or a send box when the player has something to post. If the game refuses the use, such as an empty box, it does not fire. The event does not tell you which letters or items moved. <span class="tech">(It is a postfix on `Mailbox.Interact`. If another mod skips that call with its own prefix, `Used` can still fire for a use the game did not perform, as long as the framework's prefix ran first.)</span>
- `TakeMail(id)` takes all of the player's waiting mail, not only the letters for that box. Every receive box reads the same inbox, so the id only picks which box the game runs the action on. Each letter is given out the way it would be if the player took it: a letter can add items to the inventory, add money, teach a note or recipe, change a task item or a goal, or place furniture. The box's own signals run too, as they do for the player. `Used` does not fire for a mod's `TakeMail`. <span class="tech">(It calls the game's `Mailbox.TakeMail`, which empties the inbox and gives each letter out through `MailController.TakeMailitem`. The box's `onMailTaken` and `onMailAmount` relays run.)</span>
- `IsAvailable` is `false` when the `Used` event could not be set up. See [If something fails to set up](details.md#if-something-fails-to-set-up).
- Sending mail is not part of this page. Use `Mail` on the [economy page](../economy/mail.md#mail).

## Limits

- `TakeMail` gives back `false` when nothing was taken. That happens for an unknown id, for a send box, when the inbox is empty, while a save is loading or being written, and while money changes inside your handlers are nested too deep. It gives back `true` when the inbox is empty afterwards. So it can give back `false` if a handler sends new mail during the take. <span class="tech">(The nesting check is `Cash.Refused`, the same limit of eight levels that `Cash.MoneyChanged` handlers have. `TakeMail` also checks that the game's mail, inventory, notes, recipes and money systems are all loaded.)</span>
- If the game fails during `TakeMail`, the call gives back `false` and the error goes to the log once. The game empties the inbox before it gives the letters out, so letters after the one that failed may be lost.
