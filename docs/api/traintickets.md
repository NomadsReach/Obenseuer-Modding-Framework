---
title: Train tickets API
parent: API reference
nav_order: 84
---

# Train tickets API

Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Patch owner: `obenseuer.moddingframework.traintickets`.
Files: `src/Api/TrainTickets.cs`, `src/Api/TrainTicketsPatches.cs`.

This area covers the ticket machine only (`InteractableTicketMachine`). `TrainShop` sells whole trains through `BaseShop` and has no ticket logic, so it is not part of this area.

## If a hook cannot be installed

The framework installs the `BuyTicket` and `TakeTicket` hooks under one owner. If either is missing, both are removed, `CorePatcher.IsDisabled("obenseuer.moddingframework.traintickets")` becomes true, and one warning is logged to `BepInEx\logs\obenseuer.moddingframework.traintickets.log`. `All` keeps working; `Bought` and `Taken` stop.

## Threading and safety

- Main thread only.
- `TrainTickets.IsAvailable` is false when the hooks were not installed or the owner was disabled.
- Every member is safe at any time. With no save loaded, or on the main menu, `All` returns an empty list.
- There are no writes. Nothing throws to the caller.
- Events are raised synchronously from the game's `BuyTicket` and `TakeTicket`. A handler that throws is logged and the other handlers still run.

## Machines

```csharp
public static List<TicketMachineInfo> All();

public sealed class TicketMachineInfo
{
    public string Id { get; }              // the machine's GUID, or its scene path when it has none
    public int Price { get; }              // the ticket price the machine charges (InteractableTicketMachine.ticketPrice)
    public MoneyCurrency Currency { get; } // the currency it charges in (InteractableTicketMachine.currency)
}
```

- `All` lists the active `InteractableTicketMachine` objects in the active scene. Inactive machines are not listed (Unity's default for `FindObjectsByType`).
- `Price` and `Currency` are the private fields the game charges from (`InteractableTicketMachine`), read as they are. The machine's own purchase path uses the same values.

## Events

```csharp
public static event Action<TicketBoughtArgs> Bought;
public static event Action<TicketTakenArgs> Taken;

public sealed class TicketBoughtArgs
{
    public string MachineId { get; }       // the machine, as TicketMachineInfo.Id
    public int Price { get; }              // the price charged
    public MoneyCurrency Currency { get; } // the currency charged
}

public sealed class TicketTakenArgs
{
    public string MachineId { get; }       // the machine, as TicketMachineInfo.Id
    public int ItemId { get; }             // the ticket's item id (ItemReference.ID)
}
```

- `Bought` fires when `BuyTicket` takes the money and starts the machine's sale. A purchase the player cannot afford (`Money.Buy` returns false) fires nothing.
- The money side is `Cash.MoneyChanged` ([economy.md](economy.md)). `Bought` is the ticket side of the same purchase.
- `Taken` fires after `TakeTicket` adds the ticket to the player's inventory. `TakeTicket` always adds it, whether or not the game's own check allowed the button press.

## Ceilings and risks

- `Bought` depends on the machine's `hasTicket` flag changing inside `BuyTicket`. The flag is set at the start of `BuyTicketDelay`, before its first `yield`, and Unity runs a coroutine synchronously up to its first `yield`. If the machine's GameObject is inactive, `StartCoroutine` throws and nothing fires.
- `Taken` fires when `TakeTicket` is called directly with no ticket waiting. `TakeTicket` itself does not check `hasTicket`, only the game's `Interact` does. A mod that calls `TakeTicket` directly gets an event for the ticket the game gives.
- No price setter. The price is a private field with no game write path except its serialized default, so none is exposed.

## Save loads

The framework does not gate this event on a save load. A handler that should ignore events raised while a save loads can check `SaveData.Busy`, which is true while a save is loading or being written.
