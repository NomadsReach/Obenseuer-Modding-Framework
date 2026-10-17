---
title: NPC animation
parent: People
grand_parent: API reference
has_children: true
nav_order: 6
---

# NPC animation

This page uses `KeyCode`, `Vector3` from Unity. The [glossary](../glossary.md#types-that-come-from-unity) says what each one is.
{: .unity-types}

Status: experimental.

## What it is

An animation clip is a short recorded movement, such as a wave or a stance. This API lets your mod play your own animation clip on an NPC in the scene, or on any other animated figure, and stop it again. Your clip is mixed in over the NPC's normal animation until you stop it. It does not stop the NPC from walking. To keep the NPC in place while the clip plays, hold it with [NPC movement](npc-movement.md).

Every call that takes an NPC uses the NPC's id from the [NPCs page](npcs.md).

## When to use it

- Make an NPC play a gesture or a pose that you made.
- Make a figure that your mod owns (an extra, see [Mod NPCs](modnpcs.md)) play an idle animation.
- Fade a clip in and out so it does not jump.
- Stop all the clips your mod has started in one call.

## Quick example

This mod waits for you to press F8. It finds the closest living NPC and plays a clip on it, or stops the clip when it is already playing. The clip is looked up by name among the clips the game has loaded. Change `ClipName` to the name of your own clip.

```csharp
using ObenseuerCore;
using UnityEngine;

[ModInfo("example.npcanim", "NPC Animation Demo", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class NpcAnimDemo : FrameworkMod
{
    private const string ClipName = "MyClip";
    private ModKeybind playKey;

    protected override void OnLoad()
    {
        playKey = Mod.Key("Play", "Play the clip on the closest NPC", KeyCode.F8);
    }

    private void Update()
    {
        if (playKey == null || UiState.IsMenuOpen || UiState.IsPaused || UiState.IsInputBlocked) return;
        if (!playKey.Down()) return;
        NpcInfo target = null;
        float best = float.MaxValue;
        foreach (NpcInfo npc in Npcs.All())
        {
            if (!npc.Alive) continue;
            float distance = Vector3.Distance(npc.Position, PlayerState.Position);
            if (distance < best)
            {
                best = distance;
                target = npc;
            }
        }
        if (target == null) return;
        if (NpcAnimation.IsPlaying(target.Id))
        {
            NpcAnimation.Stop(Mod.Id, target.Id, 0.3f);
            return;
        }
        foreach (AnimationClip clip in Resources.FindObjectsOfTypeAll<AnimationClip>())
        {
            if (clip.name != ClipName || clip.legacy) continue;
            if (!NpcAnimation.Play(Mod.Id, target.Id, clip, true, 0.3f)) Log.Info("The clip could not play on " + target.Name + ".");
            return;
        }
        Log.Info("No clip named " + ClipName + " is loaded.");
    }
}
```

`Play` and `Stop` take your mod's id, so each mod can only stop its own clips. A clip from your own mod, for example from an asset bundle, works the same way. See the [keybinds guide](../guides/getting-started/keybinds.md) for `Mod.Key`. A project that uses this API needs a reference to `UnityEngine.AnimationModule.dll`, because `AnimationClip` and `Animator` are in it.
