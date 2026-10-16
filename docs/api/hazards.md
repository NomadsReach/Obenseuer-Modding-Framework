---
title: Hazards API
parent: API reference
nav_order: 26
---

# Hazards API

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Patch owner: `obenseuer.moddingframework.hazards`.
Files: `src/Api/Hazards.cs`, `src/Api/HazardsPatches.cs`.

Everything not listed here is internal. Items marked **experimental** may change.

## If a hook cannot be installed

The framework installs the two `Entered` hooks, one group owner each: `obenseuer.moddingframework.hazards.radiation` (`TriggerRadiation.OnTriggerEnter`) and `obenseuer.moddingframework.hazards.hurt` (`TriggerHurt.OnTriggerEnter`). If one is missing, only its group owner is removed, `Hazards.IsAvailable` becomes `false`, and one warning is logged to `BepInEx\logs\obenseuer.moddingframework.hazards.log`. `All` and `SetEnabled` keep working. `Entered` stops for the kind whose hook is missing; the other kind keeps firing.

`WorldRadiation`, another framework facade, covers the player's radiation dose. This facade covers the zones, not the dose.

## Threading and safety

- Main thread only.
- `Hazards.IsAvailable` is false when either hook group was not installed or the owner was disabled.
- Every member is safe at any time. With no scene loaded, or on the main menu, `All` returns an empty list and `SetEnabled` returns `false`.
- `Entered` is raised synchronously from the game's physics trigger, so a handler that throws is logged and the other handlers still run.

## Hazards

```csharp
public enum HazardKind { Radiation = 0, Hurt = 1, PointRadiation = 2, Detector = 3 }

public static class Hazards
{
    public static List<HazardInfo> All();
    public static bool SetEnabled(string id, bool enabled);

    public static event Action<HazardEnteredArgs> Entered;

    public static bool IsAvailable { get; }
}

public sealed class HazardInfo
{
    public string Id { get; }                 // the component's id, plus "/" and the kind
    public HazardKind Kind { get; }
    public Vector3 Position { get; }          // the GameObject's world position
    public bool Enabled { get; }              // the zone's own flag when the list was built
}

public sealed class HazardEnteredArgs
{
    public string Id { get; }                 // the same id as HazardInfo.Id
    public HazardKind Kind { get; }
}
```

- `All` lists every `TriggerRadiation`, `TriggerHurt`, `point_radiation` and `point_radiation_detector` in the active scene. `Kind` says which game component each entry is.
- `Enabled` reads the game's own flag: `TriggerRadiation.Enabled`, `TriggerHurt.Enabled`, `point_radiation.active` and `point_radiation_detector.on`.
- `Id` appends the kind to the component's id, so the components on one GameObject get different ids. A `TriggerHurt` and its `TriggerRadiation` component have the same GameObject.
- `SetEnabled` calls the game's public methods: `TriggerRadiation.Enable`, `TriggerHurt.Enable` and `point_radiation.Toggle`. It returns `true` when the flag equals `enabled` after the call.
- A `TriggerHurt` with radiation damage type gets its own `TriggerRadiation` component at `Start`. `SetEnabled` on that hurt zone also sets the component, so the dose stops with the hurt zone. The component is also listed in `All` as its own `Radiation` entry.
- **`Detector` is a scene radiation detector.** It is the level's `point_radiation_detector`, which fires its outputs when the radiation reaches a threshold. It is not the player's hand-held dosimeter.
- **point_radiation_detector cannot be set.** It has no public method, only the public field `on`, and no game C# code writes it. `SetEnabled` returns `false` for a detector id. The flag is still read in `All`.
- `Entered` fires once when the player enters a zone that is enabled. It is raised from the game's `OnTriggerEnter`, and only when the game itself registers the player as inside: the game's `playerIn` flag for a radiation zone, and the player's `Stats` added to the zone for a hurt zone. A second collider of the same player does not raise it again. The zone's own flag decides it. A radiation-damage `TriggerHurt` raises one `Hurt` event, not a second `Radiation` event from its component.
- `Entered` does not fire for `point_radiation` or the detector, which have no trigger.

## Ceilings and risks

- Each call scans `FindObjectsByType` for the four types. Do not call `All` every frame.
- `Id` is a scene path for an object without a GUID, so it changes if the hierarchy is edited.
- Disabling a zone while the player is inside does not stop the dose. The game's `TriggerRadiation.DeltaSeconds` tests `playerIn && enabled`, so the dose runs until the player leaves. Disabling only blocks new entries.
- `SetEnabled` on `point_radiation` stops its dose at once, because `CalculateRadiation` returns 0 while `active` is false.
- `SetEnabled` returns `false` while a save is loading or being written.
