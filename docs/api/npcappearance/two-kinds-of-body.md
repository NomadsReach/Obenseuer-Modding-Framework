---
title: Two kinds of body
parent: NPC appearance
grand_parent: People
nav_order: 3
---

# Two kinds of body

The game gives each character one of two kinds of body, and `ShapeKind` tells you which. It is read while the game runs.

```csharp
public enum NpcShapeKind    // what kind of body a character has
{
    None,          // neither kind, or the character is not in the loaded scene
    Sliders,       // a body built from sliders, like the player's
    BlendShapes    // a baked body that changes through blend shapes
}

public static partial class NpcAppearance
{
    public static NpcShapeKind ShapeKind(int npcId);    // which kind of body this character has
}
```

- **A character built from sliders can only be told apart once the game has built its body.** A change made in the very first frame of a scene can be refused. Make your changes after the scene has settled.
  - Technical: `Sliders` needs an active `Appearance` component that `PlayerLook.IsReady` accepts. `BlendShapes` needs an active `CharacterAppearance` component.
- **With `None`, the look and shape calls give empty answers or `false`.**
