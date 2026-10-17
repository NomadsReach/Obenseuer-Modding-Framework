---
title: Taking and releasing an NPC
parent: NPC control
grand_parent: People
nav_order: 2
---

# Taking and releasing an NPC

This is the full list of what the API gives you, one section for each part. In every code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

Every call takes `modId`, the id of your mod. The framework uses it to remember which mod holds which NPC (see [Owner](../../glossary.md#owner) in the glossary).

```csharp
[Flags]
public enum NpcControlFlags
{
    None = 0,       // hold the NPC but change nothing
    Sight = 1,      // the NPC stops noticing anything around it
    Talk = 2,       // the player can no longer start a conversation with it
    Barks = 4,      // the game's own comments from it stop
    Fade = 8,       // it is no longer hidden when you are far away or look away
    Movement = 16,  // it stops walking and stays where it is
    All = Sight | Talk | Barks | Fade | Movement   // every part above
}

public static class NpcControl
{
    public static bool IsAvailable { get; }    // false when the NPC control feature was turned off

    public static bool Take(string modId, int npcId, NpcControlFlags flags = NpcControlFlags.All);   // takes the NPC; true on success
    public static bool Release(string modId, int npcId);     // gives the NPC back; false when your mod holds nothing on it
    public static int ReleaseAll(string modId);              // gives back every NPC your mod holds; returns how many

    public static bool IsTaken(int npcId);       // true while any mod holds the NPC
    public static string TakenBy(int npcId);     // the id of the mod that holds it; null when it is free
}
```

- **`Take(modId, npcId, flags)` takes the NPC for your mod and switches off the parts named in `flags`.** It returns `true` when it worked. By default it switches off every part.
- **`Take` returns `false` when it can't take the NPC.** That happens when another mod already holds it, when the NPC is unknown, dead or in a different scene, when it is switched off for any other reason than being far away, while a save is loading or being written, and when the feature group is turned off.
- **Only one mod can hold an NPC at a time.** Another mod's `Take` on the same NPC returns `false` until the mod that holds it releases it.
- **Calling `Take` again with the same mod changes the parts you hold.** A flag that is no longer in the set is put back. A new flag is switched on at once. It returns `true`.
- **`Take` with `None` holds the NPC without changing anything about it.** Other mods can't take it while you hold it.
- **`Release(modId, npcId)` gives the NPC back.** It puts back each thing your mod changed. It returns `false` when your mod holds nothing on that NPC.
- **`ReleaseAll(modId)` releases every NPC your mod holds.** It returns how many it released.
- **`IsTaken(npcId)` is `true` while any mod holds the NPC.** `TakenBy(npcId)` tells you which mod, or `null` when it is free.
- **`IsAvailable` is `false` when the feature group was turned off.** `Release` and the reads keep working, so a mod can still give back an NPC it took earlier.
