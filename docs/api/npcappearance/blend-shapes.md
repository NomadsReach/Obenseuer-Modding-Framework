---
title: Blend shapes
parent: NPC appearance
grand_parent: People
nav_order: 5
---

# Blend shapes

A blend shape is a setting on a part that bends it, for example to open a mouth or narrow an eye. Each shape has a weight, which says how strongly it is applied.

```csharp
public sealed class NpcBlendShapeInfo    // one blend shape on one part
{
    public string Mesh { get; }      // the name of the part
    public string Shape { get; }     // the name of the blend shape
    public float Weight { get; }     // its weight right now
}

public static partial class NpcAppearance
{
    public static List<NpcBlendShapeInfo> BlendShapes(int npcId);                                          // every blend shape of the character, with its weight
    public static bool SetBlendShape(string modId, int npcId, string mesh, string shape, float weight);    // sets the weight of one shape
}
```

- **`BlendShapes(npcId)` lists every blend shape of every switched-on part of the character, with its weight right now.**
- **`SetBlendShape` sets the weight of the shape on every switched-on part with that name that has that shape.** The weight is kept between 0 and 200.
- **`SetBlendShape` refuses the eyelid shape.** The game closes the eyelids with that shape when a character sleeps or dies, so the call returns `false` for it.
  - Technical: the shape is `_01UpperLidLower`.
- **Only one mod can change a given shape of a given part of a character at a time.** A second mod is refused until the first mod calls `Restore` or `Clear`.
- **`Restore` and `Clear` put each changed weight back to the value it had before your mod's first change.**
- **The game moves the mouth shapes itself while a character talks.** They are not refused here, so a change to a mouth shape can be overwritten while the character is talking.
