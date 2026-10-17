---
title: Rules for taking an NPC
parent: NPC control
grand_parent: People
nav_order: 4
---

# Rules for taking an NPC

## Things to know

- Your claim ends by itself when the NPC dies. The framework notices on its next call, so a later `Release` returns `false` and `IsTaken` is `false`. If your mod calls `Take` again with the same NPC id after the NPC moved to a new body in the game, the old claim is dropped and the new body is taken. That drop puts back the same things `Release` does, which for a dead NPC is the fade distance only.
- Things are put back only when the game has not changed them since. The fade distance is put back only while it still holds the value the framework set. `Movement` is the one flag that sets a value to hold the NPC, so releasing it writes the walking switch back. It does not turn movement on while the NPC is sitting in a chair or asleep, because the game turns movement on again when the NPC stands up or wakes. `Sight`, `Talk` and `Barks` change no value. The framework checks their flags whenever the game asks, so releasing them takes effect at once.
- `Release` and `ReleaseAll` always work. The game's save does not write anything the framework changes, so the framework does nothing special at save time.
- This API raises no events.

## Limits

- Only NPCs that are really in the scene can be taken. `Take` needs a loaded NPC that is alive. An NPC that is switched off only because the game hid it for being far away can be taken. With the `Fade` flag, an NPC that is switched off only because the camera hid it can be taken too.
- An NPC switched off for any other reason is refused, and so is a camera-hidden NPC taken without `Fade`. An unknown, dead or off-scene NPC is refused.
- `Take` is refused while a save loads or is being written.
- A bad `modId` or a flags value that is not a mix of the members above throws an `ArgumentException`. A game failure is logged once and the call returns `false`. Nothing else throws. <span class="tech">(The `modId` is checked by the framework's mod id rules. `flags` outside `NpcControlFlags.All` are refused.)</span>
