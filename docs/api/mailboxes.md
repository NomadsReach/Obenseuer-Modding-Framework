---
title: Mailboxes API
parent: API reference
nav_order: 43
---

# Mailboxes API

Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Patch owner: `obenseuer.moddingframework.mailboxes`.
Files: `src/Api/Mailboxes.cs`, `src/Api/MailboxesPatches.cs`.

This area wraps the world `Mailbox` object: its mode (receive or drop), the mail count the game shows on it, and one write, `TakeMail`. The player's inbox and the parcel flow are in `docs/api/economy.md` (`Mail`). A mailbox has no mail of its own. Both modes read pools that belong to the player, not to the box.

## If a hook cannot be installed

The framework installs one hook, `Mailbox.Interact`. If it fails, the owner is disabled and one warning is logged. The reads keep working; `Used` stops.

## Threading and safety

- Main thread only.
- `Mailboxes.IsAvailable` is false when the hook was not installed or the owner was disabled.
- Every member is safe at any time. On the main menu or with no save loaded, reads return an empty list or `null`.
- Nothing throws. A null or empty `id` finds nothing.
- Objects on inactive GameObjects are not returned (`FindObjectsByType` default filter).
- `Used` is raised synchronously after the game's `Interact` returns. A handler that throws is logged and the other handlers still run.
- `TakeMail` returns `false` while a save is loading or being written, or while money handlers are nested too deep.

## API

```csharp
public static class Mailboxes
{
    public static List<MailboxInfo> All();
    public static MailboxInfo Get(string id);
    public static bool TakeMail(string id);

    public static bool IsAvailable { get; }
    public static event Action<MailboxUsedArgs> Used;
}

public enum MailboxMode { Receive = 0, Send = 1 }

public sealed class MailboxInfo
{
    public string Id { get; }          // the save GUID, else the scene path
    public MailboxMode Mode { get; }   // Mailbox.mode
    public int Count { get; }          // see below
}

public sealed class MailboxUsedArgs    // payload of Used
{
    public string Id { get; }          // as MailboxInfo.Id
    public MailboxMode Mode { get; }
}
```

- `All` lists every `Mailbox` in the active scene. `Get(id)` returns one `MailboxInfo`, or `null` when no box has that id.
- `Count` is the number the game shows on the box, and it is shared by every box:
  - `Receive`: the letters waiting in the player's inbox (`MailController.GetMailAmount`), the number behind "Take N mail".
  - `Send`: the mail items the player carries to drop (inventory slots with mailable data, plus `TaskItemsManager.MailCount`), the number behind "Drop N mail".
- `Used` fires when the player uses a box that has something to do: a receive box with mail, or a send box with mailable items or task mail, in either mode. A use the game refuses (an empty box, or nothing to send) does not fire it. The payload does not say which letters or items moved. It is a postfix on `Mailbox.Interact`, so if another mod's prefix skips that call, `Used` can still fire for a use the game did not perform, provided the facade's prefix ran first.
- `TakeMail(id)` takes the player's receive mail, not only the letters for this box. Every receive box reads the same inbox, so the id only picks the box whose game method runs. It calls the game's `Mailbox.TakeMail`, which empties the inbox and gives each letter out through `MailController.TakeMailitem`. A letter can add items to the inventory, add money, teach a note or recipe, change a task item or an objective, or place furniture. The box's `onMailTaken` and `onMailAmount` relays run, as they do for the player. `Used` is not raised by this call.
- `TakeMail` returns `false` for an unknown id, a `Send` box, an empty inbox, or while a save is busy. It returns `true` when the player's inbox is empty afterwards, so it can be `false` if a handler sends new mail during the take. If the game throws, the error is logged once and the call returns `false`. The game empties the inbox before it gives the letters out, so letters after the failing one may be lost.
- Mail is sent through `Mail` (`docs/api/economy.md`), not through this area.

## Not wrapped

- `InsertMail`. It calls `TaskItemsManager.RemoveAllMail` first, which runs `TaskItemMail.OnMailed` on every mail task item the player holds and completes its `taskToFinish` objective. Then it sends and removes every mailable inventory item. A mod cannot insert one mail without that sweep, and no framework facade sets the mailable item data, so it stays out.
- The box's output relays (`onMailTaken`, `onMailInserted`, `onMailReceived`, `onMailAmount`). They are scene wiring.
- The receive-side `MailReceived` subscription. The `Mail.Received` event covers it.

## Save loads

The framework does not gate this event on a save load. A handler that should ignore events raised while a save loads can check `SaveData.Busy`, which is true while a save is loading or being written.
