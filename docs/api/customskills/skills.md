---
title: Skills
parent: Custom skills
grand_parent: Player life and skills
nav_order: 2
---

# Skills

This is the full list of what the API gives you. In every code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

A `modId` is your mod's id (see [Owner](../../glossary.md#owner) in the glossary). A `skillId` is a short name you pick for the skill, such as `baking`. Together they name one skill, so two mods can use the same `skillId` without clashing.

```csharp
public static class CustomSkills
{
    public static bool IsAvailable { get; }    // false when the character panel rows and banners could not be set up
    public static bool Register(string modId, string skillId, string displayName, float maxValue);   // declares a skill; false only when the mod already has 64 skills
    public static bool Clear(string modId);    // unregisters all of this mod's skills for this session; true when any were removed
    public static float Get(string modId, string skillId);    // the skill's value; 0 when it is not registered
    public static int Level(string modId, string skillId);    // the value rounded down; 0 when it is not registered
    public static bool Set(string modId, string skillId, float value);    // sets the value; true when it changed
    public static bool Add(string modId, string skillId, float amount);   // adds to the value (negative lowers it); true when it changed
    public static bool QueueBanner(string text);   // queues a message in the skill banner; true when it was queued
    public static event Action<CustomSkillArgs> LevelChanged;   // a skill's level went up or down
}

public sealed class CustomSkillArgs    // what LevelChanged tells you
{
    public string ModId { get; }         // the mod that owns the skill
    public string SkillId { get; }       // the skill's short name
    public float Value { get; }          // the value after the change
    public int Level { get; }            // the level after the change
    public int PreviousLevel { get; }    // the level before the change
}
```

- **`Register` declares a skill for this session, so call it every time your mod starts.** Call it from `OnLoad`. Calling it again for the same `skillId` replaces the name and the maximum. The skill's value is kept, because it is saved separately.
- **The arguments to `Register` have limits.** `skillId` is 1 to 64 characters of letters, digits, `.`, `_` or `-`. `displayName` is the name shown to the player and cannot be empty. `maxValue` is the highest value the skill can reach. It must be a number above 0 and at most 1,000,000. Use `10f` to match the game's own skills, which go up to 10.
  - Technical: `10f` matches `PlayerStats.maxLevel`. The limit of 1,000,000 exists because the experience text is the value times 1000 and must fit in an `int`.
- **A mod can have at most 64 skills.** `Register` returns `false` when your mod already has 64 and the new `skillId` is not one of them. Replacing a skill you already registered does not count against the limit. In every other case it returns `true`.
  - Technical: the limit is `CustomSkillsMath.MaxPerMod`.
- **A call with an invalid argument throws an `ArgumentException`.** That is a `modId` or `skillId` that breaks the rules above, an empty `displayName`, a `maxValue` outside the allowed range, and a `value` or `amount` that is not a real number (for example `NaN` or infinity). Fix the call rather than catching the error.
- **A skill that is not registered is not an error.** That includes one removed by `Clear`. `Get` and `Level` return `0`, and `Set` and `Add` return `false`.
- **The level is the value rounded down, and the value always stays between 0 and `maxValue`.** A value of `3.9` is level `3`. If you set more than `maxValue` or less than 0, it is held at the edge. The game does the same for its own skills.
  - Technical: this mirrors `PlayerSkill.Level` and the clamp in `IncreaseSkill`.
- **`Set` and `Add` return `true` when the stored value changed.** They return `false` when the skill is not registered, when the value did not change (for example when it was already at the maximum), and while a save is loading or being written. Use a negative amount with `Add` to lower the skill.
  - Technical: the save check is `SaveData.Busy`.
- **`Clear(modId)` hides your skills for this session, and the player keeps their progress.** It returns `true` when at least one skill was removed. The saved values stay, and registering the skill again brings them back. The rows disappear the next time the character panel refreshes.
- **`LevelChanged` fires when `Set` or `Add` changes the whole-number level.** A change that stays inside one level, such as `3.1` to `3.4`, does not fire it. It does not fire when a save loads.
- **`IsAvailable` is `false` when the character panel rows and banners could not be set up.** The calls (`Register`, `Get`, `Set`, `Add` and `Clear`) still work then, but no rows appear. `QueueBanner` returns `false`. See [If something fails to set up](details.md#if-something-fails-to-set-up).
  - Technical: it is `false` when the panel and banner Harmony patches did not install, or when the feature group was turned off.
