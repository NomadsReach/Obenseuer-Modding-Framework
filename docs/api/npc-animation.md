---
title: NPC animation API
parent: API reference
nav_order: 49.1
---

# NPC animation API

Status: experimental.
Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Log owner: `obenseuer.moddingframework.npcanimation`.
Files: `src/Api/NpcAnimation.cs`.
`AnimationClip` and `Animator` are in `UnityEngine.AnimationModule`, so a mod project needs a reference to `UnityEngine.AnimationModule.dll`.

Ids are the NPC ids from the `Npcs` facade (`NpcInfo.Id`, `Npcs.All()`).

## Rules

- **Main thread only.** Call every member on Unity's main thread.
- **Live NPCs only.** A clip plays only on an NPC that has a loaded scene instance, is active and is alive. An off-screen NPC returns `false`.
- **Owner.** `Play`, `Stop`, `PlayOn`, `StopOn` and `StopAll` take the `modId` of your mod. An invalid `modId` throws `ArgumentException`.
- **Bad arguments.** A null clip throws `ArgumentNullException` named `clip`. A legacy clip (`AnimationClip.legacy` is true) throws `ArgumentException` named `clip`. A fade that is negative, NaN or infinite throws `ArgumentException` named `fadeSeconds`.
- **Game refusals.** Every other refusal returns `false` and nothing throws. The first failure of each member is logged at Error.
- **Saves.** `Play`, `Stop`, `PlayOn` and `StopOn` return `false` while a save loads or saves. `StopAll` still stops your clips.
- **PlayOn and StopOn** take an `Animator` in place of an NPC id. They are meant for extras and other figures your mod owns (see [mod NPC API](modnpcs.md), Extras). `PlayOn` accepts any active animator that has no scene NPC above it, so it also accepts the player's and props' animators. Playing on the player's rig is not supported. `PlayOn` returns `false` for an inactive animator, and for an animator that is under a scene NPC. The NPC's own animator is played with `Play` only. A null animator throws `ArgumentNullException` named `animator`, and a destroyed one returns `false`. Otherwise the rules above apply: the clip is one per animator, the fades are the same, and a Single scene load ends the clip.
- `Play` and `Stop` do not see a clip started by `PlayOn`, and `StopOn` does not see a clip started by `Play`. `IsPlaying` covers NPC clips only. `StopAll` ends clips of both kinds.

## Clips

```csharp
public static class NpcAnimation
{
    public static bool Play(string modId, int npcId, AnimationClip clip, bool loop, float fadeSeconds = 0.2f);
    public static bool Stop(string modId, int npcId, float fadeSeconds = 0.2f);
    public static bool IsPlaying(int npcId);
    public static int StopAll(string modId);
    public static bool PlayOn(string modId, Animator animator, AnimationClip clip, bool loop, float fadeSeconds = 0.2f);
    public static bool StopOn(string modId, Animator animator, float fadeSeconds = 0.2f);
}
```

- **The clip must match the NPC's rig.** Use a humanoid clip on a humanoid rig, or a generic clip whose bone paths exist on the NPC's model. A clip that does not match plays nothing on the bones it cannot find.
- **The NPC's own animation keeps running.** The clip is blended over the NPC's current pose from its own controller, by weight on the clip's output. A clip with a weight of 0.5 gives a half blend over the controller, not a full replacement.
- **Root motion.** A clip with root motion moves the NPC when its `Animator` has `applyRootMotion` on. Leave it off for a clip that should stay in place.
- **Play** starts `clip` on the NPC. It fades in over `fadeSeconds`. A fade of `0` applies at once.
- **One clip per NPC.** A `Play` from the same mod replaces the clip. The old clip crossfades into the new one over `fadeSeconds`. A fade of `0` switches at once. A `Play` from another mod while a clip is playing returns `false`, unless that clip is already fading out. A fading clip of another mod is replaced.
- A `Play` while a crossfade is still running first jumps to the newer clip, then crossfades from it.
- For a clip that plays once, the fade is at most half the clip's length.
- **Loop.** With `loop` set to `true`, the clip repeats. A clip that repeats by itself in Unity keeps repeating. With `loop` set to `false`, the clip plays once. It starts to fade out `fadeSeconds` before its last frame, so it ends on the last frame, and then it stops.
- **Play returns `false`** for an unknown NPC, a dead NPC, an NPC whose object is not active, an NPC with no `Animator`, a clip the other mod is playing, or a save in progress.
- **Stop** fades the clip back to the NPC's own animation over `fadeSeconds`. A fade of `0` ends it at once. Only the mod that started the clip can stop it.
  - A second `Stop` on a clip that is already fading out returns `false`, unless its `fadeSeconds` is `0`. A `0` fade ends the clip at once and returns `true`.
  - `Stop` returns `false` for another mod's clip, or when nothing plays.
- **IsPlaying** returns `true` while a clip runs and is not fading out. It returns `false` once the clip starts to fade out, after `Stop`, after a clip that played once has ended, and for an NPC with no clip.
- **StopAll** ends every clip of the mod at once, without a fade, and returns how many it ended.
- **A hidden or dead NPC ends its clip.** When the NPC's object becomes inactive, or the NPC dies, its clip ends at once and the graph is destroyed. The clip is not resumed when the NPC comes back. Play it again when you need it.
- **Scene loads.** A Single scene load ends every clip at once. Clips are not saved, so play them again from `Events.WhenReady` when you need them back. A `Play` called straight from an `Events.SceneLoaded` handler is refused, because the load gate is set for the whole of that event and `Play` returns `false` while it is set.

## Movement and the NPC's own routine

The clip changes the pose. It does not stop the NPC. The NPC's own schedule and behaviour keep walking it while the clip plays. To keep an NPC in place, hold it with `NpcMovement.SetActivityEnabled(modId, npcId, false)` and release it with `true` when the clip ends. See [NPC movement](npc-movement.md). To also turn off its sight, talk and barks while it plays, borrow it with `NpcControl.Take(modId, npcId)` instead. See [NPC control](npc-control.md).

## Not included

- Animation events in the clip.
- Bone masks, a speed change for the clip, or a second clip on the same NPC.
- Sound. A clip makes no sound.
- Blending between a clip and an animator trigger. The game's own animator keeps its state, and its parameters are not copied into the clip.

## Example

```csharp
NpcMovement.SetActivityEnabled(modId, npcId, false);
NpcAnimation.Play(modId, npcId, fightStance, true, 0.3f);

NpcAnimation.Stop(modId, npcId, 0.3f);
NpcMovement.SetActivityEnabled(modId, npcId, true);
```

## If a hook cannot be installed

The facade has no patches. It uses the framework's per frame update and the scene load event, so nothing else needs to be installed.
