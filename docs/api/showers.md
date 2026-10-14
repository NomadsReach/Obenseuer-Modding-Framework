---
title: Showers API
parent: API reference
nav_order: 73
---

# Showers API

Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Patch owner: `obenseuer.moddingframework.showers`.
Files: `src/Api/Showers.cs`, `src/Api/ShowersPatches.cs`.

This area wraps `InteractableShower`: whether the shower is on and whether its water is running. It does not wrap the shower's cooling effect (`SaunaCooling`-style stats) or the water tank contents.

## If a hook cannot be installed

The framework checks the `showerOn` and `waterOn` fields, then installs two hooks, `InteractableShower.TurnOn(bool, bool)` and `TurnOff(bool)`. If either step fails, the owner is disabled and one warning is logged, and `Changed` stops. A missing field makes the reads return `false` (`On` and `Water` are `false`). `SetOn` still calls the game's method, but the `true` it returns depends on the same read, so it reports `false` for `on = true`.

## Threading and safety

- Main thread only.
- `Showers.IsAvailable` is false when the hooks were not installed or the owner was disabled.
- Every member is safe at any time. On the main menu or with no save loaded, reads return an empty list or `null`, and `SetOn` returns `false`.
- Nothing throws except `ArgumentException` for invalid arguments. A null or empty `id` finds nothing.
- `SetOn` returns `false` while a save is loading or being written.
- Objects on inactive GameObjects are not returned, because `FindObjectsByType` uses its default filter.
- `Changed` is raised synchronously after the game has changed the state. A handler that throws is logged and the other handlers still run.

## API

```csharp
public static class Showers
{
    public static List<ShowerInfo> All();
    public static ShowerInfo Get(string id);
    public static bool SetOn(string id, bool on);

    public static bool IsAvailable { get; }
    public static event Action<ShowerChangedArgs> Changed;
}

public sealed class ShowerInfo
{
    public string Id { get; }     // the save GUID, else the scene path
    public bool On { get; }       // InteractableShower.showerOn (protected)
    public bool Water { get; }    // InteractableShower.waterOn (protected): water is flowing
}

public sealed class ShowerChangedArgs   // payload of Changed
{
    public string Id { get; }     // as ShowerInfo.Id
    public bool On { get; }       // state after the change
}
```

- `All` lists every `InteractableShower` in the active scene.
- `Get(id)` returns one `ShowerInfo`, or `null` when no shower has that id.
- `SetOn(id, on)` calls the game's own `TurnOn()` or `TurnOff()`, which play the sound, set the animator and start or stop the water. It returns `true` when the shower exists and `On` now equals `on`. A repeat call, for the state the shower is already in, is a no-op that returns `true`. A shower with an empty output tank turns on but `Water` stays `false`, as in the game.
- `Changed` fires when `On` flips. It fires for any caller of `TurnOn` or `TurnOff`, including the player's use key (`Interact`). It does not fire when a save loads, because the restored state already matches the state the load-time call sets.

## Not wrapped

- `StartWater` and `StopWater` as separate calls. They run from `TurnOn` and `TurnOff` and from the output tank, so a mod toggles the water through `SetOn`.
- The output tank (`LiquidStorage`) and the cooling effect (`SaunaCooling`), which the shower sets on and off.
- The shower's owner (`owner`) and the owned-shower message. The game's `Interact` does not check the owner, so `SetOn` does not either.

## Save loads

The framework does not gate this event on a save load. A handler that should ignore events raised while a save loads can check `SaveData.Busy`, which is true while a save is loading or being written.
