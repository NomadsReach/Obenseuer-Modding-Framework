# Crafting upgrades API

Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 5.4.23.5.
Namespace: `ObenseuerCore`. Log owner: `obenseuer.moddingframework.craftingupgrades`.
Files: `src/Api/CraftingUpgrades.cs`.

Everything not listed here is internal. Items marked **experimental** are not checked in game.

## Wiring

No `Init`. The facade has no patches and no events, so Core does not need to call anything.

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
    public string Id { get; }                 // SceneIds.Of the upgrade: its GUID when it has one, else a scene path
    public string Name { get; }               // the GameObject name
    public bool On { get; }                   // CraftingUpgrade.Active when the list was built
    public List<string> Connected { get; }    // SceneIds.Of each connected station, in the game's order
}
```

- `All` returns every active `CraftingUpgrade` in the loaded scenes. `Connected` is the game's `CraftingUpgrade.Connected` list (`Crafting/CraftingUpgrade.cs:27`), with null entries removed. It lists the stations the upgrade is attached to, and the ids use the same `SceneIds.Of` rule as the rest of the facades.
- `SetOn` calls the game's public `CraftingUpgrade.Toggle(bool)` (`Crafting/CraftingUpgrade.cs:128`). That call does nothing when the upgrade is already in the requested state, and otherwise it refreshes the station connections and runs the game's `OnTurnON` or `OnTurnOFF` relay. Returns `true` when `Active` equals `on` after the call.
- `SetOn` returns `false` while a save loads, and while a crafting station's UI is open. The game's `AddUpgrade` and `RemoveUpgrade` do nothing while a station UI is open, so a toggle then would not reach the stations.
- `Toggle` is the game's method name. The facade exposes only `SetOn`.

## Ceilings and risks

- Each call scans `FindObjectsByType<CraftingUpgrade>`. Do not call it every frame.
- Turning an upgrade off removes it from its connected stations (`RefreshConnections`, `Crafting/CraftingUpgrade.cs:145`). A station that is busy with a recipe may lose the upgrade's bonus while the recipe runs. Not verified in game.
- `Active` is saved with the upgrade. `SetOn` has no save-in-flight refusal (`SaveSlots.SaveInFlight` is private in `src/Api/SaveSlots.cs`), but it does refuse during a load. Not verified in game.
- Not verified in game at all.

## Smoke test

1. Load a save with a crafting upgrade in a workshop. `CraftingUpgrades.All()` lists it with `On` set, and `Connected` lists the stations it is attached to.
2. `CraftingUpgrades.SetOn(id, false)` returns `true`. The upgrade's `On` is `false` in `Get(id)`, and the station loses the upgrade's effect. Turning it back on restores the effect.
3. Calling `SetOn(id, false)` twice returns `true` both times.
4. `CraftingUpgrades.Get("no-such-id")` is `null`, and `SetOn("no-such-id", true)` is `false`.
