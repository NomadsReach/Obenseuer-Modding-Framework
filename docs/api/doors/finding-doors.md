---
title: Finding doors
parent: Doors
grand_parent: Places and travel
nav_order: 2
---

# Finding doors

This page uses `Vector3`, `Transform` from Unity. The [glossary](../../glossary.md#types-that-come-from-unity) says what each one is.
{: .unity-types}

This is the full list of what the API gives you.

Every door is addressed by an id. `Doors.All()` and `Doors.Get(id)` give you a `DoorInfo`, a snapshot of one door taken when you asked. The snapshot does not update by itself, so ask again for fresh values.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Doors</code> <span>finds doors and reads their state</span></p>

Call `All()` or `Get(id)` to read doors, and `IsOpen(id)` when you only need the open flag.

<div class="api-pair" markdown="1">

```csharp
public static class Doors
{
    public static List<DoorInfo> All();
    public static DoorInfo Get(string id);
    public static bool IsOpen(string id);
    public static bool IsAvailable { get; }
}
```

| Method | Returns | What it does |
|---|---|---|
| `All()` | `List<DoorInfo>` | Every door in the scene you are in. |
| `Get(id)` | `DoorInfo` | One door. `null` when the id is not known. |
| `IsOpen(id)` | `bool` | `true` when that door is open. `false` when the id is not known. |

| Property | Type | What it means |
|---|---|---|
| `IsAvailable` | `bool` | `true` when the `Opened` and `Closed` events work (see Details). |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>DoorInfo</code> <span>a snapshot of one door</span></p>

`Doors.All()` and `Doors.Get(id)` give you these. `StandPosition` is where NPCs wait for the door, and it can be `null`. <span class="tech">(It is read from the game's `Door` object.)</span>

<div class="api-pair" markdown="1">

```csharp
public sealed class DoorInfo
{
    public string Id { get; }
    public string Name { get; }
    public bool IsOpen { get; }
    public bool Moving { get; }
    public Vector3 Position { get; }
    public Vector3? StandPosition { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | The door's id. Use it with the other calls. |
| `Name` | `string` | The name of the door's object in the scene. |
| `IsOpen` | `bool` | `true` when the door was open when you asked. |
| `Moving` | `bool` | `true` when the door is swinging or sliding. |
| `Position` | `Vector3` | Where the door's object is, in world coordinates, when you asked. |
| `StandPosition` | `Vector3?` | Where NPCs wait for the door. `null` when there is no such spot. |

</div>
</div>

## Things to know

- The id is the door's own save id. A door that has none uses a path through the scene instead. The path is the scene name plus the door's place in the scene, so it still works as an id. It changes if someone edits the scene. <span class="tech">(The id is the door's `GUID` field. A door with an empty `GUID` uses the scene name plus the child path.)</span>
- `All()` lists the doors in the scene you are in, including the ones with no save id. A door that is switched off is left out. <span class="tech">(It uses `FindObjectsByType<Door>`, which skips inactive objects. Elevator car doors and floor doors are `Door` objects too, so they are in the list and they raise `Opened` and `Closed` when the elevator moves them.)</span>
- Two doors can share an id, and then you get the first one. Ids are only unique inside one scene, not between scenes.
- `Get` gives `null` and `IsOpen` gives `false` for an id the game does not know.
- `IsOpen` is the game's own open flag. It turns `true` as soon as a door starts to open. It turns `false` only when the door is fully shut, so it stays `true` while the door is closing. <span class="tech">(It reads `Door.doorIsOpen`.)</span>
- `Position` is where the door's object sits, and it is not a place to stand. A swinging door turns around that point, and a sliding door's point moves along with the slide. The value is read when you ask, so it does not follow the door while it moves. For a spot in front of the door, use `StandPosition`. <span class="tech">(It is the `transform.position` of the `Door`. The game has no standing spot of its own on a door, and the framework does not check where the object sits against the door frame.)</span>
- `StandPosition` is where NPCs wait while a door opens for them. It is `null` when no waypoint is linked to the door. If two waypoints are linked to it, you get the first one found. <span class="tech">(It is the position of the scene's `Waypoint_Door` that links to the door (`Waypoint_Door.door`), and only active waypoints are considered.)</span>
