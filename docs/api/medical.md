---
title: Medical
parent: Player life and skills
grand_parent: API reference
has_children: true
nav_order: 5
---

# Medical

Status: experimental.

## What it is

The doctor in the game sells treatments: painkillers, bandages, medicine for fungal growths, help for addictions, mental health and radiation, and a treatment that heals the player's wounds. This API lets your mod list the treatments the doctor sells with their prices, check whether the player can buy one right now, buy one for the player, and get told when any treatment is bought.

## When to use it

- Show the doctor's prices somewhere else, for example in your own overlay.
- React when the player buys a treatment, for example to give a reward or a hint.
- Buy a treatment for the player from your mod while the doctor's screen is open.

## Quick example

This mod writes a log line whenever the player buys a treatment. Once a save is loaded, it also logs every treatment of the doctors in the scene with its price.

```csharp
using ObenseuerCore;

[ModInfo("example.medicalwatch", "Medical Watch", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class MedicalWatch : FrameworkMod
{
    protected override void OnLoad()
    {
        Medical.Treated += args => Log.Info("Bought " + args.Kind + " for " + args.Price + " " + args.Currency);
        Events.WhenReady(() =>
        {
            foreach (MedicalTreatmentInfo treatment in Medical.Treatments())
                Log.Info(treatment.Kind + ": " + treatment.Price + " " + treatment.Currency + ", offered now: " + treatment.Available);
        });
    }
}
```

`Treated` is an event: the framework calls your code each time it happens. `Events.WhenReady` runs your code once a save is loaded, or at once if one already is. See [Using events](../guides/getting-started/using-events.md). The list is empty when the scene has no doctor.
