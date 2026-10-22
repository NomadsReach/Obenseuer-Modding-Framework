---
title: Common tasks
parent: NPC animation
grand_parent: People
nav_order: 1
---

# Common tasks

This page uses `AnimationClip`, `Animator`, `GameObject`, `Vector3` from Unity. The [glossary](../../glossary.md#types-that-come-from-unity) says what each one is.
{: .unity-types}

Every block on this page is a small mod of its own. Copy one whole, or keep its `using` lines and its two attributes and move the calls into your own mod. All of them do their work inside `Events.WhenReady`, because a clip needs a scene that is loaded. Each block picks the first living NPC in `Npcs.All()`, and the blocks that play a clip find it among the clips the game has loaded by the name `MyClip`, so change that name to your own.

Keep an NPC in place while a clip plays, and let it go afterwards. See [NPC movement](../npc-movement.md).

```csharp
using ObenseuerCore;
using UnityEngine;

[ModInfo("example.npcanimhold", "NPC Animation Hold", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class NpcAnimHold : FrameworkMod
{
    protected override void OnLoad()
    {
        Events.WhenReady(() =>
        {
            NpcInfo npc = null;
            foreach (NpcInfo found in Npcs.All())
                if (found.Alive) { npc = found; break; }
            if (npc == null) return;

            AnimationClip clip = null;
            foreach (AnimationClip found in Resources.FindObjectsOfTypeAll<AnimationClip>())
                if (found.name == "MyClip" && !found.legacy) { clip = found; break; }
            if (clip == null) return;

            NpcMovement.SetActivityEnabled(Mod.Id, npc.Id, false);
            NpcAnimation.Play(Mod.Id, npc.Id, clip, true, 0.3f);

            NpcAnimation.Stop(Mod.Id, npc.Id, 0.3f);
            NpcMovement.SetActivityEnabled(Mod.Id, npc.Id, true);
        });
    }
}
```

`Play` and `Stop` take your mod's id, so each mod can only stop its own clip. `Play` gives `false` when it refuses, for the reasons in the [play limits](playing-and-stopping-on-an-npc.md#limits).

Play a clip once, with no fade. `IsPlaying` tells you whether a clip is running on the NPC.

```csharp
using ObenseuerCore;
using UnityEngine;

[ModInfo("example.npcanimonce", "NPC Animation Once", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class NpcAnimOnce : FrameworkMod
{
    protected override void OnLoad()
    {
        Events.WhenReady(() =>
        {
            NpcInfo npc = null;
            foreach (NpcInfo found in Npcs.All())
                if (found.Alive) { npc = found; break; }
            if (npc == null) return;

            foreach (AnimationClip clip in Resources.FindObjectsOfTypeAll<AnimationClip>())
            {
                if (clip.name != "MyClip" || clip.legacy) continue;
                if (!NpcAnimation.Play(Mod.Id, npc.Id, clip, false, 0f))
                {
                    Log.Warn("The clip was refused. See the play limits.");
                    return;
                }
                Log.Info("The clip is running: " + NpcAnimation.IsPlaying(npc.Id) + ".");
                return;
            }
        });
    }
}
```

Play a clip on a figure your mod owns, such as an extra. `SpawnExtra` gives back the figure's object, and `PlayOn` takes the `Animator` from it. `StopOn` fades that clip out again.

```csharp
using ObenseuerCore;
using UnityEngine;

[ModInfo("example.npcanimextra", "NPC Animation Extra", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class NpcAnimExtra : FrameworkMod
{
    protected override void OnLoad()
    {
        Events.WhenReady(() =>
        {
            int sourceId = 0;
            foreach (CharacterInfo character in Characters.All())
                if (!character.IsPlayer) { sourceId = character.Id; break; }

            Vector3 spot = PlayerState.Position + Vector3.forward * 3f;
            GameObject extra = ModNpcs.SpawnExtra(Mod.Id, "guard", sourceId, sourceId, spot, 180f);
            if (extra == null)
            {
                Log.Warn("The extra was refused. See the extra limits.");
                return;
            }

            AnimationClip clip = null;
            foreach (AnimationClip found in Resources.FindObjectsOfTypeAll<AnimationClip>())
                if (found.name == "MyClip" && !found.legacy) { clip = found; break; }

            Animator animator = extra.GetComponentInChildren<Animator>();
            if (animator == null || clip == null) return;

            if (!NpcAnimation.PlayOn(Mod.Id, animator, clip, true))
                Log.Warn("The clip was refused on the extra.");
            else
                NpcAnimation.StopOn(Mod.Id, animator, 0.3f);
        });
    }
}
```

`SpawnExtra` gives `null` for the reasons in the [extra limits](../modnpcs/extras.md#limits), and `PlayOn` follows the same rules as `Play`.

Stop every clip your mod has started. `StopAll` gives how many it ended.

```csharp
using ObenseuerCore;
using UnityEngine;

[ModInfo("example.npcanimstopall", "NPC Animation Stop All", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class NpcAnimStopAll : FrameworkMod
{
    protected override void OnLoad()
    {
        Events.WhenReady(() =>
        {
            int stopped = NpcAnimation.StopAll(Mod.Id);
            Log.Info("Ended " + stopped + " clips.");
        });
    }
}
```
