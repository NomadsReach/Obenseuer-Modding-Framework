---
title: Crafting upgrades
parent: Items and crafting
grand_parent: API reference
nav_order: 3
---

# Crafting upgrades

## What it is

A crafting upgrade is an add-on part that attaches to one or more crafting stations (workbenches and the like) and improves them, for example by unlocking extra recipes or raising the quality of what they make. An upgrade can be switched on or off. This API lists the upgrades in the scene you are in, shows which stations each one is attached to, and lets your mod switch one on or off.

## When to use it

- Find out which upgrades are in the current scene and whether each one is on.
- Switch an upgrade on as a reward, or off as a penalty or a story event.
- Work out which stations an upgrade is attached to.

## Quick example

This mod logs every crafting upgrade in the scene once a save is loaded, with its state and how many stations it is attached to.

```csharp
using ObenseuerCore;

[ModInfo("example.upgradewatch", "Upgrade Watch", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class UpgradeWatch : FrameworkMod
{
    protected override void OnLoad()
    {
        Events.WhenReady(() =>
        {
            foreach (CraftingUpgradeInfo upgrade in CraftingUpgrades.All())
                Log.Info(upgrade.Name + " on=" + upgrade.On + " stations=" + upgrade.Connected.Count);
        });
    }
}
```

`WhenReady` runs your code once the game is ready. See [Using events](../guides/getting-started/using-events.md) for more on it.

## Common tasks

Look up one upgrade by its `Id` (you get the ids from `All()`). `Get` returns `null` when no upgrade has that id.

```csharp
string id = "put an id from All() here";
CraftingUpgradeInfo upgrade = CraftingUpgrades.Get(id);
if (upgrade != null) Log.Info(upgrade.Name + " is on: " + upgrade.On);
```

Switch an upgrade on. `SetOn` returns `true` when the upgrade is in the state you asked for afterwards.

```csharp
string id = "put an id from All() here";
bool done = CraftingUpgrades.SetOn(id, true);
Log.Info("Upgrade is on: " + done);
```

Switch off every upgrade that is on.

```csharp
foreach (CraftingUpgradeInfo upgrade in CraftingUpgrades.All())
    if (upgrade.On) CraftingUpgrades.SetOn(upgrade.Id, false);
```

## Reference

This is the full list of what the API gives you.

### Crafting upgrades

`CraftingUpgrades` lists the upgrades and switches them. Each upgrade is a `CraftingUpgradeInfo`, a snapshot of its state at the moment you asked. The snapshot does not update by itself, so ask again for fresh values.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>CraftingUpgrades</code> <span>list and switch crafting upgrades</span></p>
<div class="api-pair" markdown="1">

```csharp
public static class CraftingUpgrades
{
    public static List<CraftingUpgradeInfo> All();
    public static CraftingUpgradeInfo Get(string id);
    public static bool SetOn(string id, bool on);
}
```

| Method | Returns | What it does |
|---|---|---|
| `All()` | `List<CraftingUpgradeInfo>` | Every upgrade in the scene you are in. |
| `Get(string id)` | `CraftingUpgradeInfo` | The upgrade with this id. `null` when none has it. |
| `SetOn(string id, bool on)` | `bool` | Switches an upgrade on or off. `true` when it ended up in that state. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>CraftingUpgradeInfo</code> <span>a snapshot of one upgrade</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class CraftingUpgradeInfo
{
    public string Id { get; }
    public string Name { get; }
    public bool On { get; }
    public List<string> Connected { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | A name for this upgrade. Use it with `Get` and `SetOn`. |
| `Name` | `string` | The upgrade object's name in the scene. |
| `On` | `bool` | `true` when the upgrade was on at the moment you asked. |
| `Connected` | `List<string>` | The id of each station the upgrade is attached to. |

</div>
</div>

#### Things to know

- `All()` lists the upgrades in the scene you are in, and it is slow, so do not call it every frame. It searches the scene each time. Call it when something happens, or on a timer, and keep the result. An upgrade whose whole object is switched off in the scene is not listed. <span class="tech">(It finds every active `CraftingUpgrade` in the active scene with `FindObjectsByType`.)</span>
- `Id` is a saved name when the upgrade has one, and a place in the scene when it does not. The saved name stays the same between sessions. The place in the scene looks like `SceneName:Root[0]/Child[2]` and changes if objects in the scene are reordered. <span class="tech">(It is the object's `GUID` when it has one, otherwise the scene path. The station ids in `Connected` follow the same rule as the station ids on the [Recipes, crafting and growing](recipes.md) page.)</span>
- `Connected` lists the stations the upgrade is attached to, in the game's order. An upgrade that is not attached to anything has an empty list. <span class="tech">(It is `CraftingUpgrade.Connected` with the null entries left out.)</span>
- `SetOn(id, on)` switches the upgrade and tells you whether it worked. If the upgrade is already in the state you asked for, nothing changes and you get `true`. Otherwise the game updates the stations the upgrade is attached to and runs the upgrade's on or off actions. <span class="tech">(It calls the game's public `CraftingUpgrade.Toggle(bool)`, which refreshes the connections and runs the `OnTurnON` or `OnTurnOFF` relay. The result is `Active == on` after the call.)</span>
- Switching an upgrade off removes its bonus from the stations it was attached to. <span class="tech">(This is `RefreshConnections` on `CraftingUpgrade`.)</span>
- The on or off state is saved with the upgrade. It is still the same when the player loads the save again. <span class="tech">(`Active` is part of the upgrade's saved data (`OnSavingGame`).)</span>

#### Limits

- `SetOn` returns `false` and does nothing in a few cases. That is when the id is empty or no upgrade has it, while a save is loading or being written, and while a crafting station's screen is open. The game does not pass the change on to the stations while a station screen is open, so the switch would have no effect on them. <span class="tech">(The game's `AddUpgrade` and `RemoveUpgrade` do nothing while `CraftingBase.currentCraftingBase` is set.)</span>
- A station that is busy with a recipe may lose the upgrade's bonus while the recipe runs.
- You can't call the game's own `Toggle` method. The framework only gives you `SetOn`.

## Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\obenseuer.moddingframework.craftingupgrades.log`.

Everything not listed in this reference is internal and may change without notice.

### When you can call it

- Call everything from your mod's normal code (`OnLoad`, event handlers, `Update`), not from a background thread you started. The game and Unity only work safely on one thread, the main thread (see the [glossary](../glossary.md#main-thread)).
- It is safe to call any of it at any time, even on the main menu or while a save is loading. If there is no scene to look in, `All()` gives an empty list, `Get` gives `null`, and `SetOn` gives `false`. Nothing is thrown to you. <span class="tech">(If the game itself fails, the first failure of each member goes to the log and you get the same plain default.)</span>

### If something fails to set up

- There is nothing to set up here. The framework does not change any game method for crafting upgrades, so there is no feature group for this page and nothing that turns off by itself.
