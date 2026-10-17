---
title: Housing
parent: Places and travel
grand_parent: API reference
has_children: true
nav_order: 4
---

# Housing

## What it is

The tenement is the building where the player lives. It has apartments, and tenants who move in, pay rent and can be evicted. The player can also place and take furniture in a room using build mode. This API has two halves.

- **`Housing`** is about the apartments and the people in them: who lives where, what rent they pay, when someone moves in or out, and how to start or stop an eviction.
- **`Building`** is about the furniture the player places: list it, place a piece, move one, take one out, unlock furniture blueprints, and find out which building area the player is standing in.

Cleaning an apartment is on the [Cleaning](cleaning.md) page. Renovations (contractor upgrades) are on the [Renovations](renovations.md) page. Tenant behaviour such as starting a tenement event is on the [Tenants](tenants.md) page.

## When to use it

- Log or react when a tenant moves in or out, or when rent comes in.
- Start an eviction, or call it off, as part of a story.
- Place a piece of furniture, or tidy a room, from your mod.
- React when the player puts furniture down or takes it out.
- Re-attach your mod's behaviour to its own furniture after a save loads.

## Quick example

This mod writes a log line when a tenant moves in or out and when rent comes in. Once a save is loaded, it also logs how many apartments are vacant.

```csharp
using ObenseuerCore;

[ModInfo("example.tenantwatch", "Tenant Watch", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class TenantWatch : FrameworkMod
{
    protected override void OnLoad()
    {
        Housing.TenantMovedIn += args => Log.Info(args.Tenant.Name + " moved into apartment " + args.ApartmentId);
        Housing.TenantMovedOut += args => Log.Info(args.Tenant.Name + " moved out of apartment " + args.ApartmentId);
        Housing.RentPaid += rent => Log.Info("Rent came in: " + rent.Oc + " OC and " + rent.Rm + " RM.");
        Events.WhenReady(() =>
        {
            int vacant = 0;
            foreach (ApartmentInfo apartment in Housing.AllApartments())
                if (apartment.Tenant == null) vacant++;
            Log.Info(vacant + " apartments are vacant.");
        });
    }
}
```

`TenantMovedIn`, `TenantMovedOut` and `RentPaid` are events: the framework calls your code each time they happen. `Events.WhenReady` waits until a save is loaded before it runs your code. See [Using events](../guides/getting-started/using-events.md). OC is Open Sewer Coins and RM is Real Money (see the glossary entries [OC](../glossary.md#oc) and [RM](../glossary.md#rm)).
