---
title: Reading mailboxes
parent: Mailboxes
grand_parent: Phones, mail and media
nav_order: 2
---

# Reading mailboxes

This is the full list of what the API gives you. In every code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

Each mailbox is a `MailboxInfo`, a snapshot taken when you asked. The snapshot does not update by itself, so ask again for fresh values.

```csharp
public static class Mailboxes
{
    public static List<MailboxInfo> All();            // every mailbox in the scene the player is in
    public static MailboxInfo Get(string id);         // the mailbox with this id; null when none has it
    public static bool TakeMail(string id);           // gives the player their waiting mail; true when the inbox is empty afterwards

    public static bool IsAvailable { get; }           // false when the framework could not set up the Used event
    public static event Action<MailboxUsedArgs> Used; // the player has used a mailbox
}

public enum MailboxMode
{
    Receive = 0,   // the player takes their waiting mail from this box
    Send = 1       // the player posts mail in this box
}

public sealed class MailboxInfo    // a snapshot of one mailbox
{
    public string Id { get; }          // the id to pass to Get and TakeMail
    public MailboxMode Mode { get; }   // whether the box receives or sends
    public int Count { get; }          // the mail count the game shows on the box (see below)
}

public sealed class MailboxUsedArgs    // what Used tells you
{
    public string Id { get; }          // the mailbox's id, the same as MailboxInfo.Id
    public MailboxMode Mode { get; }   // whether the box receives or sends
}
```

- **A mailbox's id is the number the game saves it under, or a path through the scene when it has none.** Use the id with `Get` and `TakeMail`.
  - Technical: `Id` is the box's `GUID`, else the GUID of the `SavableScript` on its object, else a path counted from the scene root.
- **`All()` lists the mailboxes that are switched on in the scene the player is in.** A mailbox that is switched off is not listed. `Get(id)` gives back one, or `null` when no box has that id.
  - Technical: it searches the active scene with `FindObjectsByType` and skips inactive objects.
- **`Count` is the number the game shows on the box, and it is the same for every box of the same kind.** The number belongs to the player, not to the box.
  - For a `Receive` box, it is the letters waiting in the player's inbox. It is the number in "Take N mail".
  - For a `Send` box, it is the mail the player carries that they can post. It is the number in "Drop N mail".
  - Technical: for `Receive` it is `MailController.GetMailAmount`. For `Send` it is the inventory slots with mailable data, plus `TaskItemsManager.MailCount`.
- **`Used` fires when the player uses a box that has something to do.** That is a receive box with mail waiting, or a send box when the player has something to post. If the game refuses the use, such as an empty box, it does not fire. The event does not tell you which letters or items moved.
  - Technical: it is a postfix on `Mailbox.Interact`. If another mod skips that call with its own prefix, `Used` can still fire for a use the game did not perform, as long as the framework's prefix ran first.
- **`TakeMail(id)` takes all of the player's waiting mail, not only the letters for that box.** Every receive box reads the same inbox, so the id only picks which box the game runs the action on. Each letter is given out the way it would be if the player took it: a letter can add items to the inventory, add money, teach a note or recipe, change a task item or a goal, or place furniture. The box's own signals run too, as they do for the player. `Used` does not fire for a mod's `TakeMail`.
  - Technical: it calls the game's `Mailbox.TakeMail`, which empties the inbox and gives each letter out through `MailController.TakeMailitem`. The box's `onMailTaken` and `onMailAmount` relays run.
- **`TakeMail` gives back `false` when nothing was taken.** That happens for an unknown id, for a send box, when the inbox is empty, while a save is loading or being written, and while money changes inside your handlers are nested too deep. It gives back `true` when the inbox is empty afterwards. So it can give back `false` if a handler sends new mail during the take.
  - Technical: the nesting check is `Cash.Refused`, the same limit of eight levels that `Cash.MoneyChanged` handlers have. `TakeMail` also checks that the game's mail, inventory, notes, recipes and money systems are all loaded.
- **If the game fails during `TakeMail`, the call gives back `false` and the error goes to the log once.** The game empties the inbox before it gives the letters out, so letters after the one that failed may be lost.
- **`IsAvailable` is `false` when the `Used` event could not be set up.** See [If something fails to set up](details.md#if-something-fails-to-set-up).
- **Sending mail is not part of this page.** Use `Mail` on the [economy page](../economy/mail.md#mail).
