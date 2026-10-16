---
title: Tanks API
parent: API reference
nav_order: 80
---

# Tanks API

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Patch owner: `obenseuer.moddingframework.tanks`.
Files: `src/Api/Tanks.cs`.

This area wraps the liquid storage (`LiquidStorage`): the liquid it holds, how much, its capacity, and its lock. It does not cover the pipes or external storages, the fill and drain items, or the tank's UI panel.

## If a hook cannot be installed

The framework installs no hooks. Every member uses public game members, so `Tanks.IsAvailable` is `true` unless the owner was disabled.

## Threading and safety

- Main thread only.
- `Tanks.IsAvailable` is `false` only when the owner was disabled.
- Every member is safe at any time. With no save loaded, or on the main menu, `All` returns an empty list, `Get` returns `null`, and the writers return `false`.
- Nothing throws. A null or empty id finds nothing.
- The writers return `false` while `SaveData.Busy` is true.
- The writers refuse the main water source. That is the tank `WaterSourceController.instance.globalStorage` returns, which `WorldWater` reads for `Amount` and `IsInfinite`. Reads still include it, with `Global` set. Changing it would change what the water supply reports.

## API

```csharp
public static class Tanks
{
    public static List<TankInfo> All();
    public static TankInfo Get(string id);
    public static bool Set(string id, int itemId, float amount);
    public static bool Empty(string id);
    public static bool SetLocked(string id, bool locked);

    public static event Action<TankChangedArgs> Changed;

    public static bool IsAvailable { get; }
}

public sealed class TankInfo
{
    public string Id { get; }           // the save GUID, or the scene path
    public int LiquidItemId { get; }    // Liquid.ID, or 0 when the tank holds nothing
    public float Amount { get; }        // LiquidStorage.LiquidAmount
    public float Capacity { get; }      // LiquidStorage.Capacity, below 0 when infinite
    public bool Locked { get; }         // _Lock.Locked, false when the tank has no lock
    public bool Global { get; }         // LiquidStorage.global
}

public sealed class TankChangedArgs
{
    public string Id { get; }
    public int LiquidItemId { get; }
    public float Amount { get; }
    public bool Locked { get; }
}
```

- `Set(id, itemId, amount)` puts the item with that id into the tank at that amount. `amount` below 0 is treated as 0. The amount is limited to 0 through `Capacity` and then rounded to a tenth the way the game rounds a liquid level. A value that rounds above `Capacity` is set to `Capacity`. A tank with infinite or zero capacity is refused, because the game draws its level as the amount divided by `Capacity`. If the tank holds another liquid, that liquid is replaced. If the tank already holds that liquid, only the amount changes and the item's quality and freshness stay as they are. An empty tank takes the item fresh. `Set(id, itemId, 0)` leaves the tank holding that item at amount 0; use `Empty` to clear it. It returns `false` for an unknown item, a NaN or infinite amount, a null id, a busy save, the main water source, or a tank with infinite or zero capacity. Otherwise it returns `true` when the tank now holds that item at that amount.
- The game does not check that the item is a liquid, so `Set` accepts any item that exists.
- `Empty(id)` calls the game's `OnEmpty`, which clears the liquid and its metadata and fires the tank's `onEmpty` relay. Quests can react to that relay. It returns `true` when the tank holds no liquid afterwards.
- `SetLocked(id, locked)` calls the game's `Lock` or `Unlock`. It returns `false` when the tank has no lock. Otherwise it returns `true` when the lock now matches `locked`.
- `Changed` fires after `Set`, `Empty` and `SetLocked` made through this API. It does not fire for fills, drains or any other change the player or the game makes. The game changes `LiquidAmount` in many places, and this area does not hook them.
- `Global` is `LiquidStorage.global`. A global tank is saved in the cross-level save data under its GUID, so it keeps its state across a level change. Other tanks are saved with the level.
- `Get` and `All` read the current values each time.

## Ceilings and risks

- `Changed` is only for this API, as described above. A mod that needs every change has to read `All` or `Get` on its own schedule.
- `Empty` fires the `onEmpty` relay, so emptying a tank can advance a quest or open a door that the relay drives.
- `Set` with a different liquid throws away the liquid that was there. The game would refuse that without force, so this API chooses the replacement.
- `Capacity` below 0 means infinite, as the game reads it (`HasInfiniteOutputCapasity`). An infinite tank cannot be filled to a limit.
- An infinite tank with no external output storage reads the main water source for its output, unless `NotPartOfMainSource` is set. Taps read that source, so writes to the tank's own fields do not change what taps give. `Get` still reports the tank's own fields.
