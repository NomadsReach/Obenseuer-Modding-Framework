---
title: Turning the power back on
parent: Flood gates
grand_parent: Objects and devices
nav_order: 3
---

# Turning the power back on

What happens when you switch a gate's power back on depends on what it was doing when the power went off. These details are for mods that cut power in the middle of a move.

- **Turning the power off in the middle of a move stops the move and turns the lights off.** The gate stays marked as moving.
  - Technical: `TurnOff` calls `StopCoroutines` (it stops the travel and timer routines) and does not clear `isBlinking`.
- **Turning the power back on after that either turns the gate around or makes it jump to the end of its move.** A gate with the game's default setting turns around and travels the other way. A gate whose "press while blinking" setting is off jumps at once to the end of the move it was making, with no travel. Where it ends up in the world after that jump is not known.
  - Technical: with `allowPressOnBlinking` true (the default), `TurnOn` calls `OpenFloodGate` or `CloseFloodGate`, which raises `Changed`. With it false, `TurnOn` calls `FullyClosed` or `FullyOpen`, which sets the rest light, fires the closed or open relay and clears `isBlinking`, so the gate reads as at the end of the move at once.
- **Turning the power on fires the game's signal for the state the gate is in.** A closed gate fires its "closed" signal, an open one its "open" signal, and a gate in an alarm fires its alarm signal and restarts the alarm. Turning the power off fires no signal; it stops the movement and turns the lights off.
  - Technical: the signals are the relays `onClosed`, `onOpen` and `onAlert`.
- **A gate whose power was cut in the middle of a move can move without power after a load.** If you save in that state, the game restarts the move when the save loads, without checking the power. Nothing in the game's code calls `TurnOn` or `TurnOff` by itself, so the game reaches them only through its scene wiring, which a mod cannot see.
  - Technical: `OnLoadingGame` starts `Open()` or `Close()` when the saved `open` or `closed` flag is set, without checking `powerOff`.
