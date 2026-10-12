# Televisions API

Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 5.4.23.5.
Namespace: `ObenseuerCore`. Patch owner: `obenseuer.moddingframework.televisions`.
Files: `src/Api/Televisions.cs`, `src/Api/TelevisionsPatches.cs`.

This area wraps the television set (`TelevisionNoise`): its switch state, and turning it on or off. It does not cover the power supply (`PowerOn`, `PowerOff`) or the screen animation.

## Wiring

```csharp
internal static void Televisions.Init();
```

Core calls this from `Core.Awake`.

`Init` installs two hooks on `TelevisionNoise`: `TurnOn(bool, bool)` and `TurnOff(bool, bool, bool, bool)`. Every public `TurnOn` and `TurnOff` overload goes through one of these two. It also needs the three private fields `isOn`, `locked` and `powerOn`. If any hook or field is missing, the owner is disabled and `Televisions.IsAvailable` is `false`, and `Changed` stops. With a field missing, the reads return an empty list, `null` or `false`, and `SetOn` returns `false`.

## Threading and safety

- Main thread only.
- `Televisions.IsAvailable` is `false` when the hooks were not installed or the owner was disabled.
- Every member is safe at any time. With no save loaded, or on the main menu, reads return an empty list or `null`, and `SetOn` returns `false`.
- Nothing throws. A null or empty `id` finds nothing.
- `SetOn` returns `false` while a save is loading (`SaveController.Loading`).

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
    public string Id { get; }   // SceneIds.Of(television): the save GUID, or the scene path
    public bool On { get; }     // TelevisionNoise.isOn, the switch state
}

public sealed class TelevisionChangedArgs
{
    public string Id { get; }   // SceneIds.Of(television)
    public bool On { get; }     // the state after the change
}
```

- `SetOn(id, on)` refuses, and returns `false`, when the set is locked or has no power. The game's own `Interact` refuses the same two cases (`TelevisionNoise.cs:232-241`). Otherwise it calls the game's `TurnOn(true)` or `TurnOff(true)`. The `true` plays the switch sound, as the player's use does. If the set is already in that state, it does nothing and returns `true`. The return value is `true` when `On` now equals the requested value.
- `Changed` fires when `isOn` really changes, from any game path: the player's use, `SetOn`, or a relay or script that calls `TurnOn` or `TurnOff`. A call that changes nothing does not fire.

## Save loading

Loading a save does not fire `Changed`. The game's `OnLoadingGame` restores `isOn` and then calls `Refresh()`, which does not call `TurnOn` or `TurnOff`. `Start` calls them with the state it already has, so it does not change anything either.

## Not exposed

- The power supply. `PowerOff` and `PowerOn` (`TelevisionNoise.cs:403-424`) are not wrapped. A powered-off set keeps `On` as it was, because the game does not change `isOn` when power is cut. Read `On` together with the power state if that matters. The power state is not exposed yet.
- The screen materials and animation.

## Ceilings and risks

- `On` is the switch state. A set that has lost power reports `On` true when it was on before the power cut, and no `Changed` fires for the cut.
- `SetOn` plays the switch sound. A mod that switches many sets at once will play that many sounds.
- `All` and `Get` use `FindObjectsByType`, which skips inactive GameObjects. Not verified in game.
- Reads and writes go through reflection on the private field `isOn`, `locked` and `powerOn`. Mono allows this, but only the decompile confirms the field names. Not verified in game.
- `Id` for a set without a save GUID is a scene path. Moving or renaming the GameObject changes the id.
- Nothing is verified in game yet.

## Smoke test

1. Load a save with a television in the scene. In the BepInEx console or a small test plugin, call `Televisions.All()` and check the count and `On` values.
2. Pick one `Id` that is on, in a room with power and no lock. Call `Televisions.SetOn(id, false)`. It should return `true`, the screen should go dark, and `Televisions.Get(id).On` should be `false`.
3. Subscribe with `Televisions.Changed += a => Log(a.Id + " " + a.On);`. Call `SetOn(id, true)`. It should return `true` and log once with `On` true.
4. Walk up to a locked set and call `SetOn(id, true)`. It should return `false` and change nothing.
5. Use the set by hand. `Changed` should log once for each switch.
6. Load a save where the set is in another state. `Changed` should log nothing during the load.
