---
title: WorldLocation
parent: World
grand_parent: Places and travel
nav_order: 6
---

# WorldLocation

`WorldLocation` tells you which scene the player is in. A scene is one area of the game, such as a building or a level, and it is loaded from a scene file.

```csharp
public static class WorldLocation
{
    public static string CurrentScene { get; }        // the name of the scene the player is in
    public static List<string> KnownScenes();         // every scene name the game has
    public static List<string> VisitedScenes();       // the scenes the player has visited in this save
}
```

- **For a scene change event, use `Events.SceneLoaded`.** It is not repeated here.
- **`KnownScenes()` lists every scene name the game has.**
  - Technical: the names come from `LevelNames.bytes`.
- **`VisitedScenes()` lists the scenes the player has been to in the save they are playing.** Each name appears once, and capital letters do not matter. It returns an empty list when there is no save, never `null`.
  - Technical: it joins four sources: the game's own visited list (when `SaveSceneManager` exists), the active scene, the level files in the save folder (`*.tnmt`, except `Globals` and `Info`), and the scenes entered this session. The game does not write its own list into a save, so the save-folder files are what keep a visit after a load.
- **The "this session" part of `VisitedScenes()` starts when the framework starts.** It is cleared when a save is loaded from the load menu and when `Main Menu` or `Open Sewer Intro` loads. Door changes keep it. Saving to a new slot does not update the folder until the next load, so the active scene and the session set cover that.
- **`Main Menu` and `Open Sewer Intro` are never in the list.** The game itself treats those two scenes as not part of play. After either of them loads, the save-folder files are ignored until the next load.
- **There is no "indoors" check.** The game does not track whether the player is inside.
  - Technical: `InsideTrigger` is for build-mode placement only.
