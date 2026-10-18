---
title: Hazards
parent: Objects and devices
grand_parent: API reference
nav_order: 7
---

# Hazards

This page uses `Vector3` from Unity. The [glossary](../glossary.md#types-that-come-from-unity) says what each one is.
{: .unity-types}

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

This is the full list of what the API gives you. Text in brackets at the end of a note is extra detail you can skip on a first read.

### Hazards

Each hazard is a `HazardInfo`, a snapshot of its state when you asked. The `Kind` says what sort of hazard it is.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Hazards</code> <span>lists hazards and switches them</span></p>

This is the class you start from. `All()` lists the hazards, `SetEnabled` switches one on or off, and `Entered` tells you when the player walks into a radiation or hurt zone.

<div class="api-pair" markdown="1">

```csharp
public static class Hazards
{
    public static List<HazardInfo> All();
    public static bool SetEnabled(string id, bool enabled);
    public static event Action<HazardEnteredArgs> Entered;
    public static bool IsAvailable { get; }
}
```

| Method | Returns | What it does |
|---|---|---|
| `All()` | `List<HazardInfo>` | Every hazard in the scene you are in, switched on or off. Hazards on objects that are switched off in the scene are not listed. |
| `SetEnabled(id, enabled)` | `bool` | Switches a hazard on (`true`) or off (`false`). Returns `true` when the hazard is now in that state. |

| Property | Type | What it means |
|---|---|---|
| `IsAvailable` | `bool` | `false` when any of the entry events could not be set up. |

| Event | Gives you | When it fires |
|---|---|---|
| `Entered` | `HazardEnteredArgs` | The player has just walked into a radiation or hurt zone. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>HazardInfo</code> <span>a snapshot of one hazard</span></p>

`All()` gives you this, one per hazard in the scene. It is a copy of the hazard when you asked, so ask again for a current `Enabled`. <span class="tech">(It is filled from the game's `TriggerRadiation`, `TriggerHurt`, `point_radiation` or `point_radiation_detector` component.)</span>

<div class="api-pair" markdown="1">

```csharp
public sealed class HazardInfo
{
    public string Id { get; }
    public HazardKind Kind { get; }
    public Vector3 Position { get; }
    public bool Enabled { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | The hazard's id: where it is, then a slash and its kind. |
| `Kind` | `HazardKind` | What sort of hazard it is. |
| `Position` | `Vector3` | Where the hazard is, in world coordinates. |
| `Enabled` | `bool` | `true` when the hazard is switched on. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>HazardKind</code> <span>what sort of hazard it is</span></p>

`HazardInfo.Kind` holds one of these for each hazard, and `HazardEnteredArgs.Kind` is `Radiation` or `Hurt`. Each value stands for one kind of game component, so it is the framework's own enum.

<div class="api-pair" markdown="1">

```csharp
public enum HazardKind
{
    Radiation = 0,
    Hurt = 1,
    PointRadiation = 2,
    Detector = 3
}
```

| Value | Meaning |
|---|---|
| `Radiation` | An area that adds to the player's radiation dose while the player stands in it. |
| `Hurt` | An area that damages the player while the player stands in it. |
| `PointRadiation` | A glowing source that gives off radiation that gets weaker with distance. |
| `Detector` | A scene radiation detector that fires a signal when the radiation passes a limit. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>HazardEnteredArgs</code> <span>what the Entered event tells you</span></p>

`Entered` gives you this. The framework builds it when the player walks into a radiation or hurt zone, so it is the framework's own type.

<div class="api-pair" markdown="1">

```csharp
public sealed class HazardEnteredArgs
{
    public string Id { get; }
    public HazardKind Kind { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | The same id as `HazardInfo.Id`. |
| `Kind` | `HazardKind` | `Radiation` or `Hurt`: the kind of zone the player entered. |

</div>
</div>

### Things to know

- `All()` lists the hazards in the scene you are in, switched on or off. It is slow, so do not call it every frame. Call it when something happens, or on a timer, and keep the result. <span class="tech">(it searches the whole scene for the four kinds each time, using `FindObjectsByType` for `TriggerRadiation`, `TriggerHurt`, `point_radiation` and `point_radiation_detector`, active objects only; each entry's `Kind` says which game component it is)</span>
- `Enabled` is the hazard's own on or off setting at the moment you asked. <span class="tech">(it is `TriggerRadiation.Enabled`, `TriggerHurt.Enabled`, `point_radiation.active` or `point_radiation_detector.on`)</span>
- The `Id` is the object's id, then a slash, then the kind, so a damaging zone and a radiation zone on one object get different ids. <span class="tech">(the first part is the object's save GUID (see the [glossary](../glossary.md#guid)), or its scene path when it has none; the scene path changes if the scene's hierarchy is edited, so do not store an id that came from a scene path)</span>
- `Detector` is a detector placed in the level, not the player's hand-held dosimeter. It measures the radiation where it stands and fires the level's signals when the reading goes above or below a limit. <span class="tech">(it is the game's `point_radiation_detector`, which fires its `OnAboveThreshold` and `OnBelowThreshold` outputs)</span>
- `SetEnabled` returns `true` when the hazard is now in the state you asked for. It returns `false` for an unknown or empty id, for a detector, and while a save is loading or saving.
- Switching off a damaging zone that gives radiation also switches off its radiation. The game puts a radiation zone on the same object, and `SetEnabled` switches both together, so the dose stops with the hurt zone. That radiation zone is also listed in `All()` as its own `Radiation` entry. <span class="tech">(for a `TriggerHurt` with `DamageType` radiation, the game adds a `TriggerRadiation` component in `Start`; `SetEnabled` also calls `Enable` on every `TriggerRadiation` on that object)</span>
- `Entered` fires once when the player walks into a zone that is switched on. It does not fire again for a second part of the player touching the same zone, and switching the zone off first means no event. A damaging zone that gives radiation raises one `Hurt` event, not a second `Radiation` event as well. <span class="tech">(it is raised from the game's `OnTriggerEnter`, and only when the game itself counts the player as inside: the zone's `playerIn` flag for a radiation zone, and the player's `Stats` added to the zone for a hurt zone; it does not fire while a save is loading)</span>
- `Entered` does not fire for glowing sources or detectors. They are not areas the player walks into, so they have no entry.

### Limits

- `SetEnabled` can switch radiation zones, hurt zones and glowing sources, but not detectors. For a detector id it returns `false`, because the game gives mods no safe way to switch one. You can still read its `Enabled` value in `All()`. <span class="tech">(`point_radiation_detector` has no public method, only the public field `on`, and no game code writes it)</span>
- Switching off a zone while the player is inside it does not stop the dose right away. The zone keeps adding radiation until the player leaves, and switching it off only stops new entries. A glowing source is different: switching it off stops its radiation at once. <span class="tech">(`TriggerRadiation.DeltaSeconds` tests `playerIn && enabled`, so the dose runs until the player leaves; `point_radiation.CalculateRadiation` returns 0 while `active` is false)</span>
- `IsAvailable` is `false` when an entry event could not be set up. Then `Entered` stops for the kind that is missing, and the other kind keeps firing. `All()` and `SetEnabled` keep working. See "If something fails to set up" below.

## Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\obenseuer.moddingframework.hazards.log`. Source files: `src/Api/Hazards.cs` and `src/Api/HazardsPatches.cs`.

Everything not listed in this reference is internal and may change without notice. Items marked experimental may change too.

- The framework changes some of the game's methods when it starts, so it can tell your mod when the player enters a zone. If a game update breaks one of them, only that part turns off, and the framework writes error lines and a warning to the log. The rest of the framework keeps working.
- This page has two feature groups, one for each kind of zone.

### When you can call it

- Call everything from your mod's normal code, not from a background thread you started. The game only runs safely on the main thread (see the [glossary](../glossary.md#main-thread)). This goes for the code that handles `Entered` too.
- It is safe to call any of it at any time, even on the main menu or with no save loaded. `All()` gives an empty list and `SetEnabled` gives `false`.
- Your `Entered` handler runs inside the game's own call, when the game notices the player walk in. If your handler throws an error, the framework logs it and still runs the other handlers. <span class="tech">(it is raised synchronously from the game's physics trigger)</span>

### If something fails to set up

- The radiation zone entry event uses the feature group `obenseuer.moddingframework.hazards.radiation`, and the hurt zone entry event uses `obenseuer.moddingframework.hazards.hurt`. If the change behind one is missing, only that group is turned off and only its events stop. `IsAvailable` becomes `false`, and the framework writes error lines and a warning to `BepInEx\logs\obenseuer.moddingframework.hazards.log`. <span class="tech">(the changed methods are `TriggerRadiation.OnTriggerEnter` and `TriggerHurt.OnTriggerEnter`)</span>
