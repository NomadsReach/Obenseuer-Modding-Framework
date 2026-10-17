---
title: Mail schedules
parent: Phones, mail and media
grand_parent: API reference
nav_order: 4
---

# Mail schedules

Status: experimental.

## What it is

Every day the game posts a little random mail to the player: items, notes, recipes and parcels. The part of the game that does this is a random mail sender. This API lets your mod list the random mail senders in the scene you are in and make one send its mail right now, instead of waiting for its daily time.

## When to use it

- Send the player a batch of the game's own random mail when something happens in your mod, for example after a quest step.
- Find out what kinds of mail a sender can send.
- Test how your mod reacts to new mail without waiting for the next day.

## Quick example

This mod waits until a save is loaded, then makes every random mail sender in the scene send one mail.

```csharp
using ObenseuerCore;

[ModInfo("example.mailnow", "Mail Now", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class MailNow : FrameworkMod
{
    protected override void OnLoad()
    {
        Events.WhenReady(() =>
        {
            foreach (MailSenderInfo sender in MailSchedules.Senders())
            {
                bool sent = MailSchedules.SendRandom(Mod.Id, sender.Id, 1, 2);
                Log.Info("Sender " + sender.Id + " has " + sender.Entries + " entries, sent: " + sent);
            }
        });
    }
}
```

`Mod.Id` is your mod's id, the first value in `ModInfo`. See the [glossary](../glossary.md#mod-id). `Events.WhenReady` runs your code once a save is loaded, or at once if one already is. See [Using events](../guides/getting-started/using-events.md).

## Common tasks

Make a sender send mail the way the game does each day. The sender uses its own smallest and largest amount, which in the game's files is 1 to 3 mails (so one or two). The call returns `true` when the mail was sent.

```csharp
foreach (MailSenderInfo sender in MailSchedules.Senders())
    MailSchedules.SendNow(Mod.Id, sender.Id);
```

Send a number of mails you choose. The largest amount is not included, so `3, 6` sends 3, 4 or 5 mails.

```csharp
foreach (MailSenderInfo sender in MailSchedules.Senders())
    MailSchedules.SendRandom(Mod.Id, sender.Id, 3, 6);
```

See what kinds of mail a sender can send.

```csharp
foreach (MailSenderInfo sender in MailSchedules.Senders())
    Log.Info(sender.Id + ": " + string.Join(", ", sender.MailNames.ToArray()));
```

## Reference

This is the full list of what the API gives you. Text in brackets at the end of a note is extra detail for experienced modders, and you can skip it on a first read.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>MailSchedules</code> <span>list senders and send their mail</span></p>
<div class="api-pair" markdown="1">

```csharp
public static class MailSchedules
{
    public static List<MailSenderInfo> Senders();
    public static bool SendNow(string modId, string senderId);
    public static bool SendRandom(string modId, string senderId, int min, int max);
}
```

| Method | Returns | What it does |
|---|---|---|
| `Senders()` | `List<MailSenderInfo>` | The random mail senders in the scene you are in. |
| `SendNow(string modId, string senderId)` | `bool` | Sends mail with the sender's own amounts. `true` when it was sent. |
| `SendRandom(string modId, string senderId, int min, int max)` | `bool` | Sends a number of mails from `min` up to, but not including, `max`. `true` when it was sent. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>MailSenderInfo</code> <span>one random mail sender</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class MailSenderInfo
{
    public string Id { get; }
    public int Entries { get; }
    public List<string> MailNames { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | The sender's id. Pass it to `SendNow` and `SendRandom`. |
| `Entries` | `int` | How many kinds of mail the sender can choose from. |
| `MailNames` | `List<string>` | The names the mail can have, each listed once. |

</div>
</div>

### Things to know

- `Senders()` lists the random mail senders in the scene you are in. For each one you get its id, how many kinds of mail it can choose from, and the names that mail can have. <span class="tech">(It lists the active `RandommailSender` objects of the active scene. `Entries` is the size of the sender's `randomMail` list. `MailNames` collects the `mailNames` of every entry, without repeats.)</span>
- Every sender has the same id, so you can only reach one sender in a scene. If a scene has more than one sender, `SendNow` and `SendRandom` use the first one they find. <span class="tech">(The id is the game's constant `RandommailSender.GUID`, which is `RandomMail`, so every sender reports it.)</span>
- `SendNow` sends as many mails as the sender would on its own. In the game files that is one or two mails, chosen at random each time. <span class="tech">(The sender's private `minAmount` and `maxAmount` are 1 and 3, and the game's `UnityEngine.Random.Range(int, int)` leaves out the top value.)</span>
- `SendRandom` sends a random number of mails from `min` up to, but not including, `max`. So `SendRandom(id, sender, 1, 2)` always sends exactly one mail. If `min` and `max` are equal it sends `min` mails. Each mail picks one of the sender's kinds of mail at random, so two mails can be the same kind.
- Sending mail now does not change the sender's own daily schedule. The game still sends its usual mail on its usual day, so the player gets both.
- You can't read when the next scheduled mail comes, and you can't reach the game's fake bills. The game keeps the time of the last send in a field that mods can't see, and it makes its fake bills inside the sender where nothing outside can call them. <span class="tech">(`lastSend` is a protected field and `SendFakeInvoice` is private.)</span>
- The framework keeps no data of its own for this API. The game saves its own sender state.

### Limits

- `SendRandom` refuses amounts that make no sense. It returns `false` when `min` is below 0 or `max` is below `min`. It throws an `ArgumentException` when `max` is above 100. Each mail is a separate send and the game waits for all of them, so a huge number would freeze the game. <span class="tech">(Each mail is one `MailController.SendMail` call, made one after another on the main thread.)</span>
- Both send calls return `false` when they can't send. That happens when your mod id is empty, when no sender has that id, and while a save is loading or saving.
- A `true` result means the framework asked the game to send, not that the mail arrived. If the game fails while sending, for example because a kind of mail has nothing in it, the game writes the error to its own log and the call still returns `true`. <span class="tech">(`RandommailSender.SendRandomMail` catches every exception itself and logs it with `Debug.LogException`.)</span>

## Details

- Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
- Namespace: `ObenseuerCore`. File: `src/Api/MailSchedules.cs`.
- Everything not listed in this reference is internal and may change without notice. Items marked experimental may change too.

### When you can call it

- Call everything from your mod's normal code (`OnLoad`, event handlers, `Update`), not from a background thread you started. The game and Unity only work safely on one thread, the main thread (see the [glossary](../glossary.md#main-thread)).

### What to check for and what comes back

- This API changes no game method, so it has no `IsAvailable` and no feature group. It only calls the game's own mail sender.
- `Senders()` gives an empty list when there is no sender. That is normal on the main menu, or in a scene without one.
- If the game itself fails while the framework works, you get `false` or an empty list instead of an error. The first failure of each call goes to the log, filed under your mod id. Later failures of the same call are not logged again.
