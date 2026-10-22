---
title: Input capture
parent: Screen and UI
grand_parent: API reference
nav_order: 6
---

# Input capture

Status: experimental.

## What it is

The game reads the keyboard through its own input manager. This API lets your mod say "the keyboard is mine right now", so the game answers "not pressed" to its own game keys while your mod holds it. Input is also held back automatically while the player is typing on a keyboard in a text field, because those keys belong to the field. The keys that leave a screen still work while the player is typing.

## When to use it

- Your mod opens its own window with buttons, and the game must not act on its keys while the player uses it.
- Your mod asks the player to press a key, for example to rebind a key, and the key must not also fire the action it is bound to.
- Your mod wants to know whether a key press will reach the game at all.

## Quick example

This mod takes the keyboard while its window is open, and gives it back when the window closes.

```csharp
using System;
using ObenseuerCore;

[ModInfo("example.panel", "Panel", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.12.0")]
public sealed class Panel : FrameworkMod
{
    private bool open;

    protected override void OnLoad()
    {
        InputCapture.Changed += OnCaptureChanged;
    }

    private void Open()
    {
        open = true;
        if (!InputCapture.Capture(Mod.Id)) Log.Warn("The game still reads its keys.");
    }

    private void Close()
    {
        open = false;
        InputCapture.Release(Mod.Id);
    }

    private void OnCaptureChanged(bool captured)
    {
        Log.Info(captured ? "The keyboard is captured." : "The keyboard is free again.");
    }
}
```

`Mod.Id` is your mod's id (see the [glossary](../glossary.md#mod-id)). `Log.Warn` writes to your own log. `InputCapture.Capture` returns `false` when the framework could not take the keyboard; then the game keeps reading its own keys.

## Reference

<div class="api-type" markdown="1">
<p class="api-type-head"><code>InputCapture</code> <span>holding the game's keyboard</span></p>

Call `Capture` with your mod id while you want the keyboard, and `Release` when you are done. `Captured` and `IsTyping` tell you what is holding it.

<div class="api-pair" markdown="1">

```csharp
public static class InputCapture
{
    public static bool Captured { get; }
    public static bool IsTyping { get; }
    public static bool Capture(string owner);
    public static void Release(string owner);
    public static event Action<bool> Changed;
}
```

| Member | Returns | What it does |
|---|---|---|
| `Captured` | `bool` | `true` while any mod holds the keyboard. |
| `IsTyping` | `bool` | `true` while the player is typing in a text field. |
| `Capture(owner)` | `bool` | Takes the keyboard for your mod id. `true` when it is held, including when it was already held. |
| `Release(owner)` | Nothing. | Gives it back. Does nothing if your mod was not holding it. |
| `Changed(captured)` | Nothing. | Runs when `Captured` changes. The value is the new state. |

</div>
</div>

### Things to know

- While `Captured` is `true`, the game sees no key press and no held key of its own. That covers the game's action keys, the pause key, the inventory key and the framework's own menu keys, because they all read the game's input manager.
- While `IsTyping` is `true`, the player is on a keyboard and no mod holds it, the game also sees no key press of its own, apart from the keys that leave a screen: `Pause Menu`, `Menu` and `Interact`. Those three still reach the game, so the player can always close the screen that holds the text field. On a controller nothing is held back for typing, because the game's on-screen keyboard reads its keys through the same input manager.
- `Capture` is counted per mod id. Several mods can hold the keyboard at once, and the keyboard is free again only when the last one releases it. Capturing twice with the same mod id has the same effect as capturing once.
- `Capture` returns `false` and writes one line to your mod's log when the framework could not set itself up. It returns `false` for an empty owner too. It never throws.
- `Release` does nothing if your mod was not holding the keyboard, so an extra release is harmless.
- `Changed` fires for capture and release only. It does not fire when `IsTyping` changes. Read `IsTyping` when you need it, for example at the start of your own `Update`.
- The keyboard is held back from the game itself, not from Unity's own `Input` class. A mod that reads `UnityEngine.Input` directly, as the framework's own key chords do, still sees the keys while the keyboard is captured. Use the framework's [key binds](settings.md) if you want them held back too.
- The keyboard is not released for you. Call `Release` when your window closes or your mod stops needing it, including when the player leaves the scene.

### Limits

- The capture covers five of the game's key readers: a single press, a held key, the repeat-while-held one, a key release, and the keyboard-only press. Any other way the game asks about the keyboard is not covered.
- A key that is held back is not stored and not delayed. When the capture ends, the game does not see the presses it missed.
- The capture is not saved. If your mod is unloaded or the game restarts while it held the keyboard, the keyboard is free again.
- The capture does not stop the player from moving with a controller or a mouse. It covers the game's action keys only.

## Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`.

Everything not listed in this reference is internal and may change without notice. Items marked experimental may change too.

The framework changes the game's key readers when it starts. Those changes belong to the feature group `obenseuer.moddingframework.inputcapture`, the name you will see in the log. Each of the five readers has its own owner, so a change that fails turns off only that one reader and the other four keep working: `obenseuer.moddingframework.inputcapture.getkeydown`, `...getkey`, `...getkeywithhold`, `...getkeyup` and `...getkeydownkeyboardonly`.

### When you can call it

- Call everything from your mod's normal code (`OnLoad`, event handlers, `Update`), not from a background thread you started. The game and Unity only work safely on one thread, the main thread (see the [glossary](../glossary.md#main-thread)).
- Your `Changed` handlers run inside the framework's capture and release calls, one at a time. If one of your handlers throws an error, the framework logs it once and still runs the other handlers.
- The check for typing runs only when the game is about to answer yes to a key, so it costs nothing while no key is pressed. It is read once per frame and gives the same answer for the rest of that frame.

### If something fails to set up

The framework changes the game's methods when it starts. If a game update breaks one of them, that reader keeps the game's normal answer, an error line and a warning go to the log, and the other four readers still hold back their keys. If all five fail, no reader is changed at all, so nothing is held back and `Capture` returns `false`; then the feature group is turned off and `InputCapture` is mentioned in the log.

### What the player loses while the keyboard is captured

While a mod holds the keyboard, the game's own ways of leaving a screen with the keyboard stop working, including the pause key. While the game is only typing, those keys still work. Give the keyboard back when your mod is done with it, so the player is never stuck.
