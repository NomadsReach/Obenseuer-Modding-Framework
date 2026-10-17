---
title: Seats
parent: Objects and devices
grand_parent: API reference
nav_order: 13
---

# Seats

## What it is

A seat is any chair in the game, toilets included. This API lets your mod list the seats in the scene you are in, see which are taken and by whom, make the player sit down in one or stand up again, and get told when the player or an NPC sits down or stands up. It does not control NPCs: the game's schedules decide when NPCs sit.

## When to use it

- React when the player sits down or stands up, for example to start a card game or a conversation.
- Find a free seat near a spot.
- Seat the player from a script or a cutscene, and stand them up again.

## Quick example

This mod writes a log line each time someone sits down or stands up. Once a save is loaded, it also logs how many seats there are in the scene and how many of them are free.

```csharp
using ObenseuerCore;

[ModInfo("example.seatwatch", "Seat Watch", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class SeatWatch : FrameworkMod
{
    protected override void OnLoad()
    {
        Seats.Sat += seat => Log.Info((seat.ByPlayer ? "The player" : "An NPC") + " sat down at " + seat.Id);
        Seats.Left += seat => Log.Info((seat.ByPlayer ? "The player" : "An NPC") + " stood up from " + seat.Id);
        Events.WhenReady(() =>
        {
            int free = 0;
            foreach (SeatInfo seat in Seats.All())
                if (!seat.IsOccupied) free++;
            Log.Info(Seats.All().Count + " seats here, " + free + " free.");
        });
    }
}
```

`Sat` and `Left` are events: the framework calls your code each time they happen. `WhenReady` waits until a save is loaded before it runs your code. See [Using events](../guides/getting-started/using-events.md) for more on both.

## Common tasks

Seat the player in the first free seat that is not a toilet. `Sit` returns `true` when the player is seated afterwards. It returns `false` when the game would not let them sit, for example because they are too far away.

```csharp
foreach (SeatInfo seat in Seats.All())
{
    if (seat.IsToilet || seat.IsOccupied) continue;
    if (Seats.Sit(seat.Id)) Log.Info("The player is sitting at " + seat.Id);
    break;
}
```

Stand the player up if they are sitting in a particular seat. `Get` returns null when no seat has that id.

```csharp
SeatInfo one = Seats.Get("some-seat-id");
if (one != null && one.OccupiedByPlayer) Seats.Leave();
```

## Reference

This is the full list of what the API gives you. Text in brackets at the end of a note is extra detail for experienced modders, and you can skip it on a first read.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Seats</code> <span>list, sit, stand and hear about seats</span></p>
<div class="api-pair" markdown="1">

```csharp
public static class Seats
{
    public static List<SeatInfo> All();
    public static SeatInfo Get(string id);
    public static bool Sit(string id);
    public static bool Leave();

    public static bool IsAvailable { get; }
    public static event Action<SeatArgs> Sat;
    public static event Action<SeatArgs> Left;
}
```

| Method | Returns | What it does |
|---|---|---|
| `All()` | `List<SeatInfo>` | Every seat in the scene you are in, toilets included. |
| `Get(string id)` | `SeatInfo` | The seat with this id. `null` when there is none. |
| `Sit(string id)` | `bool` | Makes the player sit in this seat. `true` when they are seated in it afterwards. |
| `Leave()` | `bool` | Makes the player stand up. `true` once they have started to stand. |

| Property | Type | What it means |
|---|---|---|
| `IsAvailable` | `bool` | `false` when the `Sat` and `Left` events could not be set up. |

