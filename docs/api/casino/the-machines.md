---
title: The machines
parent: Casino
grand_parent: Money and shops
nav_order: 2
---

# The machines

This is the full list of what the API gives you. In every code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

`Casino.Machines()` lists the machines in the scene you are in. Each one is a `CasinoMachineInfo`, a snapshot of the machine when you asked.

```csharp
public enum CasinoMachineKind
{
    Slot = 0,      // a slot machine
    Pajatso = 1    // a coin pusher
}

public sealed class CasinoMachineInfo    // a snapshot of one machine
{
    public string Id { get; }                 // the machine's id: its place in the scene, written the same way in every event
    public CasinoMachineKind Kind { get; }    // which kind of machine it is
    public int Stake { get; }                 // slot machine: the stake it is set to now; coin pusher: always 1 coin per launch
}
```

- **`Machines()` lists both kinds of machine that are switched on in the scene you are in.**
- **A machine's `Id` stays the same between calls and between play sessions, as long as the machine stays where it is in the scene.** It is also the id the events report, so you can match an event to a machine.
  - Technical: the id is the machine's save GUID when one of its components has one. The slot machine and the coin pusher have none in the game's scenes as far as the framework knows, so the id is the scene id: the scene name, a colon, then each object from the top down with its position number (for example `Bar:Room[2]/SlotMachine[0]`).
- **A slot machine's `Stake` changes when the player raises or lowers the stake.** Because a `CasinoMachineInfo` is a snapshot, ask again for the current stake.
