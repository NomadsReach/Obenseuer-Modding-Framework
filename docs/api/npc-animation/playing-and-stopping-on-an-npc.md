---
title: Playing and stopping on an NPC
parent: NPC animation
grand_parent: People
nav_order: 2
---

# Playing and stopping on an NPC

This is the full list of what the API gives you. In every code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

```csharp
public static class NpcAnimation
{
    public static bool Play(string modId, int npcId, AnimationClip clip, bool loop, float fadeSeconds = 0.2f);   // starts a clip on an NPC
    public static bool Stop(string modId, int npcId, float fadeSeconds = 0.2f);                                  // fades the clip out
    public static bool IsPlaying(int npcId);                                                                    // true while a clip runs on the NPC
    public static int StopAll(string modId);                                                                    // ends all clips of your mod; gives how many
}
```

In these calls, `modId` is the id of your mod, `loop` says whether the clip repeats, and `fadeSeconds` is how long the fade takes. The default fade is a fifth of a second.

- **A clip plays only on an NPC that is in the scene, switched on, and alive.** For an NPC that is far away, switched off, or dead, `Play` gives `false`.
  - Technical: the NPC needs a loaded scene instance that is active in the hierarchy and not dead. `Play` also gives `false` for an NPC with no `Animator` and while a save is loading or being written.
- **`Play` starts your clip and fades it in over `fadeSeconds`.** A fade of `0` starts it at once.
- **There is one clip for each NPC.** If your mod plays a new clip on an NPC that already has one of yours, the old clip fades into the new one over `fadeSeconds`. A fade of `0` switches at once. If another mod's clip is playing, `Play` gives `false`. The exception is a clip of another mod that is already fading out, which is replaced.
  - Technical: a `Play` while a crossfade is still running first jumps to the newer clip, then crossfades from it.
- **The fade of a clip that plays once is at most half the clip's length.** This keeps the fade from taking longer than the clip.
- **With `loop` set to `true`, the clip repeats until you stop it.** With `loop` set to `false`, it plays once. It starts to fade out `fadeSeconds` before its last frame, so it ends on the last frame, and then it stops.
  - Technical: a clip that already repeats in Unity (`isLooping`) keeps repeating. For a clip that does not, the framework loops the clip's time at its length.
- **`Stop` fades your clip out over `fadeSeconds`, and the NPC goes back to its own animation.** A fade of `0` ends it at once. Only the mod that started the clip can stop it, so `Stop` gives `false` for another mod's clip and when nothing is playing.
- **A second `Stop` on a clip that is already fading out gives `false`.** The exception is a fade of `0`, which ends the clip at once and gives `true`.
- **`IsPlaying` gives `true` while a clip runs and is not fading out.** It gives `false` once the clip starts to fade out, after `Stop`, after a clip that plays once has ended, and for an NPC with no clip. It only looks at NPC clips. A clip started with `PlayOn` is not seen by it.
- **`StopAll` ends every clip your mod has started, at once and without a fade.** It gives how many clips it ended, counting both NPC clips and `PlayOn` clips. It still works while a save is loading.
