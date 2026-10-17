---
title: Skills
parent: Player
grand_parent: Player life and skills
nav_order: 5
---

# Skills

Skills are the things the player gets better at, such as Cooking or Lockpicking. Each has a level from 0 up to the maximum.

```csharp
public enum Skill
{
    None, Lockpicking, Drinking, Cooking, Mashing, Brewing, Distilling, Farming, Malting, Manufacturing, Paperwork, Chemistry,
    Carpentry, Machining, Engineering, Bribery, Cleaning, Sewing, Electronics, Mining, Music, Smithing, Medical, Computers, Sauna
}

public sealed class SkillValue    // one skill and how far along it is
{
    public Skill Skill;     // which skill
    public float Value;     // the skill from 0 up to MaxLevel; the fraction is the progress to the next level
    public int Level;       // the whole-number level (Value rounded down)
}

public static class Skills
{
    public static bool IsAvailable { get; }           // false when the skill increased event could not be set up
    public static int MaxLevel { get; }               // the highest level a skill can reach (10 in this game version)
    public static float Get(Skill skill);             // the skill's value, with its progress as a fraction
    public static int Level(Skill skill);             // the skill's whole-number level
    public static bool Add(Skill skill, float amount);   // raises a skill; false when the game refuses
    public static List<SkillValue> All();             // every skill except None; an empty list without a player
    public static event Action<SkillIncreasedArgs> SkillIncreased;   // a skill went up; args.Skill, and args.Amount actually gained (above 0)
}
```

- **A skill is one number, and its level is that number rounded down.** The game has no separate experience counter. `Get` gives you the number, so the part after the decimal point is the progress toward the next level.
  - Technical: it is `PlayerStats.PlayerSkill.Skill`.
- **`Add` raises the skill by `amount`.** A negative amount lowers the skill, as the game does. It returns `false` when the game refuses the change, for example because the skill is already at `MaxLevel`.
  - Technical: it calls the game's `PlayerStats.IncreaseSkill` with the notification turned off.
- **`SkillIncreased` fires only when a skill really went up, whether the game or your mod raised it.** So it covers `Add` and the skill gains the game makes itself.
  - Technical: it is raised from a postfix on `PlayerStats.IncreaseSkill`.
