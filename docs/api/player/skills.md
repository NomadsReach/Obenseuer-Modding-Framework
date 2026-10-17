---
title: Skills
parent: Player
grand_parent: Player life and skills
nav_order: 5
---

# Skills

Skills are the things the player gets better at, such as Cooking or Lockpicking. Each has a level from 0 up to the maximum.



<div class="api-type" markdown="1">
<p class="api-type-head"><code>Skills</code> <span>read, raise and watch skills</span></p>

Use it to read a skill's level, raise a skill with `Add`, and listen to `SkillIncreased`.

<div class="api-pair" markdown="1">

```csharp
public static class Skills
{
    public static bool IsAvailable { get; }
    public static int MaxLevel { get; }
    public static float Get(Skill skill);
    public static int Level(Skill skill);
    public static bool Add(Skill skill, float amount);
    public static List<SkillValue> All();
    public static event Action<SkillIncreasedArgs> SkillIncreased;
}
```

| Property | Type | What it means |
|---|---|---|
| `IsAvailable` | `bool` | `false` when the skill increased event could not be set up. |
| `MaxLevel` | `int` | The highest level a skill can reach (10 in this game version). |

| Method | Returns | What it does |
|---|---|---|
| `Get(skill)` | `float` | The skill's value, with its progress as a fraction. |
| `Level(skill)` | `int` | The skill's whole-number level. |
| `Add(skill, amount)` | `bool` | Raises a skill. Returns `false` when the game refuses. |
| `All()` | `List<SkillValue>` | Every skill except `None`. An empty list without a player. |

| Event | Gives you | When it fires |
|---|---|---|
| `SkillIncreased` | `SkillIncreasedArgs` | A skill went up. `args.Skill` is the skill, and `args.Amount` is what was actually gained (above 0). |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Skill</code> <span>the player's skills</span></p>

Pass one of these to the `Skills` calls. `None` is not a skill, and `All()` leaves it out.

<div class="api-pair" markdown="1">

```csharp
public enum Skill
{
    None,
    Lockpicking,
    Drinking,
    Cooking,
    Mashing,
    Brewing,
    Distilling,
    Farming,
    Malting,
    Manufacturing,
    Paperwork,
    Chemistry,
    Carpentry,
    Machining,
    Engineering,
    Bribery,
    Cleaning,
    Sewing,
    Electronics,
    Mining,
    Music,
    Smithing,
    Medical,
    Computers,
    Sauna
}
```

| Value | Meaning |
|---|---|
| `None` | No skill. `All()` leaves it out. |
| `Lockpicking` | The Lockpicking skill. |
| `Drinking` | The Drinking skill. |
| `Cooking` | The Cooking skill. |
| `Mashing` | The Mashing skill. |
| `Brewing` | The Brewing skill. |
| `Distilling` | The Distilling skill. |
| `Farming` | The Farming skill. |
| `Malting` | The Malting skill. |
| `Manufacturing` | The Manufacturing skill. |
| `Paperwork` | The Paperwork skill. |
| `Chemistry` | The Chemistry skill. |
| `Carpentry` | The Carpentry skill. |
| `Machining` | The Machining skill. |
| `Engineering` | The Engineering skill. |
| `Bribery` | The Bribery skill. |
| `Cleaning` | The Cleaning skill. |
| `Sewing` | The Sewing skill. |
| `Electronics` | The Electronics skill. |
| `Mining` | The Mining skill. |
| `Music` | The Music skill. |
| `Smithing` | The Smithing skill. |
| `Medical` | The Medical skill. |
| `Computers` | The Computers skill. |
| `Sauna` | The Sauna skill. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>SkillValue</code> <span>one skill and how far along it is</span></p>

`Skills.All()` gives you one of these for each skill. `Value` includes the progress toward the next level, and `Level` is the whole-number part of it.

<div class="api-pair" markdown="1">

```csharp
public sealed class SkillValue
{
    public Skill Skill;
    public float Value;
    public int Level;
}
```

| Property | Type | What it means |
|---|---|---|
| `Skill` | `Skill` | The skill this is about. |
| `Value` | `float` | The skill from 0 up to `MaxLevel`. The fraction is the progress to the next level. |
| `Level` | `int` | The whole-number level (`Value` rounded down). |

</div>
</div>

## Things to know

- A skill is one number, and its level is that number rounded down. The game has no separate experience counter. `Get` gives you the number, so the part after the decimal point is the progress toward the next level. <span class="tech">(It is `PlayerStats.PlayerSkill.Skill`.)</span>
- `Add` raises the skill by `amount`. A negative amount lowers the skill, as the game does. It returns `false` when the game refuses the change, for example because the skill is already at `MaxLevel`. <span class="tech">(It calls the game's `PlayerStats.IncreaseSkill` with the notification turned off.)</span>
- `SkillIncreased` fires only when a skill really went up, whether the game or your mod raised it. So it covers `Add` and the skill gains the game makes itself. <span class="tech">(It is raised from a postfix on `PlayerStats.IncreaseSkill`.)</span>
