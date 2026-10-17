---
title: Law actions
parent: Law and crime
grand_parent: API reference
nav_order: 1
---

# Law actions

Status: experimental.

## What it is

When the player commits crimes in the game, the game keeps a criminal record. When the law catches up with them, the player can pay a fine, try to bribe their way out, or be arrested and have their things taken. This API lets your mod do those things for the player: check whether a fine can be paid or a bribe tried, pay the fine, try the bribe, and move everything the player carries into a container. Reading the record and sending the player to prison are on the [Story API](../api-story/law-experimental-for-arrest.md#law-experimental-for-arrest) page, in the `Law` part.

## When to use it

- Build your own police encounter that lets the player pay a fine or try a bribe.
- Take the player's things when your mod arrests them, and put them in an evidence locker.
- Check whether paying or bribing is possible before you offer it.

## Quick example

This mod waits for the law to spot the player. It then writes a log line that says whether the player could pay the fine and whether a bribe is still possible.

```csharp
using ObenseuerCore;

[ModInfo("example.lawcheck", "Law Check", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class LawCheck : FrameworkMod
{
    protected override void OnLoad()
    {
        Law.Spotted += () => Log.Info("Spotted. Can pay the fine: " + LawActions.CanPayFine() + ", can bribe: " + LawActions.CanBribe());
    }
}
```

`Spotted` is an event: the framework calls your code each time it happens. See [Using events](../guides/getting-started/using-events.md) for more on events.

## Common tasks

Pay the player's fine when they can afford it, and try a bribe when one is still possible. `TryBribe` returns `false` when the player cannot afford the price or when the skill roll fails.

```csharp
if (LawActions.CanPayFine())
{
    LawActions.PayFine(Mod.Id);
}

if (LawActions.CanBribe() && !LawActions.TryBribe(Mod.Id))
    Log.Info("The bribe did not work.");
```

Move everything the player carries into a container, for example an evidence locker. You need the container's `GUID`, which you can read from the [container list](inventory/containers.md#containers). Passing `false` keeps what is already in the container.

```csharp
if (LawActions.ConfiscateInventory(Mod.Id, containerGuid, false))
    Log.Info("The player's things went into the evidence locker.");
```

## Reference

This is the full list of what the API gives you. In the code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

Money here uses the game's two currencies: OC is Open Sewer Coins and RM is Real Money (see the [glossary](../glossary.md#oc)).

```csharp
public static class LawActions
{
    public static bool CanPayFine();   // true when the fine is above 0 and the player has the Real Money for it
    public static bool CanBribe();     // true until a bribe has been tried for this criminal record
    public static bool PayFine(string modId);    // pays the fine and clears the record; true when it did
    public static bool TryBribe(string modId);   // tries a bribe; true when it worked and the record is cleared
    // Moves the player's things into the container with this GUID. Returns true when it was done.
    // clearContainer true empties the container first.
    public static bool ConfiscateInventory(string modId, string containerGuid, bool clearContainer);
}
```

- **Every call returns `false` unless the player has a criminal record to deal with.** That means the law system must be working, the record must have at least one crime, and the game must not be loading or saving, changing level, or in the middle of an arrest. Stolen goods count as crimes here, as they do in the bribe price.
  - Technical: the checks are `Law.IsAvailable`, `Crime.CountCrimes() > 0`, no `SaveGate.Busy`, no level change and no `Crime.ArrestInProgress`.
- **A write call needs your mod's id.** `PayFine`, `TryBribe` and `ConfiscateInventory` throw `ArgumentException` for an empty or invalid mod id. Pass your mod's id, such as `Mod.Id`. Apart from that, nothing throws. A bad argument or a failure in the game gives `false`, and the first failure of each call goes to the log as an error.
  - Technical: a mod id must be 1 to 64 characters of letters, digits, `.`, `_` or `-`.
- **`CanPayFine` is `true` when the fine is above 0 and the player has that much Real Money.** The fine is the player's crimes, without stolen goods, times the fine multiplier, rounded to a whole number. A fine that rounds to 0 can't be paid, and stolen goods alone give no fine. Read the amount with `Law.FineRM`.
  - Technical: it is the game's `Crime.CanPayFine()`, which checks the money as Real Money.
- **`CanBribe` is `true` until a bribe has been tried for this record.** A try that failed counts as a try.
  - Technical: it is `!Crime.triedBribe`.
- **`PayFine` pays the fine with the player's Real Money and clears the record.** It returns `false` unless `CanPayFine` is `true`. The check matters because the game takes the player's stolen goods away before it looks at the money, so paying without enough money would still lose the stolen goods. The framework checks first so that cannot happen. A payment from your mod fires `Law.Fined` like any other payment.
  - Technical: the framework already watches the game's `Crime.PayFine`, so `Law.Fined` fires.
- **`TryBribe` works out the price, checks the player can pay it, and only then makes the attempt.** The price is in Open Sewer Coins. The game lets the player try only once for each record, and the attempt uses that one try. So when the player has too little money, `TryBribe` returns `false` and the one try is still there. A failed skill roll does use up the try, as in the game. A successful bribe clears the record and returns `true`. It returns `false` when `CanBribe` is `false`.
  - Technical: the framework runs `Crime.CalculateBribe()`, then checks the money against the price before it calls `Crime.TryToBribe()`, which sets the once-per-record flag (`triedBribe`).
- **A bribe can cost nothing.** The game works the price out as the player's crime total times 5, divided by their Bribery skill, rounded. A small crime total rounds to 0. Then `TryBribe` clears the record with no payment and returns `true`.
  - Technical: the price is `round(crime total * 5 / Bribery skill quality)` with the skill quality at least 0.25. With a skill quality of 1, a total under about 0.1 gives 0.
- **`ConfiscateInventory` moves the player's things into the container with the given `GUID`.** Things the player carries that belong to someone else are removed instead of moved. The backpack spot is not used. It returns `false` for an empty or unknown container. Find the `GUID` on the [container list](inventory/containers.md#containers).
  - Technical: it calls `Crime.ConfiscateItems(storage, clearContainer, null)`. An item with an owner id above 0 is removed with `RemoveStack`.
- **`ConfiscateInventory` can't be undone, so use it with care.** With `clearContainer` set to `true`, every slot of the container is emptied and no copy is kept. Each item the player carries is then moved in, and an item that does not fit is deleted, not left behind. Use it only for a seizure where the player should lose the items.
  - Technical: the game moves each stack with `deleteFailed` set.
- **You can't read the bribe price from here.** Working out the price fixes it for the record, so reading it would change the game's state. Read the price from the game's dialogue instead. This is not offered.
- **For the fine amount, use `Law.FineRM`, and to send the player to prison, use `Law.Arrest`.** `Law.Arrest` already sends the player to prison.

## Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\obenseuer.moddingframework.lawactions.log`.

Everything not listed in this reference is internal and may change without notice. Items marked experimental may change too.

### When you can call it

- **Call everything from your mod's normal code (`OnLoad`, event handlers, `Update`), not from a background thread you started.** The game only works safely on one thread, the main thread (see the [glossary](../glossary.md#main-thread)).
- **It is safe to call at any time.** If the player has no record, or the game is busy, you get `false`.

### If something fails to set up

This API does not change any game method, so there is no feature group to turn off, and nothing for the framework to set up. It depends on `Law.IsAvailable`. When that is `false`, every call here returns `false`.
