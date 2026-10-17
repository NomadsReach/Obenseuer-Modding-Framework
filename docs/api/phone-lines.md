---
title: Phone lines
parent: Phones, mail and media
grand_parent: API reference
has_children: true
nav_order: 8
---

# Phone lines

## What it is

A phone line is one phone in the game world, the kind the player can walk up to and use. This API lets your mod list the phones in the scene, make a phone ring as if someone were calling, answer or decline a ring, start a call from a phone, check whether a phone is busy or ringing, and get told when a phone rings or a call starts or ends. The list of numbers a phone can dial is `Phones` on the [Phones page](phones.md). It is not repeated here. This page does not cover the dial pad on screen, and it does not let you hang up a call that is going on.

## When to use it

- Make a phone ring as part of a quest, with a number the player knows.
- Start a call from a phone yourself, for a story moment.
- React when the player starts or ends a phone call.
- Check whether a phone is free before you ring it.

## Quick example

This mod writes a log line when a phone rings, and when a call starts or ends. Once a save is loaded, it also logs how many phones the scene has.

```csharp
using ObenseuerCore;

[ModInfo("example.phonewatch", "Phone Watch", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class PhoneWatch : FrameworkMod
{
    protected override void OnLoad()
    {
        PhoneLines.Ringing += args => Log.Info("Phone " + args.Id + " is ringing, caller " + args.Number);
        PhoneLines.CallStarted += args => Log.Info("A call started on " + args.Id + " with " + args.Number);
        PhoneLines.CallEnded += args => Log.Info("A call ended on " + args.Id);
        Events.WhenReady(() =>
        {
            Log.Info("This scene has " + PhoneLines.All().Count + " phones.");
        });
    }
}
```

`Ringing`, `CallStarted` and `CallEnded` are events: the framework calls your code each time they happen. `Events.WhenReady` waits until a save is loaded before it runs your code. See [Using events](../guides/getting-started/using-events.md).
