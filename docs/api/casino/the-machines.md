---
title: The machines
parent: Casino
grand_parent: Money and shops
nav_order: 2
---

# The machines

This is the full list of what the API gives you. Text in brackets at the end of a note is extra detail for experienced modders, and you can skip it on a first read.

`Casino.Machines()` lists the machines in the scene you are in. Each one is a `CasinoMachineInfo`, a snapshot of the machine when you asked.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>CasinoMachineKind</code> <span>the kinds of machine</span></p>

`CasinoMachineInfo.Kind` holds one of these, and `Casino.SetPayoutFactor` takes one. <span class="tech">(It stands for the game's `SlotMachineGameplay` and `PajatsoGameplay` components.)</span>

<div class="api-pair" markdown="1">

```csharp
public enum CasinoMachineKind
{
    Slot = 0,
    Pajatso = 1
}
```

| Value | Meaning |
|---|---|
| `Slot` | A slot machine. |
| `Pajatso` | A coin pusher. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>CasinoMachineInfo</code> <span>a snapshot of one machine</span></p>

`Casino.Machines()` returns a list of these, one for each machine it finds in the scene you are in.

<div class="api-pair" markdown="1">

```csharp
public sealed class CasinoMachineInfo
{
    public string Id { get; }
    public CasinoMachineKind Kind { get; }
    public int Stake { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | The machine's id: its place in the scene, written the same way in every event. |
| `Kind` | `CasinoMachineKind` | Which kind of machine it is. |
| `Stake` | `int` | Slot machine: the stake it is set to now. Coin pusher: always 1 coin per launch. |

</div>
</div>

## Things to know

- `Machines()` lists both kinds of machine that are switched on in the scene you are in.
- A machine's `Id` stays the same between calls and between play sessions, as long as the machine stays where it is in the scene. It is also the id the events report, so you can match an event to a machine. <span class="tech">(The id is the machine's save GUID when one of its components has one. The slot machine and the coin pusher have none in the game's scenes as far as the framework knows, so the id is the scene id: the scene name, a colon, then each object from the top down with its position number (for example `Bar:Room[2]/SlotMachine[0]`).)</span>
- A slot machine's `Stake` changes when the player raises or lowers the stake. Because a `CasinoMachineInfo` is a snapshot, ask again for the current stake.
