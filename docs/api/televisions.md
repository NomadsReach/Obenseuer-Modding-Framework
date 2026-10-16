---
title: Televisions API
parent: API reference
nav_order: 82
---

# Televisions API

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Patch owner: `obenseuer.moddingframework.televisions`.
Files: `src/Api/Televisions.cs`, `src/Api/TelevisionsPatches.cs`.

This area wraps the television set (`TelevisionNoise`): its switch state, and turning it on or off. It does not cover the power supply (`PowerOn`, `PowerOff`) or the screen animation.

## If a hook cannot be installed

The framework installs two hooks on `TelevisionNoise`: `TurnOn(bool, bool)` and `TurnOff(bool, bool, bool, bool)`. Every public `TurnOn` and `TurnOff` overload goes through one of these two. It also needs the three private fields `isOn`, `locked` and `powerOn`. If any hook or field is missing, the owner is disabled and `Televisions.IsAvailable` is `false`, and `Changed` stops. With a field missing, the reads return an empty list, `null` or `false`, and `SetOn` returns `false`.

## Threading and safety

- Main thread only.
- `Televisions.IsAvailable` is `false` when the hooks were not installed or the owner was disabled.
- Every member is safe at any time. With no save loaded, or on the main menu, reads return an empty list or `null`, and `SetOn` returns `false`.
- Nothing throws. A null or empty `id` finds nothing.
- `SetOn` returns `false` while a save is loading or being written.

## API

```csharp
public static class Televisions
{
    public static List<TelevisionInfo> All();
    public static TelevisionInfo Get(string id);
    public static bool SetOn(string id, bool on);

    public static event Action<TelevisionChangedArgs> Changed;

    public static bool IsAvailable { get; }
}

public sealed class TelevisionInfo
{
    public string Id { get; }   // the save GUID, or the scene path
    public bool On { get; }     // TelevisionNoise.isOn, the switch state
}

public sealed class TelevisionChangedArgs
{
    public string Id { get; }   // save GUID, or the scene path
    public bool On { get; }     // the state after the change
}
```

- `SetOn(id, on)` refuses, and returns `false`, when the set is locked or has no power. The game's own `Interact` refuses the same two cases. Otherwise it calls the game's `TurnOn(true)` or `TurnOff(true)`. The `true` plays the switch sound, as the player's use does. If the set is already in that state, it does nothing and returns `true`. The return value is `true` when `On` now equals the requested value.
- `Changed` fires when `isOn` really changes, from any game path: the player's use, `SetOn`, or a relay or script that calls `TurnOn` or `TurnOff`. A call that changes nothing does not fire.

## Save loading

Loading a save does not fire `Changed`. The game's `OnLoadingGame` restores `isOn` and then calls `Refresh()`, which does not call `TurnOn` or `TurnOff`. `Start` calls them with the state it already has, so it does not change anything either.

## Not exposed

- The power supply. `PowerOff` and `PowerOn` are not wrapped. A powered-off set keeps `On` as it was, because the game does not change `isOn` when power is cut. Read `On` together with the power state if that matters. The power state is not exposed yet.
- The screen materials and animation.

## Ceilings and risks

- `On` is the switch state. A set that has lost power reports `On` true when it was on before the power cut, and no `Changed` fires for the cut.
- `SetOn` plays the switch sound. A mod that switches many sets at once will play that many sounds.
- `All` and `Get` list only the active scene. They use `FindObjectsByType`, which skips inactive GameObjects.
- Reads and writes go through reflection on the private field `isOn`, `locked` and `powerOn`. Mono allows this, but only the game code confirms the field names.
- `Id` for a set without a save GUID is a scene path. Moving or renaming the GameObject changes the id.

## Save loads

The framework does not gate this event on a save load. A handler that should ignore events raised while a save loads can check `SaveData.Busy`, which is true while a save is loading or being written.
