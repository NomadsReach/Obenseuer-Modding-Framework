# Lights API

Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 5.4.23.5.
Namespace: `ObenseuerCore`. Patch owner: `obenseuer.moddingframework.lights`.
Files: `src/Api/Lights.cs`, `src/Api/LightsPatches.cs`.

This area wraps the switchable light fixture (`LightTurnOffOn`): whether it is on, and turning it on or off. It does not cover the multi-state light (`LightTurnOffOnMultiState`) or the `LightsON` script. See "Not exposed".

## Wiring

```csharp
internal static void Lights.Init();
```

Core calls this from `Core.Awake`.

`Init` installs three hooks on `LightTurnOffOn`: `turnLightOn`, `turnLightOff` and `TurnLightOffFade`. If any is missing, the owner is disabled and `Lights.IsAvailable` is `false`. The reads and `SetOn` keep working; `Toggled` stops.

## Threading and safety

- Main thread only.
- `Lights.IsAvailable` is `false` when the hooks were not installed or the owner was disabled.
- Every member is safe at any time. With no save loaded, or on the main menu, reads return an empty list or `null`, and `SetOn` returns `false`.
- Nothing throws. A null or empty `id` finds nothing.
- `SetOn` returns `false` while a save is loading (`SaveController.Loading`).

## API

```csharp
public static class Lights
{
    public static List<LightInfo> All();
    public static LightInfo Get(string id);
    public static bool SetOn(string id, bool on);

    public static event Action<LightToggledArgs> Toggled;

    public static bool IsAvailable { get; }
}

public sealed class LightInfo
{
    public string Id { get; }   // SceneIds.Of(light): the save GUID, or the scene path
    public bool On { get; }     // LightTurnOffOn.on
}

public sealed class LightToggledArgs
{
    public string Id { get; }   // SceneIds.Of(light)
    public bool On { get; }     // the state after the change
}
```

- `SetOn(id, on)` calls the game's `turnLightOn()` or `turnLightOff()`, so the material swap, the light objects and the outputs run as they do in game. Turning on a light that is fading out uses the game's `TurnLightOnFade(false)` instead, because a plain `turnLightOn` would leave it dark. If the light is already in that state, it does nothing and returns `true`, so the outputs do not fire twice. The return value is `true` when `On` now equals the requested value.
- `Toggled` fires when `on` really changes, from any game path: `turnLightOn`, `turnLightOff` and `TurnLightOffFade`. A call that changes nothing does not fire.

## Save loading

Loading a save does not fire `Toggled`. The game's `OnLoadingGame` writes `on` from the save and then calls `turnLightOn` or `turnLightOff`. By then `on` already holds the restored value, so the hook sees no change. The light still changes on screen. Nothing is raised for it.

## Not exposed

- `LightTurnOffOnMultiState`: its `on` flag does not say which state is lit. `Toggle(true)` with `currentState == -1` lights nothing, so a bool `SetOn` has no clear meaning. A state index API would be the next step if a modder needs it.
- `LightsON`: it has no GUID and no saved state, so it cannot be addressed by id, and a change is lost on the next scene start.
- Fade timing. `TurnLightOnFade` and `TurnLightOffFade` run over several frames. `Toggled` fires when the switch flips, not when the light has finished fading.
- Light intensity and materials.

## Ceilings and risks

- `Toggled` for a fade-off fires at once. `TurnLightOffFade` sets `on = false` immediately and the intensity fades out over later frames (`LightTurnOffOn.cs:147-152`). Not verified in game.
- `All` and `Get` use `FindObjectsByType`, which skips inactive GameObjects. Whether a fixture whose script sits on an inactive object should appear is not verified in game.
- `Id` for a fixture without a save GUID is a scene path. Moving or renaming the GameObject changes the id.
- Nothing is verified in game yet.

## Smoke test

1. Load a save with a lit room. In the BepInEx console or a small test plugin, call `Lights.All()` and check the count and `On` values.
2. Pick one `Id` that is on. Call `Lights.SetOn(id, false)`. It should return `true`, the light should go out, and `Lights.Get(id).On` should be `false`.
3. Subscribe with `Lights.Toggled += a => Log(a.Id + " " + a.On);`. Call `SetOn(id, false)` again. It should return `true` and log nothing.
4. Call `SetOn(id, true)`. It should log once with `On` true.
5. Load a different save where that light is in another state. `Toggled` should log nothing during the load.
