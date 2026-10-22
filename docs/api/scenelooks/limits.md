---
title: Limits
parent: Scene looks
grand_parent: Places and travel
nav_order: 8
---

# Limits

This page uses `Mesh` from Unity. The [glossary](../../glossary.md#types-that-come-from-unity) says what each one is.
{: .unity-types}

- The face keeps its neutral shape. The look's face shapes are not applied yet.
- Only `Bip001` looks can use the retarget path. The body-part map is for `Bip001` only, so a look with another kind of skeleton uses the name path or is refused.
- Materials are opaque only. Transparency is not copied, because the settings that go with it are not copied either. <span class="tech">(`_Mode` is not copied, because its keywords and the matching render queue are not copied)</span>
- A look uses one skeleton for the whole look, so a mesh can sit up to about 1 cm off. In the installed file, the look's meshes disagree slightly about their shared starting pose, so one mesh of a look can sit that far from the others. <span class="tech">(`SceneLookHarness bindposes` reports the figure)</span>
- The retarget path costs extra work every frame. Each NPC makes two humanoid pose calls a frame, on top of normal skinning.
- A look is not saved. Apply it again after a load, as `SpawnSaved` does for a mod NPC's own look.
- You can't build a body from a look. A look can only go on a body that already exists.
