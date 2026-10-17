---
title: Putting a look on a body
parent: Scene looks
grand_parent: Places and travel
nav_order: 6
---

# Putting a look on a body

This page uses `GameObject`, `Mesh` from Unity. The [glossary](../../glossary.md#types-that-come-from-unity) says what each one is.
{: .unity-types}

`Apply` and `Reskin` put a loaded look on a body. `Remove` takes it off again.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>SceneLooks</code> <span>puts looks on bodies</span></p>

`Apply` puts a loaded look on a body your mod has just spawned, `Reskin` puts it on an NPC that is already in the scene, and `Remove` takes it off again. `Apply`, `Reskin` and `Remove` return `false` with a `reason` when they refuse.

<div class="api-pair" markdown="1">

```csharp
public static class SceneLooks
{
    public static bool Apply(string modId, SceneLook look, GameObject body, out string reason);
    public static bool Reskin(string modId, SceneLook look, GameObject liveNpc, out string reason);
    public static bool Remove(string modId, GameObject body, out string reason);
    public static void Release(string modId, SceneLook look);
}
```

| Method | Returns | What it does |
|---|---|---|
| `Apply(modId, look, body, out reason)` | `bool` | Puts the look on a body your mod just spawned. Returns `true`, or `false` with a `reason`. |
| `Reskin(modId, look, liveNpc, out reason)` | `bool` | Puts the look on an NPC that is already in the scene. Returns `true`, or `false` with a `reason`. |
| `Remove(modId, body, out reason)` | `bool` | Takes the look off a body. Returns `true`, or `false` with a `reason`. |
| `Release(modId, look)` | `void` | Lets go of the look. |

</div>
</div>

## Things to know

- `Apply` puts the look on a body and hides the body's own models. It is meant for a body your mod has just spawned with `ModNpcs.Spawn` or `ModNpcs.SpawnWithLook`. A failed `Apply` leaves no textures or materials behind.
- The framework chooses how to fit the look to the body's skeleton. It tries the name path first, then the retarget path, and refuses if neither works. No half-applied look is left behind.
- On the name path, every bone of the look has a matching name on the body. The look's models are tied to the body's own bones, and the body's own animation keeps driving them. This costs nothing each frame. Two names are matched specially, and only when the body has no bone with the exact name: `mr_duck_head` is matched to `Bip001 Head`, and `Bip001_root` is matched to the body root.
- On the retarget path, the body is a humanoid and the look covers the 15 body parts that humanoid animation needs. The framework builds the look's skeleton under the body and copies the body's pose onto it every frame, so the look follows the body's animation. The name path wins when both could work. <span class="tech">(the 15 body parts are the required Mecanim humanoid roles)</span>
- A look's models are built separately for each body, but its textures and materials are built once and shared. Where a model's pieces sit at rest comes from the rest pose, never from the live pose. <span class="tech">(on the name path the bind poses are computed from the target's own mesh bind poses; on the retarget path the built mesh takes the look's own file bind poses, and the rebuilt skeleton is derived from those)</span>
- `Reskin` puts the look on an NPC that is already in the scene and animated, and hides the NPC's own models.
- `Reskin` uses the same fitting paths and gives the same refusals as `Apply`, and it also refuses a null NPC. The NPC's own animation keeps driving its bones, so the name path needs no work each frame, and the retarget path copies the pose as it does for `Apply`.
- `Reskin` also adds a small safety check that keeps the NPC's old models hidden. The game can switch an NPC's models back on, so the check hides them again about twice a second, and writes one line to the log the first time it has to. It only touches the same models the framework hid, so other parts of the NPC, such as a prop it is holding, are left as the game had them. `Remove` ends the check. <span class="tech">(the game's `Appearance` and `CharacterAppearance` show meshes by turning their GameObjects on, which does not undo the framework's hide, and neither rebuilds a body's renderers; the guard is a component on the look's own holder object that hides the same `SkinnedMeshRenderer`s again, so `Remove`, which destroys that object, also ends it; `MeshRenderer`s are left alone)</span>
- `Remove` takes the look off a body and puts back what was hidden. It destroys the models built for that body and switches the body's own models back on, each with the value it had before.
- Only the mod that applied the look can remove it. Another mod gets `false` with the reason `the look on this body was applied by mod <id>`. A body with no look gives `false` with the reason `the body has no applied look`.
- `Release(modId, look)` lets go of every hold your mod has on a look. `Load`, `Apply` and `Reskin` each take a hold, so ten applies still need only one `Release`. A look that was loaded and never applied can be released too.
- The look is freed only when no mod holds it any more and no body still has it applied, so one mod cannot free another mod's look. If your mod holds nothing on the look, `Release` does nothing except write one line to the log. <span class="tech">(the look leaves the session cache when no mod holds it, and its shared textures and materials are destroyed only when no body still has it applied)</span>

## Limits

- `Apply` refuses in these cases: the mod id is invalid, the look is not ready, the body is missing, the body already has a look or sits above or below one, or none of the ways to fit the look to the skeleton works (see the fitting paths above).
- If neither path works, `reason` says why. Nothing is applied in that case.
- It is not yet known whether the `Reskin` safety check ever fires in a real game.
- `Release` does not take the look off a body. Use `Remove` for that.
- A released handle cannot be used again. `Ready` becomes `false` and `Reason` becomes `released`.
