---
title: Instruments
parent: Objects and devices
grand_parent: API reference
has_children: true
nav_order: 8
---

# Instruments

## What it is

The only musical instrument in the game is the guitar. The player holds it in a hand and strums it with the Primary Use key, and people nearby may comment on the playing. This API lists the guitars in the scene you are in, plays a single strum or a timed run of strums from your mod, holds a long note, and tells you when a strum is played and when a witness comments on it.

## When to use it

- Do something when the player strums a guitar, for example count strums or give a reward.
- React when a person nearby comments on the playing.
- Play a short tune on the guitar the player is holding, for example as part of a quest or a gag.

## Quick example

This mod logs each strum and each comment a nearby person makes on the playing. Hold a guitar in the game and strum it to see the lines in the log.

```csharp
using ObenseuerCore;

[ModInfo("example.guitarwatch", "Guitar Watch", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class GuitarWatch : FrameworkMod
{
    protected override void OnLoad()
    {
        Instruments.GuitarPlayed += args => Log.Info("Strum on guitar " + args.GuitarId);
        Instruments.GuitarCommented += args => Log.Info("NPC " + args.NpcId + " commented, positive=" + args.Positive);
    }
}
```

`GuitarPlayed` and `GuitarCommented` are events: the framework calls your code each time they happen. See [Using events](../guides/getting-started/using-events.md) for more on events.
