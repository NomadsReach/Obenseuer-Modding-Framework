---
title: Train tickets
parent: Places and travel
grand_parent: API reference
nav_order: 16
---

# Train tickets

## What it is

A ticket machine is the machine where the player buys a train ticket. The player pays, the machine prints the ticket, and the player takes it. This API lets your mod list the ticket machines in the scene you are in, read the price each one charges, and get told when a ticket is bought and when it is taken.

It covers ticket machines only. The shop that sells whole trains is a different thing and is not part of this API.

## When to use it

- Show a message or write a log line when the player buys a ticket.
- React when the player picks up a ticket.
- Show what each ticket machine charges.

## Quick example

This mod writes a log line when a ticket is bought and when it is taken. When the game is ready, it also logs every ticket machine in the scene with its price.

```csharp
using ObenseuerCore;

[ModInfo("example.ticketwatch", "Ticket Watch", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class TicketWatch : FrameworkMod
{
    protected override void OnLoad()
    {
        TrainTickets.Bought += args => Log.Info("ticket bought for " + args.Price + " " + args.Currency);
        TrainTickets.Taken += args => Log.Info("ticket " + args.ItemId + " taken");
        Events.WhenReady(() =>
        {
            foreach (TicketMachineInfo machine in TrainTickets.All())
                Log.Info(machine.Id + " charges " + machine.Price + " " + machine.Currency);
        });
    }
}
```

