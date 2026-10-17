---
title: Blend shapes
parent: NPC appearance
grand_parent: People
nav_order: 5
---

# Blend shapes

A blend shape is a setting on a part that bends it, for example to open a mouth or narrow an eye. Each shape has a weight, which says how strongly it is applied.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>NpcBlendShapeInfo</code> <span>one blend shape on one part</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class NpcBlendShapeInfo
{
    public string Mesh { get; }
    public string Shape { get; }
    public float Weight { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Mesh` | `string` | The name of the part. |
| `Shape` | `string` | The name of the blend shape. |
| `Weight` | `float` | Its weight right now. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>NpcAppearance</code> <span>blend shape calls</span></p>
<div class="api-pair" markdown="1">

```csharp
public static partial class NpcAppearance
{
    public static List<NpcBlendShapeInfo> BlendShapes(int npcId);
    public static bool SetBlendShape(string modId, int npcId, string mesh, string shape, float weight);
}
```

| Method | Returns | What it does |
|---|---|---|
| `BlendShapes(int npcId)` | `List<NpcBlendShapeInfo>` | Every blend shape of the character, with its weight. |
| `SetBlendShape(string modId, int npcId, string mesh, string shape, float weight)` | `bool` | Sets the weight of one shape. Returns `true` when it was set. |

</div>
</div>

## Things to know

- `BlendShapes(npcId)` lists every blend shape of every switched-on part of the character, with its weight right now.
- `SetBlendShape` sets the weight of the shape on every switched-on part with that name that has that shape. The weight is kept between 0 and 200.
- Only one mod can change a given shape of a given part of a character at a time. A second mod is refused until the first mod calls `Restore` or `Clear`.
- `Restore` and `Clear` put each changed weight back to the value it had before your mod's first change.

## Limits

- `SetBlendShape` refuses the eyelid shape. The game closes the eyelids with that shape when a character sleeps or dies, so the call returns `false` for it. <span class="tech">(The shape is `_01UpperLidLower`.)</span>
- The game moves the mouth shapes itself while a character talks. They are not refused here, so a change to a mouth shape can be overwritten while the character is talking.
