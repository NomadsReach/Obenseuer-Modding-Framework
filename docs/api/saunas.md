# Saunas API

Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 5.4.23.5.
Namespace: `ObenseuerCore`. Patch owner: `obenseuer.moddingframework.saunas`.
Files: `src/Api/Saunas.cs`, `src/Api/SaunasPatches.cs`.

This area wraps the sauna object (`Sauna`): whether it is on, its temperature, and its löyly (steam). It does not cover the player's sauna stat (`SaunaHeat` in `docs/API.md`) or the sauna contest (`docs/api/contests.md`). The sauna cooling object (`SaunaCooling`) is not wrapped.

## Wiring

```csharp
internal static void Saunas.Init();
```

Core calls this from `Core.Awake`.

`Init` installs two hooks, `Sauna.TurnOn` and `Sauna.TurnOff`. If either is missing, the owner is disabled and one warning is logged to `BepInEx\logs\obenseuer.moddingframework.saunas.log`. The reads and `SetOn` keep working; `TurnedOn` and `TurnedOff` stop.

## Threading and safety

- Main thread only.
- `Saunas.IsAvailable` is false when the hooks were not installed or the owner was disabled.
- Every member is safe at any time. With no save loaded, or on the main menu, reads return an empty list or `null`, and `SetOn` returns `false`.
- Nothing throws. A null or empty `id` finds nothing.
- Events are raised synchronously after the game has changed the state. A handler that throws is logged and the other handlers still run (`Events.Raise`).

## API

```csharp
public static class Saunas
{
    public static List<SaunaInfo> All();
    public static SaunaInfo Get(string id);
    public static bool SetOn(string id, bool on);

    public static bool IsAvailable { get; }
    public static event Action<SaunaChangedArgs> TurnedOn;
    public static event Action<SaunaChangedArgs> TurnedOff;
}

public sealed class SaunaInfo
{
    public string Id { get; }              // SceneIds.Of: the save GUID, else the scene path
    public bool On { get; }                // Sauna.On
    public float Temperature { get; }      // Sauna.temperature, in degrees Celsius
    public float Loyly { get; }            // Sauna.löyly, the steam from throws
}

public sealed class SaunaChangedArgs       // payload of TurnedOn and TurnedOff
{
    public string Id { get; }              // as SaunaInfo.Id
    public bool On { get; }                // state after the change
    public float Temperature { get; }
    public float Loyly { get; }
}
```

- `All` lists every `Sauna` in the scene.
- `Get(id)` returns one `SaunaInfo`, or `null` when no sauna has that id.
- `SetOn(id, on)` calls the game's own `Sauna.TurnOn` or `Sauna.TurnOff`. It returns `true` when the sauna exists and `On` now equals `on`. It returns `false` when no sauna has that id. Turning a sauna on starts its timer and, for a passive sauna, sets löyly to 1 (`Sauna.cs:209`).
- `TurnedOn` fires after a sauna goes from off to on. `TurnedOff` fires after it goes from on to off. Calling `TurnOn` on a sauna that is already on fires nothing. The payload is a `SaunaChangedArgs` with the sauna's state after the change. The event comes from any caller of `Sauna.TurnOn` or `Sauna.TurnOff`, not only `SetOn`.

## Not wrapped

- Reduction rates (`ReduceDrunk`, `ReduceSMV`, `ImproveHygiene` and the others). They are public fields, read every tick in `PlayerStatsSauna.ApplySauna` (`Player/PlayerStatsSauna.cs:53`, `:88`), so a setter is possible. It is not exposed yet: eight setters are a public commitment for a tuning feature nobody has asked for.
- Player-in-sauna sessions. The sauna learns a player is inside only from a private trigger handler (`World/Sauna.cs:282`, `:290`), with no public signal to hook. A session event needs a new Unity-message patch and is left out.
- Throwing water (`Sauna.ThrowWater`, `World/Sauna.cs:244`) and the stove sounds and particles.
- `SaunaCooling`. It has its own `Toggle(bool)` (`World/SaunaCooling.cs:66`) and is a separate object.
