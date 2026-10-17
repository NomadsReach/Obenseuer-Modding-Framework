---
title: Simple lights
parent: Lights
grand_parent: Objects and devices
nav_order: 2
---

# Simple lights

This is the full list of what the API gives you, one section for each part. In every code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

A simple light is a fixture that is either on or off. `Lights.All()` lists them, and each one is a `LightInfo`, a snapshot of the light at the moment you asked. The snapshot does not update by itself, so ask again for fresh values.

```csharp
public sealed class LightInfo    // a snapshot of one simple light
{
    public string Id { get; }    // its save id, or when it has none a text path of where it sits in the scene
    public bool On { get; }      // true when the light is on
}

public sealed class LightToggledArgs    // what the Toggled event tells you
{
    public string Id { get; }    // the light, the same as LightInfo.Id
    public bool On { get; }      // true when the light is on after the change
}

public static class Lights
{
    public static List<LightInfo> All();                  // every simple light in the active scene
    public static LightInfo Get(string id);               // one light; null when nobody has that id
    public static bool SetOn(string id, bool on);         // switches the light; true when it now matches

    public static event Action<LightToggledArgs> Toggled; // a light was just switched on or off

    public static bool IsAvailable { get; }               // false when a light event could not be set up
}
```

- **`All()` and `Get(id)` only see the scene you are in.** Lights that are switched off at the object level, or in another scene, are not listed. Whether a fixture whose script sits on a switched-off object should be listed is still undecided.
  - Technical: they use `FindObjectsByType`, which skips inactive objects.
- **`Id` is the light's save id, or a text path when it has none.** A path changes if the object is moved or renamed, so do not keep it for long.
  - Technical: the id is the `GUID` field, or the scene path when `GUID` is empty.
- **`SetOn(id, on)` switches the light the way the game does.** The materials, the light objects and the signals wired to the light all run as they do in the game. If the light is already in the state you asked for, nothing happens, you get `true`, and the signals do not fire twice.
  - Technical: it calls the game's `turnLightOn()` or `turnLightOff()`. The return value is `true` when `LightTurnOffOn.on` now equals the requested value.
- **Switching on a light that is fading out brings it back properly.** The game fades some lights out over a few frames. If you ask for such a light to turn on, the framework uses the game's fade-in instead of the plain switch, because the plain switch would leave it dark.
  - Technical: it calls `TurnLightOnFade(false)` when the private `fadeOut` flag is set.
- **`SetOn` returns `false` for an id nobody has, and while a save is loading or being written.** It also returns `false` for a multi-state light, because `All`, `Get` and `SetOn` don't see those.
- **`Toggled` fires whenever a light really changes between on and off, whoever changed it.** That includes your `SetOn` calls, the player, and the game's own scripts. A call that changes nothing does not fire.
  - Technical: it is raised after `turnLightOn`, `turnLightOff` and `TurnLightOffFade` when `on` is different from before.
- **`Toggled` for a light that fades out fires at once, not when the fade is over.** The light is already counted as off, and its brightness then fades out over the next few frames.
  - Technical: `TurnLightOffFade` sets `on = false` immediately.
- **Loading a save does not fire `Toggled`, even though lights change on screen.** The game restores each light's saved state before it switches the light, so the framework sees no change.
  - Technical: the game's `OnLoadingGame` writes `on` from the save and then calls `turnLightOn` or `turnLightOff`. By then `on` already holds the restored value.
