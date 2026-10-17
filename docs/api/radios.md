---
title: Radios
parent: Phones, mail and media
grand_parent: API reference
nav_order: 10
---

# Radios

## What it is

The radios in the game's rooms are objects you can switch on, and each can be tuned to a channel. This API lets your mod list the radios in the scene you are in, read whether each one is switched on and which channel it is on, and switch a radio on or off.

The game has no single radio that plays everywhere. Each radio is its own object with its own sound, so every call here is about one radio, found by its id. Nothing here can start or stop a station for the whole game. For the game's music and sound, see the [Audio page](audio.md), which says that the game has no music or radio player for a mod to control.

## When to use it

- Switch a radio off when your mod starts a scene or a cutscene.
- Check whether a radio is playing before your mod talks over it.
- Read which channel a radio is on.

## Quick example

This mod waits for you to press F8. Then it switches every radio in the scene off if any of them is on, or on if they are all off.

```csharp
using ObenseuerCore;
using UnityEngine;

[ModInfo("example.radiotoggle", "Radio Toggle", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class RadioToggle : FrameworkMod
{
    private ModKeybind toggleKey;

    protected override void OnLoad()
    {
        toggleKey = Mod.Key("Toggle", "Switch the radios on or off", KeyCode.F8);
    }

    private void Update()
    {
        if (toggleKey == null || UiState.IsMenuOpen || UiState.IsPaused || UiState.IsInputBlocked) return;
        if (!toggleKey.Down()) return;
        bool anyOn = false;
        foreach (RadioInfo radio in Radios.All())
        {
            if (radio.Enabled) anyOn = true;
        }
        foreach (RadioInfo radio in Radios.All())
        {
            Radios.SetEnabled(radio.Id, !anyOn);
            Log.Info(radio.Title + " is now " + (anyOn ? "off" : "on") + ".");
        }
    }
}
```

See the [keybinds guide](../guides/getting-started/keybinds.md) for `Mod.Key`.

## Common tasks

List the radios and their channels.

```csharp
foreach (RadioInfo radio in Radios.All())
    Log.Info(radio.Title + ": " + (radio.Enabled ? "on" : "off") + ", channel " + radio.State);
```

Switch one radio off, if you know its id. `SetEnabled` gives `false` when nothing was found.

```csharp
if (!Radios.SetEnabled(radioId, false)) Log.Info("No radio has that id.");
```

Find out if one radio is playing. A radio plays when it is switched on and on a channel above 0.

```csharp
RadioInfo found = Radios.Get(radioId);
bool playing = found != null && found.Enabled && found.State > 0;
```

In these snippets, `radioId` is the `Id` of a radio from `Radios.All()`.

## Reference

This is the full list of what the API gives you. `Radios.All()` and `Radios.Get(id)` give you a `RadioInfo`, a snapshot of one radio taken when you asked. The snapshot does not update by itself, so ask again for fresh values.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Radios</code> <span>the radios in the scene</span></p>
<div class="api-pair" markdown="1">

```csharp
public static class Radios
{
    public static List<RadioInfo> All();
    public static RadioInfo Get(string id);
    public static bool Enabled(string id);
    public static int State(string id);
    public static bool SetEnabled(string id, bool enabled);
}
```

