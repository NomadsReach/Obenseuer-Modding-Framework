---
title: Custom skills
parent: Player life and skills
grand_parent: API reference
has_children: true
nav_order: 2
---

# Custom skills

## What it is

The game has skills the player levels up, shown in the skills list on the character panel. This API lets your mod add skills of its own. A skill you register gets a row in that list, drawn the same way as the game's own rows, and the game's level-up banner when the player gains a level. The values are saved with the player's save, so a skill keeps its level between sessions.

The game's own skills are on the [Player](player/skills.md#skills) page.

## When to use it

- Add a new skill that your mod's activities level up, such as a trade your mod adds.
- Give the player experience for something your mod does, and react when they reach a new level.
- Show a message in the same banner style the game uses for skill level-ups.

## Quick example

This mod adds a skill called Baking. Each time the player finishes a craft successfully, the skill goes up a little, and the log says when it reaches a new level.

```csharp
using ObenseuerCore;

[ModInfo("example.baking", "Baking Skill", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class BakingSkill : FrameworkMod
{
    protected override void OnLoad()
    {
        CustomSkills.Register(Mod.Id, "baking", "Baking", 10f);
        CustomSkills.LevelChanged += args => Log.Info(args.SkillId + " is now level " + args.Level);
        Crafting.Crafted += args =>
        {
            if (args.Success) CustomSkills.Add(Mod.Id, "baking", 0.05f);
        };
    }
}
```

`Register` declares the skill, and `10f` is the highest value it can reach. `Add` raises the value. `LevelChanged` and `Crafted` are events: the framework calls your code each time they happen. See [Using events](../guides/getting-started/using-events.md).
