---
title: Putting a look on the body's own skeleton
parent: Scene looks
grand_parent: Places and travel
nav_order: 7
---

# Putting a look on the body's own skeleton

This page uses `GameObject`, `Animator` and `Avatar` from Unity. The [glossary](../../glossary.md#types-that-come-from-unity) says what each one is.
{: .unity-types}

`ApplyToSkeleton` puts a loaded look on a humanoid body and ties the look's models straight to the body's own bones. The body's animator then drives the look, so nothing is copied each frame.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>SceneLooks</code> <span>puts a look on the body's own skeleton</span></p>

`ApplyToSkeleton` works like `Reskin`, but it fits the look to the body's own skeleton instead of choosing between the name path and the retarget path. See [Putting a look on a body](putting-a-look-on-a-body.md) for the other two paths.

<div class="api-pair" markdown="1">

```csharp
public static class SceneLooks
{
    public static bool ApplyToSkeleton(string modId, SceneLook look, GameObject body, out string reason);
}
```

| Method | Returns | What it does |
|---|---|---|
| `ApplyToSkeleton(modId, look, body, out reason)` | `bool` | Ties the look's models to the body's own bones. Returns `true`, or `false` with a `reason`. |

</div>
</div>

## Things to know

- The body must be humanoid: it needs an `Animator` whose `Avatar` is a humanoid avatar. Any other body is refused with the reason `the body is not humanoid`.
- Each bone of the look is matched to a bone of the body in two steps. The exact name is tried first. When the body has no bone with that name, the framework reads the bone's humanoid role, such as `LeftHand`, and uses the body bone that the body's avatar gives that role.
- The models of the look are bound to the body's own bones, with bind poses taken from the body's rest pose. The look follows the body's animation with no work each frame and no pose driver.
- Every bone of the look must be matched. A look with a bone that the body has neither by name nor by humanoid role is refused, and the reason names the bones that could not be matched. <span class="tech">(the look's finger and prop bones are the usual case, because the humanoid roles do not cover them)</span>
- The body's own models are hidden, the look is added under the body, and `Remove` puts everything back, exactly as it does for `Apply` and `Reskin`.
- The safety check that keeps the body's own models hidden after the game turns them back on is added here too, so a live NPC keeps the look. <span class="tech">(the same guard component sits on the look's own holder object, and `Remove` destroys that object, which ends the check)</span>
- `RigOf` reports `HasPoseDriver` as `false` for a look put on the body's own skeleton, because there is no pose to copy. `SyncPose` and `AutoPose` refuse on such a body.
- One line is written to your mod's own log when the look is applied. It names the path the framework took, which is `name`, `retarget` or `skeleton`, and it reports any scale or axis anomaly it found in the look. `CoreLog.LineLimit` reads and changes how many lines per second that log file accepts.

## Limits

- This path has not been run in the live game. The offline checks cover the bone matching and the bind pose maths only.
- A look whose bones the body does not have by name or by humanoid role cannot use this path. On the installed game data most looks carry finger and prop bones that no humanoid role covers, so those looks are refused unless the body also has them by name.
- A body that is not humanoid cannot use this path even when every bone name matches. Use `Apply` or `Reskin` for those bodies.
- The look's models sit in the body's own frame. The bind poses come from the body's rest pose, so the look's own bind poses are not used on this path.
