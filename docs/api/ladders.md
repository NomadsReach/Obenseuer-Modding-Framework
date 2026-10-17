---
title: Ladders
parent: Places and travel
grand_parent: API reference
nav_order: 6
---

# Ladders

## What it is

A ladder is a part of the world the player can climb. This API lets your mod list the ladders in the scene you are in, see whether a ladder is blocked, see whether the player is on one, and block or unblock a ladder. A blocked ladder shows "Blocked" on its use prompt and the game refuses the climb. It does not start or stop a climb.

## When to use it

- Block a ladder until the player finishes a quest step, then unblock it.
- Check whether the player is on a ladder before your mod does something that would get in the way.
- List the ladders in a scene.

## Quick example

This mod logs every ladder in the scene once a save is loaded, and blocks the first one.

```csharp
using ObenseuerCore;

[ModInfo("example.ladderwatch", "Ladder Watch", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class LadderWatch : FrameworkMod
{
    protected override void OnLoad()
    {
        Events.WhenReady(() =>
        {
            foreach (LadderInfo ladder in Ladders.All())
                Log.Info("Ladder " + ladder.Id + ", blocked: " + ladder.Blocked);
            if (Ladders.All().Count > 0 && Ladders.SetBlocked(Ladders.All()[0].Id, true))
                Log.Info("The first ladder is blocked.");
        });
    }
}
```

`SetBlocked` returns `false` when it could not change the ladder, for example when the player is on it. See [Using events](../guides/getting-started/using-events.md) for more on `WhenReady`.

## Common tasks

Unblock a ladder.

```csharp
LadderInfo ladder = Ladders.Get("some-id");
if (ladder != null && ladder.Blocked) Ladders.SetBlocked(ladder.Id, false);
```

Check whether the player is on a ladder.

```csharp
Log.Info(Ladders.IsPlayerClimbing ? "The player is on a ladder." : "The player is not on a ladder.");
```

## Reference

This is the full list of what the API gives you. In the code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

### Ladders

Each ladder is a `LadderInfo`, a snapshot of its state at the moment you asked. The snapshot does not update by itself, so ask again for fresh values.

```csharp
public static class Ladders
{
    public static List<LadderInfo> All();                    // every ladder in the scene you are in
    public static LadderInfo Get(string id);                 // the ladder with this id; null when none has it
    public static bool IsPlayerClimbing { get; }             // true while the player is on any ladder
    public static bool SetBlocked(string id, bool blocked);  // blocks or unblocks a ladder; true when it ends up as you asked
}

public sealed class LadderInfo    // a snapshot of one ladder
{
    public string Id { get; }        // the ladder's id: its save id, or its place in the scene when it has none
    public bool Blocked { get; }     // true when the ladder is blocked: its prompt reads "Blocked" and the climb is refused
    public bool PlayerOn { get; }    // true when the player is on this ladder
}
```

- **`All()` lists every ladder that is switched on in the scene you are in.**
  - Technical: it lists the active `InteractableLadder` components in the active scene.
- **`Get(id)` gives you one ladder, or `null` when no ladder has that id.** An unknown or empty id also gives `null`.
- **`IsPlayerClimbing` is `true` while the player is on any ladder, not only one you looked up.**
  - Technical: it reads the static `InteractableLadder.playerClimbingGlobal`.
- **`SetBlocked` returns `true` when the ladder now has the value you asked for.** It returns `false` when no ladder has that id, while a save is loading or being written, and while the player is on that ladder. The player can still get off the ladder, and you can try again after that. If none of those apply and the ladder already has the value you asked for, it returns `true` and changes nothing.
  - Technical: it writes the game's public `blocked` field, which the game's `ClimbToggle` reads, so nothing else needs to change. The check for the player on the ladder reads the ladder's own `playerClimbing` flag, and `SetBlocked` also returns `false` when that flag cannot be read. The save check is `SaveData.Busy`.
- **A blocked ladder stays blocked after the player saves and loads, if the ladder has a save id.** The game saves the blocked value with the ladder.
- **A ladder with no save id is not saved.** Its id is its place in the scene, and the change lasts only until the scene reloads. This may not hold for every ladder.
  - Technical: the game's save code skips objects with an empty `GUID`.

### What you can't do (yet)

- **You can't make the player climb or get off a ladder, and there is no event for ladders.** This API only reads and blocks.
  - Technical: `Climb` and `StopClimb` are not covered.

## Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Source: `src/Api/Ladders.cs`. Its log name is `obenseuer.moddingframework.ladders`.

Everything not listed in this reference is internal and may change without notice.

### When you can call it

- **Call everything from your mod's normal code (`OnLoad`, event handlers, `Update`), not from a background thread you started.** The game only works safely on its main thread (see the [glossary](../glossary.md#main-thread)).
- **It is safe to call any of it at any time.** With no scene loaded, or on the main menu, `All()` gives an empty list, `Get` gives `null`, `IsPlayerClimbing` gives `false` and `SetBlocked` gives `false`. Nothing throws.

### If something fails to set up

- **This part of the framework does not change any game methods, so there is no feature group that can turn off.** It needs no set-up at all.