`Bought` and `Taken` are events: the framework calls your code each time they happen. The currency is OC (Open Sewer Coins) or RM (Real Money), see the [glossary](../glossary.md#oc). See [Using events](../guides/getting-started/using-events.md) for more on events and `WhenReady`.

## Common tasks

Find the cheapest ticket machine in the scene.

```csharp
TicketMachineInfo cheapest = null;
foreach (TicketMachineInfo machine in TrainTickets.All())
    if (cheapest == null || machine.Price < cheapest.Price) cheapest = machine;
if (cheapest != null) Log.Info("cheapest: " + cheapest.Id + " at " + cheapest.Price);
```

Count the tickets the player buys.

```csharp
int bought = 0;
TrainTickets.Bought += args => { bought++; Log.Info("tickets bought so far: " + bought); };
```

Watch the money side of a ticket purchase too.

```csharp
Cash.MoneyChanged += args => Log.Info(args.Currency + " went from " + args.Before + " to " + args.After);
```

## Reference

This is the full list of what the API gives you, one section for each part. In every code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

### Machines

Each ticket machine is a `TicketMachineInfo`, a snapshot of it at the moment you asked.

```csharp
public static class TrainTickets
{
    public static List<TicketMachineInfo> All();   // every ticket machine in the scene you are in

    public static bool IsAvailable { get; }        // false when the Bought and Taken events could not be set up
}

public sealed class TicketMachineInfo    // a snapshot of one ticket machine
{
    public string Id { get; }               // names this machine; see the first bullet below
    public int Price { get; }               // the price of one ticket
    public MoneyCurrency Currency { get; }  // the money the machine charges in: OC or RM
}
```

- **A machine's `Id` names one ticket machine in the scene you are in.** If the game gave the machine a save id, that is the id. If not, the id is the machine's place in the scene, and it changes if someone edits the objects around it.
  - Technical: the id is the object's GUID when it has one, otherwise a scene path such as `Scene:Root[0]/Child[2]`.
- **`All()` lists the ticket machines in the scene you are in.** A machine that is switched off is not listed.
  - Technical: it uses `FindObjectsByType`, which skips inactive objects by default.
- **`Price` and `Currency` are what the machine really charges.** The machine's own buying code uses the same values.
  - Technical: they are read from the private `InteractableTicketMachine.ticketPrice` and `currency` fields.
- **You can't change a ticket price.** The game sets the price when the map is built and has no way to change it while the game runs, so no price setter is offered.
  - Technical: the price is a private field with no game write path except its serialized default.

### Events

```csharp
public static class TrainTickets
{
    public static event Action<TicketBoughtArgs> Bought;   // the player paid for a ticket
    public static event Action<TicketTakenArgs> Taken;     // a ticket was taken from a machine
}

public sealed class TicketBoughtArgs    // what you get when a ticket is bought
{
    public string MachineId { get; }        // the machine, as TicketMachineInfo.Id
    public int Price { get; }               // the price that was charged
    public MoneyCurrency Currency { get; }  // the money it was charged in
}

public sealed class TicketTakenArgs    // what you get when a ticket is taken
{
    public string MachineId { get; }       // the machine, as TicketMachineInfo.Id
    public int ItemId { get; }             // the item id of the ticket the player received
}
```

- **`Bought` fires when the machine takes the money and starts printing the ticket.** If the player can't afford the ticket, nothing fires.
  - Technical: it is raised after the game's `BuyTicket` when the machine's `hasTicket` flag changed from false to true.
- **`Bought` is the ticket half of a purchase, and the money half is `Cash.MoneyChanged`.** See the [economy page](economy/money-cash.md#money-cash).
- **`Taken` fires when a ticket is added to the player's inventory.** It fires every time `TakeTicket` runs, whether or not the game's own check allowed the button press.
  - Technical: `TakeTicket` itself does not check `hasTicket`, only the game's `Interact` does. A mod that calls `TakeTicket` directly gets an event for the ticket the game gives.

## Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\obenseuer.moddingframework.traintickets.log`.

Everything not listed in this reference is internal and may change without notice.

The framework changes some of the game's methods when it starts, so it can tell your mod when a ticket is bought or taken. These changes are grouped as a feature group. This page's feature group is `obenseuer.moddingframework.traintickets`, the name you will see in the log.

### When you can call it

- **Call everything from your mod's normal code, not from a background thread you started.** The game and Unity only work safely on one thread, the main thread (see the [glossary](../glossary.md#main-thread)). This goes for every member on this page and for the code that handles each event.
- **It is safe to call any of it at any time, even on the main menu or while a save is loading.** With no save loaded, `All` returns an empty list. Nothing here changes the game, and nothing throws.
- **Your event handlers run inside the game's own call, one at a time.** If one of your handlers throws an error, the framework logs it and still runs the other handlers.
  - Technical: both events are raised synchronously from the game's `BuyTicket` and `TakeTicket`.

### If something fails to set up

The framework changes some of the game's methods when it starts. If a game update breaks one of them, the framework removes all the changes of the [feature group](../glossary.md#feature-group) that depends on it, turns the group off, and writes error lines and a warning to the group's log file in `BepInEx\logs`. The rest of the framework keeps working.

- **Both changes behind `Bought` and `Taken` are needed.** If either one cannot be made, the whole feature group turns off, `TrainTickets.IsAvailable` becomes `false`, and the framework writes error lines and a warning to `BepInEx\logs\obenseuer.moddingframework.traintickets.log`. `All` keeps working. Only `Bought` and `Taken` stop.
  - Technical: the changes are on `InteractableTicketMachine.BuyTicket` and `TakeTicket`. You can also check `CorePatcher.IsDisabled("obenseuer.moddingframework.traintickets")`.

### Limits

- **`Bought` only works when the machine's ticket flag changes during the buy call.** If the machine's object is switched off when the player buys, the game's buying code stops with an error and nothing fires.
  - Technical: `hasTicket` is set at the start of `BuyTicketDelay`, before its first `yield`, and Unity runs a coroutine synchronously up to its first `yield`. If the machine's GameObject is inactive, `StartCoroutine` throws.

### While a save is loading

The framework does not hold back `Bought` or `Taken` while a save is loading. If your handler should ignore events raised during a load, check `SaveData.Busy` first. It is `true` while a save is loading or being written.
