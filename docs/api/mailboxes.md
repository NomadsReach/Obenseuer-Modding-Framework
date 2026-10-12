# Mailboxes API

Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 5.4.23.5.
Namespace: `ObenseuerCore`. Patch owner: `obenseuer.moddingframework.mailboxes`.
Files: `src/Api/Mailboxes.cs`, `src/Api/MailboxesPatches.cs`.

This area wraps the world `Mailbox` object: its mode (receive or drop) and the mail count the game shows on it. The player's inbox and the parcel flow are in `docs/api/economy.md` (`Mail`). A mailbox has no mail of its own. Both modes read pools that belong to the player, not to the box.

## Wiring

```csharp
internal static void Mailboxes.Init();
```

Core calls this from `Core.Awake`.

`Init` installs one hook, `Mailbox.Interact`. If it fails, the owner is disabled and one warning is logged. The reads keep working; `Used` stops.

## Threading and safety

- Main thread only.
- `Mailboxes.IsAvailable` is false when the hook was not installed or the owner was disabled.
- Every member is safe at any time. On the main menu or with no save loaded, reads return an empty list or `null`.
- Nothing throws. A null or empty `id` finds nothing.
- Objects on inactive GameObjects are not returned (`FindObjectsByType` default filter).
- `Used` is raised synchronously after the game's `Interact` returns. A handler that throws is logged and the other handlers still run.

## API

```csharp
public static class Mailboxes
{
    public static List<MailboxInfo> All();
    public static MailboxInfo Get(string id);

    public static bool IsAvailable { get; }
    public static event Action<MailboxUsedArgs> Used;
}

public enum MailboxMode { Receive = 0, Send = 1 }

public sealed class MailboxInfo
{
    public string Id { get; }          // SceneIds.Of: the save GUID, else the scene path
    public MailboxMode Mode { get; }   // Mailbox.mode
    public int Count { get; }          // see below
}

public sealed class MailboxUsedArgs    // payload of Used
{
    public string Id { get; }          // as MailboxInfo.Id
    public MailboxMode Mode { get; }
}
```

- `All` lists every `Mailbox` in the scene. `Get(id)` returns one `MailboxInfo`, or `null` when no box has that id.
- `Count` is the number the game shows on the box, and it is shared by every box:
  - `Receive`: the letters waiting in the player's inbox (`MailController.GetMailAmount`), the number behind "Take N mail".
  - `Send`: the mail items the player carries to drop (inventory slots with mailable data, plus `TaskItemsManager.MailCount`), the number behind "Drop N mail".
- `Used` fires each time the player uses a box, in either mode. It fires even when the game refuses the action (no mail to take or drop), because the game's `Interact` is the event point. The payload does not say whether anything moved.
- Mail is not written here. `TakeMail` and `InsertMail` are public game methods, but a mail write touches the inbox and the parcel flow that `Mail` and the mail area own, so this facade stays read-only.

## Not wrapped

- `TakeMail` and `InsertMail` (`Interaction/Mailbox.cs:136`, `:153`). A write through them is a mail-system change and is left to the mail area.
- The box's output relays (`onMailTaken`, `onMailInserted`, `onMailReceived`, `onMailAmount`). They are scene wiring.
- The receive-side `MailReceived` subscription (`Interaction/Mailbox.cs:44`). The `Mail.Received` event covers it.
