---
title: Recycling API
parent: API reference
nav_order: 60
---

# Recycling API

Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Log owner: `obenseuer.moddingframework.recycling`.
Files: `src/Api/Recycling.cs`, `src/Api/RecyclingPatches.cs`.

This area wraps the bottle recycling machine (`BottleRecycling`): the OC the player is owed for returned bottles. It reads and sets that amount, and raises an event when bottles are returned. It does not pay the amount out. See "Not exposed".

## If a hook cannot be installed

At start the framework installs one prefix and one postfix on `BottleRecyclingPanel.BottleAdd`, the private method the panel runs for each returned item. If that hook is missing, the owner is disabled and `Recycling.IsAvailable` is `false`. `All`, `Owed` and `SetOwed` do not depend on the hook.

## Threading and safety

- Main thread only.
- Every member is safe at any time. `Owed` returns `-1` for an unknown id, or when no save is loaded.
- Nothing throws. A null or empty `id` finds nothing.
- `SetOwed` returns `false` while a save is loading or saving (`SaveData.Busy`).
- `Recycled` is raised only from the panel's own return step. A save or a load does not raise it. A handler that throws is logged, and the other handlers still run.

## API

```csharp
public static class Recycling
{
    public static event Action<RecycledArgs> Recycled;
    public static bool IsAvailable { get; }
    public static List<string> All();
    public static int Owed(string id);
    public static bool SetOwed(string id, int owed);
}

public sealed class RecycledArgs
{
    public string Id { get; }      // the machine's scene id
    public int Amount { get; }     // the amount argument of this return
    public int Money { get; }      // OC added to the owed amount by this return
}
```

- `All()` lists the scene id of every `BottleRecycling` in the active scene. It uses `FindObjectsByType`, which skips inactive GameObjects. A machine with no id is skipped. It returns an empty list on error.
- `Owed(id)` returns the machine's `bottleRecyclingMoney`: the OC the player can still collect there. It returns `-1` for an unknown id.
- `SetOwed(id, owed)` sets `bottleRecyclingMoney` directly. A negative amount returns `false` and changes nothing. It returns `true` after the write.
- The machine is found by its save GUID or scene path, as with other areas. The panel adds to this amount when bottles are returned, and pays it out one OC at a time. A change made here is used the next time the panel counts.
- `SetOwed` does not fire `onGiveMoney`. That relay belongs to the payout.
- `Recycled` fires once for each return the panel accepts, after the game has added the value to `bottleRecyclingMoney`. `Amount` is the number of items in that return. `Money` is the change in `bottleRecyclingMoney` across the game's call, so a mushroom return, which the game gives a random value, reports the value the game actually added. It does not fire when the panel refuses an item, and it does not fire for `SetOwed`.

## Not exposed

- The payout. `BottleRecyclingPanel.GiveCoins` is public, but it pays through `StartGiveMoney`, which reads the panel's `currentBottleRecycling` field. The game sets that field only in `Open`, which also opens the game menu, and clears it in `Close`. A mod would have to set the panel's field itself to pay with the menu closed, which the game never does. So the payout stays out, and `Owed` and `SetOwed` are the way to change the amount.
- The bottle values. The panel's category table (`BottleAdd`) is not wrapped. `Recycled` reports what the game added.
- Firing `onGiveMoney` from the API.

## Ceilings and risks

- The payout coroutine counts the field down while it runs. Setting the field during a payout changes how much is paid.
- A return made during a payout stops that payout, so the rest of the owed amount stays on the machine. `Money` still counts only that return.
