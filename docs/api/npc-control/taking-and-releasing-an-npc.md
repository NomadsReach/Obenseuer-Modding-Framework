---
title: Taking and releasing an NPC
parent: NPC control
grand_parent: People
nav_order: 2
---

# Taking and releasing an NPC

This is the full list of what the API gives you, one section for each part. Every call takes `modId`, the id of your mod. The framework uses it to remember which mod holds which NPC (see [Owner](../../glossary.md#owner) in the glossary).

<div class="api-type" markdown="1">
<p class="api-type-head"><code>NpcControlFlags</code> <span>which parts of the NPC to switch off</span></p>
<div class="api-pair" markdown="1">

```csharp
[Flags]
public enum NpcControlFlags
{
    None = 0,
    Sight = 1,
    Talk = 2,
    Barks = 4,
    Fade = 8,
    Movement = 16,
    All = Sight | Talk | Barks | Fade | Movement
}
```

| Value | Meaning |
|---|---|
| `None` | Hold the NPC but change nothing. |
| `Sight` | The NPC stops noticing anything around it. |
| `Talk` | The player can no longer start a conversation with it. |
| `Barks` | The game's own comments from it stop. |
| `Fade` | It is no longer hidden when you are far away or look away. |
| `Movement` | It stops walking and stays where it is. |
| `All` | Every part above. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>NpcControl</code> <span>take and release NPCs</span></p>
<div class="api-pair" markdown="1">

```csharp
public static class NpcControl
{
    public static bool IsAvailable { get; }

    public static bool Take(string modId, int npcId, NpcControlFlags flags = NpcControlFlags.All);
    public static bool Release(string modId, int npcId);
    public static int ReleaseAll(string modId);

    public static bool IsTaken(int npcId);
    public static string TakenBy(int npcId);
}
```

| Method | Returns | What it does |
|---|---|---|
| `Take(string modId, int npcId, NpcControlFlags flags = NpcControlFlags.All)` | `bool` | Takes the NPC and switches off the parts named in `flags`. `true` on success. |
| `Release(string modId, int npcId)` | `bool` | Gives the NPC back. `false` when your mod holds nothing on that NPC. |
| `ReleaseAll(string modId)` | `int` | Gives back every NPC your mod holds, and returns how many. |
| `IsTaken(int npcId)` | `bool` | `true` while any mod holds the NPC. |
| `TakenBy(int npcId)` | `string` | The id of the mod that holds the NPC. `null` when it is free. |

| Property | Type | What it means |
|---|---|---|
| `IsAvailable` | `bool` | `false` when the NPC control feature was turned off. |

</div>
</div>

## Things to know

- `Take` switches off every part by default, or only the parts you name in `flags`.
- Only one mod can hold an NPC at a time. Another mod's `Take` on the same NPC returns `false` until the mod that holds it releases it.
- Calling `Take` again with the same mod changes the parts you hold. A flag that is no longer in the set is put back. A new flag is switched on at once. It returns `true`.
- `Take` with `None` holds the NPC without changing anything about it. Other mods can't take it while you hold it.
- `Release` puts back each thing your mod changed.
- `ReleaseAll` returns how many NPCs it released.
- `TakenBy` tells you which mod holds the NPC.
- `IsAvailable` is `false` when the feature group was turned off. `Release` and the reads keep working, so a mod can still give back an NPC it took earlier.

## Limits

- `Take` returns `false` when it can't take the NPC. That happens when another mod already holds it, when the NPC is unknown, dead or in a different scene, when it is switched off for any other reason than being far away, while a save is loading or being written, and when the feature group is turned off.
