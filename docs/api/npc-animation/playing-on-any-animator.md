---
title: Playing on any animator
parent: NPC animation
grand_parent: People
nav_order: 3
---

# Playing on any animator

`PlayOn` and `StopOn` work like `Play` and `Stop`, but they take an `Animator` instead of an NPC id. They are meant for figures your mod owns, such as extras (see [Mod NPCs](../modnpcs.md)).

<div class="api-type" markdown="1">
<p class="api-type-head"><code>NpcAnimation</code> <span>clips on any animator</span></p>
<div class="api-pair" markdown="1">

```csharp
public static class NpcAnimation
{
    public static bool PlayOn(string modId, Animator animator, AnimationClip clip, bool loop, float fadeSeconds = 0.2f);
    public static bool StopOn(string modId, Animator animator, float fadeSeconds = 0.2f);
}
```

| Method | Returns | What it does |
|---|---|---|
| `PlayOn(string modId, Animator animator, AnimationClip clip, bool loop, float fadeSeconds = 0.2f)` | `bool` | Starts a clip on an animator. |
| `StopOn(string modId, Animator animator, float fadeSeconds = 0.2f)` | `bool` | Fades that clip out. |

</div>
</div>

## Things to know

- `PlayOn` works on any switched-on animator that is not part of a scene NPC. It also accepts the animators of props. It gives `false` for an animator that is switched off and for one that belongs to a scene NPC. Play a clip on a scene NPC with `Play` only. Playing on the player's own model is not supported. <span class="tech">(It refuses an animator that has an `NPCInfo` among its parents, so the player's rig and props are accepted but not supported as a target.)</span>
- A `null` animator throws an error, and an animator that was destroyed gives `false`. The error is an `ArgumentNullException` named `animator`.
- The other rules are the same as for `Play`. There is one clip for each animator, the fades work the same way, and a scene load ends the clip.
- `Play` and `Stop` do not see a clip started by `PlayOn`, and `StopOn` does not see a clip started by `Play`.
