---
title: Travel
parent: Player
grand_parent: Player life and skills
nav_order: 8
---

# Travel

This page uses `Transform` from Unity. The [glossary](../../glossary.md#types-that-come-from-unity) says what each one is.
{: .unity-types}

Travel moves the player between places in a level. An entrypoint is a named spawn point in a level. The game uses entrypoints for level changes and for teleports inside a level.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Travel</code> <span>spawn points and teleports</span></p>

Use it to list the spawn points in the loaded level, teleport the player to one, and add or remove your own spawn points.

<div class="api-pair" markdown="1">

```csharp
public static class Travel
{
    public static bool UsingMinitrain { get; }
    public static List<string> Entrypoints();
    public static bool TeleportTo(string name);
    public static bool OpenFastTravelMenu();
    public static bool AddEntrypoint(string name, Transform location);
    public static bool AddEntrypoint(string modId, string name, Transform location);
    public static bool RemoveEntrypoint(string name);
    public static bool RemoveEntrypoint(string modId, string name);
}
```

| Property | Type | What it means |
|---|---|---|
| `UsingMinitrain` | `bool` | `true` when the player is riding the minitrain. |

| Method | Returns | What it does |
|---|---|---|
| `Entrypoints()` | `List<string>` | The names of the spawn points in the loaded level. |
| `TeleportTo(name)` | `bool` | Moves the player to the spawn point with this name. |
| `OpenFastTravelMenu()` | `bool` | Opens the game's fast travel panel. |
| `AddEntrypoint(name, location)` | `bool` | Adds a named spawn point to the loaded level. |
| `AddEntrypoint(modId, name, location)` | `bool` | The same, filed under your mod's id. |
| `RemoveEntrypoint(name)` | `bool` | Removes a spawn point that `AddEntrypoint` added. |
| `RemoveEntrypoint(modId, name)` | `bool` | Removes a spawn point that your mod added. |

</div>
</div>

## Things to know

- `Entrypoints()` lists the level's named spawn points. <span class="tech">(The names are the `PlayerLevelEntrypoints`.)</span>
- `TeleportTo(name)` moves the player to that spawn point the way the game does. It also sets off the spawn point's own outputs. <span class="tech">(It runs `PlayerLevelEntrypoints.Entrypoint.TeleportPlayer`.)</span>
- `OpenFastTravelMenu()` opens the game's fast travel panel, the same as the Fast Travel object does. It does nothing if the panel is already open. It changes no saved state, so it also works while a save is busy. <span class="tech">(It calls `FastTravelController.ToggleMenu`, and it does not check the save gate.)</span>
- `AddEntrypoint` adds a spawn point that the game can find by name. After that, a level change or a door whose target spawn point is that name places the player at `location`.
- `AddEntrypoint` returns `false` for an empty name, a `null` location, no loaded level, or a name the level already has, so a level's own spawn point is never replaced. The new spawn point is added after the level's own, so the game still picks the level's first spawn point as its default.
- `RemoveEntrypoint` removes only a spawn point that `AddEntrypoint` added. It returns `false` for a level's own spawn point, so the game's spawn points can't be removed. Call it before you destroy the `Transform` you passed: the spawn point keeps that transform, and a teleport or level change to that name would use a destroyed object.
- `AddEntrypoint` and `RemoveEntrypoint` work while a save is busy, and they write nothing to the save. That is why you can add the spawn point again from `Events.SceneLoaded`.
- With several mods, pass your mod id. `RemoveEntrypoint(modId, name)` removes only a spawn point that the same `modId` added, so another mod can't remove yours.
- The versions without `modId` share one common owner, and they can't remove a spawn point that was added with a mod id. If an add is refused because the name already exists, one warning in the log names who holds the name. An invalid `modId` throws an `ArgumentException`.

## Limits

- The fast travel menu's own list of places is built inside a part of the game that the framework can't read, so it is not available. <span class="tech">(The fast travel list is built inside a UI prefab that is not in the DLL.)</span>
- The spawn point lasts only until the level changes. It lives in the loaded level's list, so a scene change drops it. Add it again each time the level loads, for example in an `Events.SceneLoaded` handler.
- A spawn point from a mod can be chosen as the game's fallback spawn for NPCs. The game picks a random spawn point for NPCs from the whole list, and leaves out only `UnderMapDrop`, so your spawn point can be picked. <span class="tech">(The game call is `GetRandomEntrypoint`.)</span>
