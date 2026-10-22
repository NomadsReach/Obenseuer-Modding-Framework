---
title: Common tasks
parent: Mailboxes
grand_parent: Phones, mail and media
nav_order: 1
---

# Common tasks

Every example below is a complete mod. Each one runs once a save is loaded, because all of them need the mailboxes in the scene. Each one checks what the call gave back. The limits are on the [reading mailboxes](reading-mailboxes.md#limits) page.

## Log the mail count each box shows

```csharp
using ObenseuerCore;

[ModInfo("example.mailboxcount", "Mailbox Count", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public class MailboxCount : FrameworkMod
{
    protected override void OnLoad()
    {
        Events.WhenReady(() =>
        {
            foreach (MailboxInfo box in Mailboxes.All())
                Log.Info(box.Id + " (" + box.Mode + "): " + box.Count);
        });
    }
}
```

## Take the player's waiting mail

This works on a receive box and gives the player everything in their inbox. `TakeMail` returns `true` when it took the mail.

```csharp
using ObenseuerCore;

[ModInfo("example.takemail", "Take Mail", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public class TakeMail : FrameworkMod
{
    protected override void OnLoad()
    {
        Events.WhenReady(() =>
        {
            foreach (MailboxInfo box in Mailboxes.All())
            {
                if (box.Mode != MailboxMode.Receive)
                    continue;

                if (Mailboxes.TakeMail(box.Id))
                    Log.Info("Took the player's waiting mail.");
                else
                    Log.Warn("There was no mail to take. See the limits on the reading mailboxes page.");

                break;
            }
        });
    }
}
```

## Look up one mailbox by its id

`Get` gives back `null` when no mailbox has that id. This uses the id of the first box in the scene.

```csharp
using ObenseuerCore;

[ModInfo("example.mailboxlookup", "Mailbox Lookup", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public class MailboxLookup : FrameworkMod
{
    protected override void OnLoad()
    {
        Events.WhenReady(() =>
        {
            foreach (MailboxInfo box in Mailboxes.All())
            {
                MailboxInfo found = Mailboxes.Get(box.Id);
                if (found != null)
                    Log.Info("Count: " + found.Count);
                else
                    Log.Warn("No mailbox has that id. See the limits on the reading mailboxes page.");

                break;
            }
        });
    }
}
```
