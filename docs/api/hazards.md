---
title: Hazards
parent: Objects and devices
grand_parent: API reference
nav_order: 7
---

# Hazards

## What it is

Hazards are the dangerous places in the game world: zones that give the player a radiation dose, zones that hurt the player, glowing radiation sources, and the detectors that measure that radiation. This API lets your mod list them, see whether each one is switched on, switch one on or off, and get told when the player walks into one. It covers the zones. The radiation dose the player has collected is on the [World](world/worldradiation.md#worldradiation) page.

## When to use it

- Switch off a radiation zone as a reward, for example after the player cleans a place.
- Warn the player, or log a line, when they walk into a dangerous zone.
- Count the hazards in a scene, or draw a map of them from their positions.

## Quick example

This mod writes a log line each time the player walks into a hazard zone. Once a save is loaded, it also logs how many hazards are switched on in the scene.

```csharp
using ObenseuerCore;

[ModInfo("example.hazardwatch", "Hazard Watch", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class HazardWatch : FrameworkMod
{
    protected override void OnLoad()
    {
        Hazards.Entered += args => Log.Info("You walked into a " + args.Kind + " zone: " + args.Id);
        Events.WhenReady(() =>
        {
            int dangerous = 0;
            foreach (HazardInfo hazard in Hazards.All())
                if (hazard.Enabled) dangerous++;
            Log.Info(dangerous + " hazards are switched on in this scene.");
        });
    }
}
```

`Entered` is an event: the framework calls your code each time it happens. `Events.WhenReady` waits until a save is loaded before it runs your code. See [Using events](../guides/getting-started/using-events.md).

## Common tasks

List the radiation zones in the scene and say where each one is and whether it is on.

```csharp
foreach (HazardInfo hazard in Hazards.All())
{
    if (hazard.Kind == HazardKind.Radiation)
        Log.Info("Radiation zone at " + hazard.Position + (hazard.Enabled ? " is on" : " is off"));
}
```

Switch a hazard off. Take the id from `Hazards.All()`. The call gives `true` when the hazard is now off.

```csharp
string id = "some id from Hazards.All()";
bool off = Hazards.SetEnabled(id, false);
Log.Info(off ? "The hazard is switched off." : "Could not switch it off.");
```

Check whether every hazard event works.

```csharp
if (!Hazards.IsAvailable) Log.Warn("Some hazard events are off.");
```

## Reference

This is the full list of what the API gives you. In the code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

### Hazards

Each hazard is a `HazardInfo`, a snapshot of its state when you asked. The `Kind` says what sort of hazard it is.

```csharp
public enum HazardKind
{
    Radiation = 0,        // an area that adds to the player's radiation dose while the player stands in it
    Hurt = 1,             // an area that damages the player while the player stands in it
    PointRadiation = 2,   // a glowing source that gives off radiation that gets weaker with distance
    Detector = 3          // a scene radiation detector that fires a signal when the radiation passes a limit
}

public static class Hazards
{
    public static List<HazardInfo> All();                       // every hazard in the scene you are in
    public static bool SetEnabled(string id, bool enabled);     // switches a hazard on (true) or off (false); true when it is now in that state

    public static event Action<HazardEnteredArgs> Entered;      // the player has just walked into a radiation or hurt zone

    public static bool IsAvailable { get; }                     // false when any of the entry events could not be set up
}

public sealed class HazardInfo    // a snapshot of one hazard
{
    public string Id { get; }                 // the hazard's id: where it is, then a slash and its kind
    public HazardKind Kind { get; }           // what sort of hazard it is
    public Vector3 Position { get; }          // where the hazard is, in world coordinates
    public bool Enabled { get; }              // true when the hazard is switched on
}

public sealed class HazardEnteredArgs    // what the Entered event tells you
{
    public string Id { get; }                 // the same id as HazardInfo.Id
    public HazardKind Kind { get; }           // Radiation or Hurt: the kind of zone the player entered
}
```

- **`All()` lists the hazards that are switched on in the scene you are in, and it is slow, so do not call it every frame.** It searches the whole scene for the four kinds each time. Call it when something happens, or on a timer, and keep the result.
  - Technical: it uses `FindObjectsByType` for `TriggerRadiation`, `TriggerHurt`, `point_radiation` and `point_radiation_detector`, active objects only. Each entry's `Kind` says which game component it is.
- **`Enabled` is the hazard's own on or off setting at the moment you asked.**
  - Technical: it is `TriggerRadiation.Enabled`, `TriggerHurt.Enabled`, `point_radiation.active` or `point_radiation_detector.on`.
- **The `Id` ends with the kind, so two hazards on one object get different ids.** A damaging zone and a radiation zone can sit on the same object. The id of the object comes first, then a slash, then the kind.
  - Technical: the first part is the object's save GUID (see the [glossary](../glossary.md#guid)), or its scene path when it has none. The scene path changes if the scene's hierarchy is edited, so do not store an id that came from a scene path.
- **`Detector` is a detector placed in the level, not the player's hand-held dosimeter.** It measures the radiation where it stands and fires the level's signals when the reading goes above or below a limit.
  - Technical: it is the game's `point_radiation_detector`, which fires its `OnAboveThreshold` and `OnBelowThreshold` outputs.
- **`SetEnabled` can switch radiation zones, hurt zones and glowing sources, but not detectors.** For a detector id it returns `false`, because the game gives mods no safe way to switch one. You can still read its `Enabled` value in `All()`.
  - Technical: `point_radiation_detector` has no public method, only the public field `on`, and no game code writes it.
- **`SetEnabled` returns `true` when the hazard is now in the state you asked for.** It returns `false` for an unknown or empty id, for a detector, and while a save is loading or saving.
- **Switching off a damaging zone that gives radiation also switches off its radiation.** When a hurt zone's damage type is radiation, the game adds a radiation zone to the same object. `SetEnabled` switches both together so the dose stops with the hurt zone. That radiation zone is also listed in `All()` as its own `Radiation` entry.
  - Technical: for a `TriggerHurt` with `DamageType` radiation, the game adds a `TriggerRadiation` component in `Start`. `SetEnabled` also calls `Enable` on every `TriggerRadiation` on that object.
- **Switching off a zone while the player is inside it does not stop the dose right away.** The zone keeps adding radiation until the player leaves. Switching off only stops new entries. A glowing source behaves differently: switching it off stops its radiation at once.
  - Technical: `TriggerRadiation.DeltaSeconds` tests `playerIn && enabled`, so the dose runs until the player leaves. `point_radiation.CalculateRadiation` returns 0 while `active` is false.
- **`Entered` fires once when the player walks into a zone that is switched on.** It does not fire again for a second part of the player touching the same zone. Switching the zone off first means no event. A damaging zone that gives radiation raises one `Hurt` event, not a second `Radiation` event as well.
  - Technical: it is raised from the game's `OnTriggerEnter`, and only when the game itself counts the player as inside: the zone's `playerIn` flag for a radiation zone, and the player's `Stats` added to the zone for a hurt zone. It does not fire while a save is loading.
- **`Entered` does not fire for glowing sources or detectors.** They are not areas the player walks into, so they have no entry.
- **`IsAvailable` is `false` when an entry event could not be set up.** Then `Entered` stops for the kind that is missing, and the other kind keeps firing. `All()` and `SetEnabled` keep working. See "If something fails to set up" below.

## Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\obenseuer.moddingframework.hazards.log`. Source files: `src/Api/Hazards.cs` and `src/Api/HazardsPatches.cs`.

Everything not listed in this reference is internal and may change without notice. Items marked experimental may change too.

### When you can call it

- **Call everything from your mod's normal code, not from a background thread you started.** The game only works safely on one thread, the main thread (see the [glossary](../glossary.md#main-thread)). This goes for the code that handles `Entered` too.
- **It is safe to call any of it at any time, even on the main menu or with no save loaded.** `All()` gives an empty list and `SetEnabled` gives `false`.
- **Your `Entered` handler runs inside the game's own call, when the game notices the player walk in.** If your handler throws an error, the framework logs it and still runs the other handlers.
  - Technical: it is raised synchronously from the game's physics trigger.

### If something fails to set up

The framework changes some of the game's methods when it starts, so it can tell your mod when the player enters a zone. If a game update breaks one of them, only that part turns off, and the framework writes error lines and a warning to the log. The rest of the framework keeps working. This page has two feature groups, one for each kind of zone.

- **The radiation zone entry event uses the feature group `obenseuer.moddingframework.hazards.radiation`, and the hurt zone entry event uses `obenseuer.moddingframework.hazards.hurt`.** If the change behind one is missing, only that group is turned off and only its events stop. `IsAvailable` becomes `false`, and the framework writes error lines and a warning to `BepInEx\logs\obenseuer.moddingframework.hazards.log`.
  - Technical: the changed methods are `TriggerRadiation.OnTriggerEnter` and `TriggerHurt.OnTriggerEnter`.
