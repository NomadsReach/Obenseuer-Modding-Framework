---
title: Ladders API
parent: API reference
nav_order: 37
---

# Ladders API

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Log owner: `obenseuer.moddingframework.ladders`.
Files: `src/Api/Ladders.cs`.

This area reads and sets the ladder's blocked state. It does not start or stop a climb.

Everything not listed here is internal.

## If a hook cannot be installed

No Harmony patches and no event. The framework needs no registration for this facade.

## Threading and safety

- Main thread only.
- Every member is safe at any time. With no scene loaded, or on the main menu, `All` returns an empty list, `Get` returns `null`, `IsPlayerClimbing` returns `false` and `SetBlocked` returns `false`.
- Nothing throws to the caller. An unknown or empty id returns `null` or `false`.
- `SetBlocked` returns `false` while a save loads or saves (`SaveData.Busy`).
- `SetBlocked` returns `false` while the ladder's own player-on-ladder flag is set, and also when that flag cannot be read. The player can still get off the ladder; the change applies once they do.
- `SetBlocked` returns `true` without writing when the ladder already has the requested value. This runs after the busy and climbing checks, so it returns `false` in those cases even when the value already matches.

## API

```csharp
public static class Ladders
{
    public static List<LadderInfo> All();
    public static LadderInfo Get(string id);
    public static bool IsPlayerClimbing { get; }
    public static bool SetBlocked(string id, bool blocked);
}

public sealed class LadderInfo
{
    public string Id { get; }        // the save GUID, or a scene path when it has none
    public bool Blocked { get; }     // InteractableLadder.blocked: the use prompt reads "Blocked" and the climb is refused
    public bool PlayerOn { get; }    // this ladder's playerClimbing flag: the player is on this ladder
}
```

## Notes

- `All` lists every active `InteractableLadder` in the active scene.
- `Get(id)` returns one `LadderInfo`, or `null` when no ladder has that id.
- `IsPlayerClimbing` is true while the player is on any ladder. It reads the static `playerClimbingGlobal`.
- `SetBlocked` writes the game's public `blocked` field. The game's `ClimbToggle` reads the same field, so nothing else needs to change.
- A ladder with an empty GUID has the id of its scene path, and `SetBlocked` on it is not saved. The save code skips objects with an empty `GUID`, so the change lasts until the scene reloads. This may not hold for every ladder.
- A ladder with a save GUID keeps its `blocked` value across a save and load, because `blocked` is serialized with the ladder.
- Climb and StopClimb are not exposed, and the ladder has no event.
