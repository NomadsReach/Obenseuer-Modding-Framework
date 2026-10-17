---
title: Finding doors
parent: Doors
grand_parent: Places and travel
nav_order: 2
---

# Finding doors

This is the full list of what the API gives you. In every code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

Every door is addressed by an id. `Doors.All()` and `Doors.Get(id)` give you a `DoorInfo`, a snapshot of one door taken when you asked. The snapshot does not update by itself, so ask again for fresh values.

```csharp
public static class Doors
{
    public static List<DoorInfo> All();         // every door in the scene you are in
    public static DoorInfo Get(string id);      // one door; null when the id is not known
    public static bool IsOpen(string id);       // true when that door is open; false when the id is not known

    public static bool IsAvailable { get; }     // true when the Opened and Closed events work (see Details)
}

public sealed class DoorInfo    // a snapshot of one door
{
    public string Id { get; }               // the door's id; use it with the other calls
    public string Name { get; }             // the name of the door's object in the scene
    public bool IsOpen { get; }             // true when the door was open when you asked
    public bool Moving { get; }             // true when the door is swinging or sliding
    public Vector3 Position { get; }        // where the door's object is, in world coordinates, when you asked
    public Vector3? StandPosition { get; }  // where NPCs wait for the door; null when there is no such spot
}
```

- **The id is the door's own save id. A door that has none uses a path through the scene instead.** The path is the scene name plus the door's place in the scene, so it still works as an id. It changes if someone edits the scene.
  - Technical: the id is the door's `GUID` field. A door with an empty `GUID` uses the scene name plus the child path.
- **`All()` lists the doors in the scene you are in, including the ones with no save id.** A door that is switched off is left out.
  - Technical: it uses `FindObjectsByType<Door>`, which skips inactive objects. Elevator car doors and floor doors are `Door` objects too, so they are in the list and they raise `Opened` and `Closed` when the elevator moves them.
- **Two doors can share an id, and then you get the first one.** Ids are only unique inside one scene, not between scenes.
- **`Get` gives `null` and `IsOpen` gives `false` for an id the game does not know.**
- **`IsOpen` is the game's own open flag.** It turns `true` as soon as a door starts to open. It turns `false` only when the door is fully shut, so it stays `true` while the door is closing.
  - Technical: it reads `Door.doorIsOpen`.
- **`Position` is where the door's object sits, and it is not a place to stand.** A swinging door turns around that point, and a sliding door's point moves along with the slide. The value is read when you ask, so it does not follow the door while it moves. For a spot in front of the door, use `StandPosition`.
  - Technical: it is the `transform.position` of the `Door`. The game has no standing spot of its own on a door, and the framework does not check where the object sits against the door frame.
- **`StandPosition` is where NPCs wait while a door opens for them.** It is `null` when no waypoint is linked to the door. If two waypoints are linked to it, you get the first one found.
  - Technical: it is the position of the scene's `Waypoint_Door` that links to the door (`Waypoint_Door.door`), and only active waypoints are considered.
