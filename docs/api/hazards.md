# Hazards API

Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 5.4.23.5.
Namespace: `ObenseuerCore`. Patch owner: `obenseuer.moddingframework.hazards`.
Files: `src/Api/Hazards.cs`, `src/Api/HazardsPatches.cs`.

Everything not listed here is internal. Items marked **experimental** are not checked in game.

## Wiring

```csharp
internal static void Hazards.Init();
```

Core calls this from `Core.Awake`.

`Init` installs the two `Entered` hooks under one owner. If either is missing, both are removed, `Hazards.IsAvailable` becomes `false`, and one warning is logged to `BepInEx\logs\obenseuer.moddingframework.hazards.log`. `All` and `SetEnabled` keep working; only `Entered` stops.

`WorldRadiation` (Core) covers the player's radiation dose. This facade covers the zones, not the dose.

## Threading and safety

- Main thread only.
- `Hazards.IsAvailable` is false when the hooks were not installed or the owner was disabled.
- Every member is safe at any time. With no scene loaded, or on the main menu, `All` returns an empty list and `SetEnabled` returns `false`.
- `Entered` is raised synchronously from the game's physics trigger, through `Events.Raise`, so a handler that throws is logged and the other handlers still run.

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
    public string Id { get; }                 // SceneIds.Of the component, plus "/" and the kind
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

- `All` lists every `TriggerRadiation`, `TriggerHurt`, `point_radiation` and `point_radiation_detector` in the loaded scenes. `Kind` says which game component each entry is.
- `Enabled` reads the game's own flag: `TriggerRadiation.Enabled` (`Events/TriggerRadiation.cs:7`), `TriggerHurt.Enabled` (`Events/TriggerHurt.cs:7`), `point_radiation.active` (`Player/point_radiation.cs:10`) and `point_radiation_detector.on` (`Player/point_radiation_detector.cs:7`).
- `Id` appends the kind to `SceneIds.Of`, so the components on one GameObject get different ids. A `TriggerHurt` and its `TriggerRadiation` child have the same GameObject.
- `SetEnabled` calls the game's public methods: `TriggerRadiation.Enable` (`Events/TriggerRadiation.cs:18`), `TriggerHurt.Enable` (`Events/TriggerHurt.cs:31`) and `point_radiation.Toggle` (`Player/point_radiation.cs:66`). It returns `true` when the flag equals `enabled` after the call.
- A `TriggerHurt` with radiation damage type gets its own `TriggerRadiation` child at `Start` (`Events/TriggerHurt.cs:36`). `SetEnabled` on that hurt zone also sets the child, so the dose stops with the hurt zone. The child is also listed in `All` as its own `Radiation` entry.
- **point_radiation_detector cannot be set.** It has no public method, only the public field `on`, and no game C# code writes it. `SetEnabled` returns `false` for a detector id. The flag is still read in `All`.
- `Entered` fires once when the player enters a zone that is enabled. It is raised from the game's `OnTriggerEnter` (`Events/TriggerRadiation.cs:46`, `Events/TriggerHurt.cs:46`), and only when the game itself registers the player as inside: the game's `playerIn` flag for a radiation zone, and the player's `Stats` added to the zone for a hurt zone. A second collider of the same player does not raise it again. The zone's own flag decides it. A radiation-damage `TriggerHurt` raises one `Hurt` event, not a second `Radiation` event from its child.
- `Entered` does not fire for `point_radiation` or the detector, which have no trigger.

## Ceilings and risks

- Each call scans `FindObjectsByType` for the four types. Do not call `All` every frame.
- `Id` is a scene path for an object without a GUID, so it changes if the hierarchy is edited.
- Disabling a zone while the player is inside does not stop the dose. The game's `TriggerRadiation.DeltaSeconds` tests `playerIn && enabled` (`Events/TriggerRadiation.cs:87`), so the dose runs until the player leaves. Disabling only blocks new entries.
- `SetEnabled` on `point_radiation` stops its dose at once, because `CalculateRadiation` returns 0 while `active` is false (`Player/point_radiation.cs:73`).
- `SetEnabled` returns `false` while a save loads.
- Not verified in game at all.

## Smoke test

1. Walk into a radiation zone. `Hazards.All()` lists the `TriggerRadiation` with `Enabled` true.
2. Subscribe with `Hazards.Entered += a => Log(a.Kind + " " + a.Id);`. Walk into the zone. `Entered` fires once with `Kind` `Radiation`.
3. `Hazards.SetEnabled(id, false)` returns `true`. Walk into the zone again: `Entered` does not fire, and the dose stays flat.
4. `Hazards.SetEnabled(id, true)` returns `true` and the zone works again.
5. `Hazards.SetEnabled(detectorId, false)` returns `false`. `Hazards.SetEnabled("no-such-id", true)` returns `false`.