| Event | Gives you | When it fires |
|---|---|---|
| `Sat` | `SeatArgs` | Someone has sat down. |
| `Left` | `SeatArgs` | Someone has stood up. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>SeatInfo</code> <span>a snapshot of one seat</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class SeatInfo
{
    public string Id { get; }
    public bool IsOccupied { get; }
    public bool OccupiedByPlayer { get; }
    public Vector3 Position { get; }
    public bool IsToilet { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | A name for this seat that you can use with `Get` and `Sit`. |
| `IsOccupied` | `bool` | `true` when an NPC, a piece of furniture or the player holds the seat. |
| `OccupiedByPlayer` | `bool` | `true` when the player is sitting in this seat. |
| `Position` | `Vector3` | Where the seat is, in world coordinates. |
| `IsToilet` | `bool` | `true` when the seat is a toilet. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>SeatArgs</code> <span>what <code>Sat</code> and <code>Left</code> tell you</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class SeatArgs
{
    public string Id { get; }
    public bool ByPlayer { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | The seat's id, the same as `SeatInfo.Id`. |
| `ByPlayer` | `bool` | `true` when the player did it. `false` when an NPC did. |

</div>
</div>

### Things to know

- `All()` lists the seats that are switched on in the scene you are in. Toilets are included. <span class="tech">(it lists every active `InteractableChair` in the active scene.)</span>
- `Id` is a name that identifies one seat. It normally stays the same each time the game runs. A seat the game gave no unique id gets one made from its place in the scene, and that id changes when the scene changes. <span class="tech">(it is the save GUID, else the scene path.)</span>
- `Get(id)` returns `null` for an empty or unknown id.
- `Sit(id)` makes the player sit the way the game does when they press the use key. If the game would refuse, `Sit` refuses quietly and returns `false`. <span class="tech">(the checks are `InteractableChair.PlayerIsSitting`, `SaveData.Busy`, the `GameController` state (menu, shop, pause menu, console, dialogue, `keysDisabled`, `GameIsPaused`), `PlayerState.IsSleeping`, `OpenSewerCharacterController.ControlsDisabled`, `Interactable.IsInteractDisabled`, `Occupied` and the distance to the camera (`InteractObjects.useDistance`). After the checks it calls the chair's public `Interact()`, which is virtual, so a `Toilet` keeps its own lid and bucket checks. The result is read back from `PlayerSittingHere`, so a refusal inside the game, for example a camera move still running, also gives `false`.)</span>
- `Leave()` starts the player standing up, so they may still look seated right after the call. It returns `true` once the stand-up has started. The player is no longer seated, and `Left` fires, when the camera move ends. <span class="tech">(it uses the same busy checks as `Sit`, then `Interact()` on the seated chair. It is refused while `DisableStopSit` is set (contests and card tables) or while the chair's camera move is still running. For a `Toilet` whose `enabled` is `false`, the game's `Toilet.Use` only shows a warning.)</span>
- `Sat` fires after the player sits, or after an NPC is seated while you play. `ByPlayer` tells you which. <span class="tech">(only the live schedule path raises `Sat` and `Left` for NPCs. `NPCSit` and `StopNPCSit` are driven by the NPC schedules and the tenement spawn.)</span>
- `Left` fires when the player stands up, or when an NPC is told to stand. For an NPC it fires before the game's short delay that releases the NPC.
- The events fire after the game has changed the state. A handler that throws is logged, and the other handlers still run.

### Limits

- `Sit(id)` is `false` when the seat is not found, when the player is already seated anywhere, and when the game is loading or saving a save. It is also `false` when the player is busy: a menu, shop, pause menu, console or conversation is open, keys are turned off, the game is paused, or the player is asleep. And it is `false` when the player cannot move, for example on a ladder or while picking a lock, when the seat is switched off for use or already taken, and when the player's camera is more than 1.5 metres from the seat. A toilet still shows its own warnings, such as a missing lid or bucket, because they come from the game's own use code. <span class="tech">(the camera check uses `InteractObjects.useDistance`. The result also covers a refusal inside the game, as described above.)</span>
- `Leave()` returns `false` when the player is not seated, when the game is busy in the ways listed for `Sit`, or when the game has locked them in. A card table or a contest locks the player into the chair, and a toilet that is switched off only shows a warning instead of standing the player up.
- `Sat` does not fire for NPCs that the game places in a seat while it loads or sets up a scene, and it does not fire for the sit that a save restores.
- The events never fire while a save is loading.

### What you can't do (yet)

- You can't set whether a seat counts as occupied. The game sets that flag again on every physics step from a trigger on the furniture, so a change from your mod would be overwritten at once. <span class="tech">(`InteractableChair.ChangeOccupied` is written every physics frame by `ChairOccupiedTrigger`.)</span>
- You can't put an NPC in a seat or take one out. The NPC schedules and the tenement spawn do that.
- You can't stop the player from standing up. The card game and contest code does that for itself. <span class="tech">(`DisableStopSit` is not wrapped.)</span>
- You can't change where the camera or the player goes when sitting. <span class="tech">(`cameraPosition`, `upChairOffset` and `downChairOffset` are not wrapped.)</span>
- You can't flush, empty the bucket, or use paper or the urinal from here. <span class="tech">(the `Toilet` members are not wrapped.)</span>

## Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\obenseuer.moddingframework.seats.log`.

Everything not listed in this reference is internal and may change without notice.

### When you can call it {#threading-and-safety}

- Call everything from your mod's normal code (`OnLoad`, event handlers, `Update`), not from a background thread you started. The game only works safely on one thread, the main thread (see the [glossary](../glossary.md#main-thread)).
- It is safe to call any of it at any time. With no save loaded, or on the main menu, `All()` gives an empty list, `Get` gives `null`, and `Sit` and `Leave` give `false`.
- Nothing throws an error to you. A null or empty `id` finds nothing.
- Your event handlers run inside the game's own call, one at a time. If one of your handlers throws an error, the framework logs it and still runs the other handlers.

### If something fails to set up

The framework changes some of the game's methods when it starts, so it can tell your mod when someone sits or stands. If a game update breaks one of them, only the `Sat` and `Left` events turn off, and the framework writes error lines and a warning to the log. Its feature group is `obenseuer.moddingframework.seats`, the name you will see in the log. A feature group is the named set of the framework's changes for one feature.

- `Seats.IsAvailable` is `false` when the events are switched off. `All`, `Get`, `Sit` and `Leave` keep working. If any one of the changes is missing, none of them is used. <span class="tech">(the methods are the private `InteractableChair.Sit(bool, bool)`, `StopSitEnd()`, `NPCSit(GameObject, bool)` and `StopNPCSit(GameObject)`. The framework also needs the private `disableStopSit` and `currentCoroutine` fields.)</span>
