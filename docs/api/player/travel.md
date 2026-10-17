---
title: Travel
parent: Player
grand_parent: Player life and skills
nav_order: 8
---

# Travel

Travel moves the player between places in a level. An entrypoint is a named spawn point in a level. The game uses entrypoints for level changes and for teleports inside a level.

```csharp
public static class Travel
{
    public static bool UsingMinitrain { get; }              // true when the player is riding the minitrain
    public static List<string> Entrypoints();               // the names of the spawn points in the loaded level
    public static bool TeleportTo(string name);             // moves the player to the spawn point with this name
    public static bool OpenFastTravelMenu();                // opens the game's fast travel panel
    public static bool AddEntrypoint(string name, Transform location);                  // adds a named spawn point to the loaded level
    public static bool AddEntrypoint(string modId, string name, Transform location);    // the same, filed under your mod's id
    public static bool RemoveEntrypoint(string name);                                   // removes a spawn point that AddEntrypoint added
    public static bool RemoveEntrypoint(string modId, string name);                     // removes a spawn point that your mod added
}
```

- **`Entrypoints()` lists the level's named spawn points.** The fast travel menu's own list of places is built inside a part of the game that the framework can't read, so it is not available.
  - Technical: the names are the `PlayerLevelEntrypoints`. The fast travel list is built inside a UI prefab that is not in the DLL.
- **`TeleportTo(name)` moves the player to that spawn point the way the game does.** It also sets off the spawn point's own outputs.
  - Technical: it runs `PlayerLevelEntrypoints.Entrypoint.TeleportPlayer`.
- **`OpenFastTravelMenu()` opens the game's fast travel panel, the same as the Fast Travel object does.** It does nothing if the panel is already open. It changes no saved state, so it also works while a save is busy.
  - Technical: it calls `FastTravelController.ToggleMenu`, and it does not check the save gate.
- **`AddEntrypoint` adds a spawn point that the game can find by name.** After that, a level change or a door whose target spawn point is that name places the player at `location`. It returns `false` for an empty name, a `null` location, no loaded level, or a name the level already has, so a level's own spawn point is never replaced. The new spawn point is added after the level's own, so the game still picks the level's first spawn point as its default.
- **The spawn point lasts only until the level changes.** It lives in the loaded level's list, so a scene change drops it. Add it again each time the level loads, for example in an `Events.SceneLoaded` handler.
- **`RemoveEntrypoint` removes only a spawn point that `AddEntrypoint` added.** It returns `false` for a level's own spawn point, so the game's spawn points can't be removed. Call it before you destroy the `Transform` you passed: the spawn point keeps that transform, and a teleport or level change to that name would use a destroyed object.
- **`AddEntrypoint` and `RemoveEntrypoint` work while a save is busy, and they write nothing to the save.** That is why you can add the spawn point again from `Events.SceneLoaded`.
- **A spawn point from a mod can be chosen as the game's fallback spawn for NPCs.** The game picks a random spawn point for NPCs from the whole list, and leaves out only `UnderMapDrop`, so your spawn point can be picked.
  - Technical: the game call is `GetRandomEntrypoint`.
- **With several mods, pass your mod id.** `RemoveEntrypoint(modId, name)` removes only a spawn point that the same `modId` added, so another mod can't remove yours. The versions without `modId` share one common owner, and they can't remove a spawn point that was added with a mod id. If an add is refused because the name already exists, one warning in the log names who holds the name. An invalid `modId` throws an `ArgumentException`.
