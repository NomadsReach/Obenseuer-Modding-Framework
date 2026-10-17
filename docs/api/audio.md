---
title: Audio
parent: Screen and UI
grand_parent: API reference
has_children: true
nav_order: 1
---

# Audio

Status: experimental.

## What it is

This API plays sound in the game. It can play the game's own sounds (the noise an item makes when you pick it up, and the game's sets of random sounds such as footsteps and scary noises). It can also play a sound of your own: a WAV file from your mod folder, or a sound your code builds from numbers. It reads the volume sliders from the options menu, lets you turn a volume up or down for the current session, and lets you change the background ambient sound of an area.

The game has no music player, so you can't play or stop the game's music tracks with this API. See [What you can't do (yet)](audio/what-you-can-t-do-yet.md#what-you-cant-do-yet).

## When to use it

- Play a short sound when something happens in your mod, for example a coin sound when the player is paid.
- Play your own sound effect from a WAV file that you ship with your mod.
- Make a simple sound in code, such as a beep, with no file at all.
- Read how loud the player has set the master, sound and music sliders.
- Switch the ambient sound of the area the player is in.

## Quick example

This mod builds a half-second beep in code and plays it once a save is loaded. The clip is made from 16-bit samples, then played through the game's `Effects` mixer group at half volume.

```csharp
using ObenseuerCore;
using UnityEngine;

[ModInfo("example.beep", "Beep", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class Beep : FrameworkMod
{
    protected override void OnLoad()
    {
        int rate = 22050;
        short[] samples = new short[rate / 2];
        for (int i = 0; i < samples.Length; i++)
            samples[i] = (short)(Mathf.Sin(2f * Mathf.PI * 440f * i / rate) * 8000f);

        AudioClip clip = Sounds.FromPcm16("beep", samples, 1, rate);
        Events.WhenReady(() =>
        {
            bool played = Sounds.PlayClip(clip, "Effects", 0.5f);
            Log.Info("Beep played: " + played);
        });
    }
}
```

`WhenReady` runs your code once a save is loaded (see [Framework helpers](core-helpers/events-whenready.md#eventswhenready)). `PlayClip` returns `true` when the sound started. A project that uses `AudioClip` needs a reference to `UnityEngine.AudioModule.dll`, because that is where Unity keeps audio.
