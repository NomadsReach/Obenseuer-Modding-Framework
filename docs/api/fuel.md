---
title: Fuel
parent: Money and shops
grand_parent: API reference
has_children: true
nav_order: 5
---

# Fuel

## What it is

This page covers two things about fuel. The first is the gas stations: the pumps where the player fills up and pays. The API lists the stations in the scene you are in, tells you each station's current price, and tells your mod when the player pays for fuel. The second is the list of items that burn as fuel in a generator or similar machine, with how long each one burns and what it leaves behind.

The minitrain's fuel tank is not on this page. `Trains.Minitrain` already reports its `Fuel` and `Capacity` (see [Trains](trains.md)).

## When to use it

- Show or log the fuel price at each gas station.
- React when the player buys fuel, for example to give a reward or keep a record of what was spent.
- Work out how long an item burns in a generator, or which items give off ash or another leftover.

## Quick example

This mod logs a line when the player pays for fuel. Once a save is loaded, it logs the price at each gas station and counts the items that can be used as fuel.

```csharp
using ObenseuerCore;

[ModInfo("example.fuelwatch", "Fuel Watch", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class FuelWatch : FrameworkMod
{
    protected override void OnLoad()
    {
        FuelStations.FuelBought += args => Log.Info("Paid " + args.Cost + " " + args.Currency + " for " + args.Amount + " fuel at " + args.StationId);
        Events.WhenReady(() =>
        {
            foreach (FuelStationInfo station in FuelStations.All())
                Log.Info("Station " + station.Id + " charges " + station.Price + " " + station.Currency + " a unit.");
            Log.Info(FuelTypes.All().Count + " items can be burned as fuel.");
        });
    }
}
```

`FuelBought` is an event: the framework calls your code each time it happens. See [Using events](../guides/getting-started/using-events.md) for more on events and `WhenReady`.
