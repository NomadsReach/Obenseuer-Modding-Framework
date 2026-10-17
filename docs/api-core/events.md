---
title: Events
parent: Framework core classes
grand_parent: Framework basics
nav_order: 5
---

# Events

`Events` tells your mod when the game clock reaches a new hour or a new day, when a scene loads, and when a save is written or loaded. You subscribe with `+=`, and the framework calls your code each time it happens. There is also `Events.WhenReady`, which waits until a save is loaded and then runs your code once.

```csharp
public static class Events
{
    public static event Action<int> HourPassed;     // the new hour of the day, 0 to 23, when the game hour changes
    public static event Action<int> DayPassed;      // the new day number, after the day changed
    public static event Action<string> SceneLoaded; // the name of a scene that was loaded; fires for every load, even a scene added on top of the current one
    public static event Action Saved;               // after the save files are written
    public static event Action Loaded;              // after a save has been loaded

    public static void WhenReady(Action action);    // run now if a save is loaded or a new game's first level is loaded, otherwise once when that happens
}
```

- **Your handlers run in the order they subscribed, and one handler that fails does not stop the others.** The framework writes the error to the log. It writes the first failure of each handler for each event, and not the later ones. You may subscribe or unsubscribe inside a handler. The change starts with the next event.
- **`HourPassed` is checked once every game minute, so it can arrive up to a minute late.** If the player sleeps or the time jumps, you see the new hour on the next minute. If the time jumps to the same hour on a different day, it still fires.
- **A level change does not repeat the hour it lands on.** After a level change, `HourPassed` fires only when the hour or the day is different from the last one reported. A load or a new game started from the main menu does fire on its first game minute, even for the same hour, because the main menu forgets the last hour.
- **`Saved` and `Loaded` also fire on every level change.** The game changes level by saving and then loading again, so you get one of each.
- **If your handler lives in a scene object, unsubscribe when the object is destroyed.** Do it in `OnDestroy`. If you do not, the framework still calls the handler after its object is gone, and then only the first failure is logged.
- **`WhenReady` runs your code at once when a save is already loaded.** Otherwise it runs it once, later. An error in your code is logged, and does not stop anything else. It throws `ArgumentNullException` when `action` is null.

```csharp
using BepInEx;
using BepInEx.Unity.Mono;
using ObenseuerCore;

public class MyMod : BaseUnityPlugin
{
    private void Awake()
    {
        Events.HourPassed += hour => Logger.LogInfo("hour " + hour);
        Events.DayPassed += day => Logger.LogInfo("day " + day);
        Events.Loaded += () => Logger.LogInfo("loaded");
    }
}
```
