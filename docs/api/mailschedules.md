---
title: MailSchedules API
parent: API reference
nav_order: 98
---

# MailSchedules API

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. File: `src/Api/MailSchedules.cs`.

This area lists the scene's random mail senders and sends their mail on demand. A sender is a `RandommailSender` in a scene. It sends a random mix of letters, notes, recipes and parcels to the player.

## API

```csharp
public static List<MailSenderInfo> Senders();
public static bool SendNow(string modId, string senderId);
public static bool SendRandom(string modId, string senderId, int min, int max);

public sealed class MailSenderInfo
{
    public string Id { get; }
    public int Entries { get; }
    public List<string> MailNames { get; }
}
```

- `Senders` gives each sender's id, how many entries it has, and the mail names its entries use.
- `SendNow` sends with the sender's own minimum and maximum amounts.
- `SendRandom` sends a random number of mails between `min` (inclusive) and `max` (exclusive), as the game's own call does. It refuses `min` below 0 or `max` below `min`. It throws `ArgumentException` for a `max` above 100, because each mail is a separate send on the main thread.
- Both send calls return `false` for an empty mod id, an unknown sender, a save that is loading or saving, or bad amounts.

## Example

```csharp
foreach (MailSenderInfo sender in MailSchedules.Senders())
{
    MailSchedules.SendRandom("com.example.mymod", sender.Id, 1, 2);
}
```

## Behaviour

- A send uses the game's own random mail call, which sends each mail through the game's `MailController`.
- A send does not move the sender's own daily schedule. The game still sends its daily mail on its own day.
- Every sender uses the same id, the game's constant `RandomMail`. When a scene has more than one sender, the first one is used.

## Not exposed

- The next scheduled send time. The game keeps the last send time as a protected field.
- The game's fake invoices. They are private to the sender.

## Ceilings and risks

- A `max` of up to 100 sends up to 99 mails at once.

## Save loads

Nothing is kept by the framework. The game saves its own sender state.
