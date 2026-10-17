---
title: Animal actions
parent: Animals and plants
grand_parent: API reference
has_children: true
nav_order: 1
---

# Animal actions

Status: experimental.

## What it is

This API lets your mod give orders to the animals around the player: make one sit, sleep, run, hunt something, or become tame or wild. You find an animal with [`Animals`](animals.md), then pass its `Id` here.

## When to use it

- Make a cat run for a few seconds.
- Put an animal to sleep, or keep it asleep until your mod lets it go.
- Send an animal after a target, such as another animal.
- Tame an animal, or make a tame one wild, and keep that choice after the player loads the save again.

## Quick example

This mod waits until the game is ready, finds the first cat in the scene you are in, and makes it run for 4 seconds.

```csharp
using ObenseuerCore;

[ModInfo("example.catrun", "Cat Run", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class CatRun : FrameworkMod
{
    protected override void OnLoad()
    {
        Events.WhenReady(() =>
        {
            AnimalInfo cat = Animals.All().Find(a => a.Kind == AnimalKind.Cat);
            if (cat != null && AnimalActions.Run(Mod.Id, cat.Id, 4f))
                Log.Info("The cat is running.");
        });
    }
}
```

Every order takes your mod's id as its first argument. `Mod.Id` is the id you gave in `[ModInfo]`. An order returns `true` when it was given and `false` when it was not. See [Using events](../guides/getting-started/using-events.md) for more on `WhenReady`.
