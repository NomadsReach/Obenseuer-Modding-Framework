---
title: Train tickets
parent: Places and travel
grand_parent: API reference
nav_order: 16
---

# Train tickets

This page uses `GameObject` from Unity. The [glossary](../glossary.md#types-that-come-from-unity) says what each one is.
{: .unity-types}

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

This is the full list of what the API gives you, one section for each part. Text in brackets at the end of a note is extra detail for experienced modders, and you can skip it on a first read.

### Machines

Each ticket machine is a `TicketMachineInfo`, a snapshot of it at the moment you asked.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>TrainTickets</code> <span>lists ticket machines and reports sales</span></p>
Use `All()` to list the ticket machines, and the `Bought` and `Taken` events to hear when a ticket is bought or taken.

<div class="api-pair" markdown="1">

```csharp
public static class TrainTickets
{
    public static List<TicketMachineInfo> All();
    public static bool IsAvailable { get; }
    public static event Action<TicketBoughtArgs> Bought;
    public static event Action<TicketTakenArgs> Taken;
}
```

| Method | Returns | What it does |
|---|---|---|
| `All()` | `List<TicketMachineInfo>` | Every ticket machine in the scene you are in. |

| Property | Type | What it means |
|---|---|---|
| `IsAvailable` | `bool` | `false` when the Bought and Taken events could not be set up. |

| Event | Gives you | When it fires |
|---|---|---|
| `Bought` | `TicketBoughtArgs` | The player paid for a ticket. |
| `Taken` | `TicketTakenArgs` | A ticket was taken from a machine. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>TicketMachineInfo</code> <span>a snapshot of one ticket machine</span></p>
`All()` gives you one of these for each ticket machine, with its `Price` and `Currency`. It is a snapshot, so ask again for fresh values. <span class="tech">(It reads the game's `InteractableTicketMachine`.)</span>

<div class="api-pair" markdown="1">

```csharp
public sealed class TicketMachineInfo
{
    public string Id { get; }
    public int Price { get; }
    public MoneyCurrency Currency { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | Names this machine. The first note below says where the name comes from. |
| `Price` | `int` | The price of one ticket. |
| `Currency` | `MoneyCurrency` | The money the machine charges in: OC or RM. |

</div>
</div>

### Events

<div class="api-type" markdown="1">
<p class="api-type-head"><code>TicketBoughtArgs</code> <span>what you get when a ticket is bought</span></p>
`Bought` gives you this when the player buys a ticket. `MachineId` names the machine, and `Price` and `Currency` are its ticket price.

<div class="api-pair" markdown="1">

```csharp
public sealed class TicketBoughtArgs
{
    public string MachineId { get; }
    public int Price { get; }
    public MoneyCurrency Currency { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `MachineId` | `string` | The machine, as `TicketMachineInfo.Id`. |
| `Price` | `int` | The price that was charged. |
| `Currency` | `MoneyCurrency` | The money it was charged in. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>TicketTakenArgs</code> <span>what you get when a ticket is taken</span></p>
`Taken` gives you this when the player takes a ticket out of a machine. `ItemId` is the item id of that ticket.

<div class="api-pair" markdown="1">

```csharp
public sealed class TicketTakenArgs
{
    public string MachineId { get; }
    public int ItemId { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `MachineId` | `string` | The machine, as `TicketMachineInfo.Id`. |
| `ItemId` | `int` | The item id of the ticket the player received. |

</div>
</div>

### Things to know

- A machine's `Id` names one ticket machine in the scene you are in. If the game gave the machine a save id, that is the id. If not, the id is the machine's place in the scene, and it changes if someone edits the objects around it. <span class="tech">(the id is the object's GUID when it has one, otherwise a scene path such as `Scene:Root[0]/Child[2]`)</span>
- `All()` lists the ticket machines in the scene you are in. A machine that is switched off is not listed. <span class="tech">(it uses `FindObjectsByType`, which skips inactive objects by default)</span>
- `Price` and `Currency` are what the machine really charges. The machine's own buying code uses the same values. <span class="tech">(they are read from the private `InteractableTicketMachine.ticketPrice` and `currency` fields)</span>
- `Bought` fires when the machine takes the money and starts printing the ticket. If the player can't afford the ticket, nothing fires. <span class="tech">(it is raised after the game's `BuyTicket` when the machine's `hasTicket` flag changed from false to true)</span>
- `Bought` is the ticket half of a purchase, and the money half is `Cash.MoneyChanged`. See the [economy page](economy/money-cash.md#money-cash).
- `Taken` fires when a ticket is added to the player's inventory. It fires every time `TakeTicket` runs, whether or not the game's own check allowed the button press. <span class="tech">(`TakeTicket` itself does not check `hasTicket`, only the game's `Interact` does; a mod that calls `TakeTicket` directly gets an event for the ticket the game gives)</span>

### Limits

- You can't change a ticket price. The game sets the price when the map is built and has no way to change it while the game runs, so no price setter is offered. <span class="tech">(the price is a private field with no game write path except its serialized default)</span>
- `Bought` only works when the machine's ticket flag changes during the buy call. If the machine's object is switched off when the player buys, the game's buying code stops with an error and nothing fires. <span class="tech">(`hasTicket` is set at the start of `BuyTicketDelay`, before its first `yield`, and Unity runs a coroutine synchronously up to its first `yield`; if the machine's GameObject is inactive, `StartCoroutine` throws)</span>

## Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\obenseuer.moddingframework.traintickets.log`.

Everything not listed in this reference is internal and may change without notice.

The framework changes some of the game's methods when it starts, so it can tell your mod when a ticket is bought or taken. These changes are grouped as a feature group. This page's feature group is `obenseuer.moddingframework.traintickets`, the name you will see in the log.

### When you can call it

- Call everything from your mod's normal code, not from a background thread you started. The game and Unity only work safely on one thread, the main thread (see the [glossary](../glossary.md#main-thread)). This goes for every member on this page and for the code that handles each event.
- It is safe to call any of it at any time, even on the main menu or while a save is loading. With no save loaded, `All` returns an empty list. Nothing here changes the game, and nothing throws.
- Your event handlers run inside the game's own call, one at a time. If one of your handlers throws an error, the framework logs it and still runs the other handlers. <span class="tech">(both events are raised synchronously from the game's `BuyTicket` and `TakeTicket`)</span>

### If something fails to set up

The framework changes some of the game's methods when it starts. If a game update breaks one of them, the framework removes all the changes of the [feature group](../glossary.md#feature-group) that depends on it, turns the group off, and writes error lines and a warning to the group's log file in `BepInEx\logs`. The rest of the framework keeps working.

- Both changes behind `Bought` and `Taken` are needed. If either one cannot be made, the whole feature group turns off, `TrainTickets.IsAvailable` becomes `false`, and the framework writes error lines and a warning to `BepInEx\logs\obenseuer.moddingframework.traintickets.log`. `All` keeps working. Only `Bought` and `Taken` stop. <span class="tech">(the changes are on `InteractableTicketMachine.BuyTicket` and `TakeTicket`; you can also check `CorePatcher.IsDisabled("obenseuer.moddingframework.traintickets")`)</span>

### While a save is loading

The framework does not hold back `Bought` or `Taken` while a save is loading. If your handler should ignore events raised during a load, check `SaveData.Busy` first. It is `true` while a save is loading or being written.
