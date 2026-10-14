---
title: Crafting upgrades API
parent: API reference
nav_order: 15
---

# Crafting upgrades API

Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Log owner: `obenseuer.moddingframework.craftingupgrades`.
Files: `src/Api/CraftingUpgrades.cs`.

Everything not listed here is internal. Items marked **experimental** may change.

## If a hook cannot be installed

The facade has no patches and no events, so the framework does not need to call anything.

## Threading and safety

- Main thread only.
- Every member is safe at any time. With no scene loaded, or on the main menu, reads return an empty list or `null`.
- `SetOn` returns `false` for an unknown or empty id, and when the game call throws. Nothing throws to the caller.

## Crafting upgrades

```csharp
public static class CraftingUpgrades
{
    public static List<CraftingUpgradeInfo> All();
    public static CraftingUpgradeInfo Get(string id);     // null when no upgrade has the id
    public static bool SetOn(string id, bool on);
}

public sealed class CraftingUpgradeInfo
{
    public string Id { get; }                 // the upgrade's save GUID when it has one, else a scene path
    public string Name { get; }               // the GameObject name
    public bool On { get; }                   // CraftingUpgrade.Active when the list was built
    public List<string> Connected { get; }    // the id of each connected station, in the game's order
}
```

- `All` returns every active `CraftingUpgrade` in the active scene. `Connected` is the game's `CraftingUpgrade.Connected` list, with null entries removed. It lists the stations the upgrade is attached to, and the ids use the same rule as the rest of the facades.
- `SetOn` calls the game's public `CraftingUpgrade.Toggle(bool)`. That call does nothing when the upgrade is already in the requested state, and otherwise it refreshes the station connections and runs the game's `OnTurnON` or `OnTurnOFF` relay. Returns `true` when `Active` equals `on` after the call.
- `SetOn` returns `false` while a save is loading or being written, and while a crafting station's UI is open. The game's `AddUpgrade` and `RemoveUpgrade` do nothing while a station UI is open, so a toggle then would not reach the stations.
- `Toggle` is the game's method name. The facade exposes only `SetOn`.

## Ceilings and risks

- Each call scans `FindObjectsByType<CraftingUpgrade>`. Do not call it every frame.
- Turning an upgrade off removes it from its connected stations (`RefreshConnections`, `CraftingUpgrade`). A station that is busy with a recipe may lose the upgrade's bonus while the recipe runs.
- `Active` is saved with the upgrade. `SetOn` refuses while a save is loading or being written.
