---
title: Two kinds of body
parent: NPC appearance
grand_parent: People
nav_order: 3
---

# Two kinds of body

The game gives each character one of two kinds of body, and `ShapeKind` tells you which. It is read while the game runs.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>NpcShapeKind</code> <span>what kind of body a character has</span></p>
<div class="api-pair" markdown="1">

```csharp
public enum NpcShapeKind
{
    None,
    Sliders,
    BlendShapes
}
```

| Value | Meaning |
|---|---|
| `None` | Neither kind, or the character is not in the loaded scene. |
| `Sliders` | A body built from sliders, like the player's. |
| `BlendShapes` | A baked body that changes through blend shapes. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>NpcAppearance</code> <span>body kind call</span></p>
<div class="api-pair" markdown="1">

```csharp
public static partial class NpcAppearance
{
    public static NpcShapeKind ShapeKind(int npcId);
}
```

| Method | Returns | What it does |
|---|---|---|
| `ShapeKind(int npcId)` | `NpcShapeKind` | Which kind of body this character has. |

</div>
</div>

## Things to know

- A character built from sliders can only be told apart once the game has built its body. A change made in the very first frame of a scene can be refused, so make your changes after the scene has settled. <span class="tech">(`Sliders` needs an active `Appearance` component that `PlayerLook.IsReady` accepts. `BlendShapes` needs an active `CharacterAppearance` component.)</span>
- With `None`, the look and shape calls give empty answers or `false`.