| Method | Returns | What it does |
|---|---|---|
| `All()` | `List<RadioInfo>` | Every radio in the scene you are in. |
| `Get(string id)` | `RadioInfo` | One radio. `null` when no radio has that id. |
| `Enabled(string id)` | `bool` | `true` when the radio is switched on. `false` when the id is not known. |
| `State(string id)` | `int` | The channel. `0` when the radio is off or the id is not known. |
| `SetEnabled(string id, bool enabled)` | `bool` | Switches the radio on or off. `true` when the radio was found. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>RadioInfo</code> <span>a snapshot of one radio</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class RadioInfo
{
    public string Id { get; }
    public string Title { get; }
    public Vector3 Position { get; }
    public bool Enabled { get; }
    public int State { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | The radio's id. Use it with the other calls. |
| `Title` | `string` | The name the game shows for it. `"Radio"` when it has none. |
| `Position` | `Vector3` | Where the radio is, in world coordinates. |
| `Enabled` | `bool` | `true` when the radio is switched on. |
| `State` | `int` | The channel. `0` is off, `1` and up are the channels. |

</div>
</div>

### Things to know

- `All()` lists every radio in the scene you are in whose object is active. A radio whose object is switched off in the scene (not the radio's power) is left out. <span class="tech">(It lists every active `Radio` in the active scene.)</span>
- The id is the radio's own save id. A radio without one gets a path through the scene instead. The path is made of the object names from the top of the scene down to the radio, each with its place among its neighbours. It stays the same while the scene keeps its order. <span class="tech">(The id is `Radio.GUID`, or `<scene name>:<root>[i]/.../<radio>[j]` for an empty `GUID`, where each part is a hierarchy node name and its sibling index. The game clears the GUIDs of prefabs spawned through `logic_prefab_spawn`.)</span>
- `Enabled` is the radio's power, not its channel. A radio that is switched off can still be set to a channel, and keeps that channel until it is switched on again, the way the game does it. <span class="tech">(`Enabled` reads `Radio.RadioEnabled`, and `State` reads `Radio.CurrentState`.)</span>
- `Title` is what the game shows when you look at the radio. If the radio has no name of its own, the game calls it "Radio". <span class="tech">(It is `Interactable.title`, which the game sets to "Radio" in `Radio.Start` when it is empty.)</span>
- `SetEnabled` switches the radio on or off the way the game does, and it keeps the channel. It stops or starts the sound and changes the radio's look. It gives `true` when the radio was found and the switch ran. The game ignores a call that does not change anything, so switching an already-on radio on does nothing and still gives `true`. <span class="tech">(It calls the game's own `Radio.EnableRadio(bool)`, which stops or starts the sound and swaps the mesh material, and leaves `CurrentState` as it is.)</span>
- Switching a radio on plays the sounds and fires the triggers of its channel. The game sets the channel again, so you hear the channel's sound and the radio's turn-on or turn-off triggers fire. A radio that is on channel 0 plays the turn-off sound and fires its turn-off triggers when you switch it on. <span class="tech">(Enabling a radio calls `SetState(CurrentState)`, which plays the channel or turn-off sound and fires `OnTurnOn` or `OnTurnOff`.)</span>

### Limits

- The game removes the save ids of objects it creates while it runs, and such a radio is not saved. A radio that is created while the game runs can get another path in a later session. Use a path-style id for the current session only.
- No game code calls `EnableRadio` by itself, so it is not known whether a scene does. A scene can do it through a saved scene event, and that is still open. The player's use key changes the channel and does not call it. <span class="tech">(The player's use key calls `Radio.Use`, which only changes the channel.)</span>
- `SetEnabled` gives `false` and logs once for an id that is `null` or empty. It also gives `false` while a save is loading or being written, and when no radio has the id.
- You can't change a radio's channel. The player changes channels with the use key, and the game plays a channel sound when it does. This API only reads the channel. <span class="tech">(The framework has no call for `Radio.Use` or `Radio.SetState`.)</span>
- You can't turn a radio off with the turn-off sound. Only switching it on or off is offered. <span class="tech">(The framework has no call for `Radio.TurnOff`.)</span>
- You can't control the game's music or soundscape here. See the [Audio page](audio.md).

## Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Errors are written to the framework's main log, `BepInEx\logs\ObenseuerCore.log`.

Everything not listed in this reference is internal and may change without notice.

### If something fails to set up

The framework changes some of the game's methods when it starts, so it can tell your mod when something happens. This part of the API changes none of them and has no events, so there is nothing that can fail to set up.

### When you can call it

- Call everything from your mod's normal code (OnLoad, event handlers, Update), not from a background thread you started. The game only works safely on one thread, the main thread (see the [glossary](../glossary.md#main-thread)).
- It is safe to call any of it at any time, even on the main menu. With no save loaded, `All()` gives an empty list, `Enabled` gives `false`, `State` gives `0` and `SetEnabled` gives `false`.
- An error from the game is caught and logged once, and you get the plain default. The first error of each member goes to the log.
