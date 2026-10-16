---
title: Custom skills API
parent: API reference
nav_order: 103
---

# Custom skills API

Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Patch owner: `obenseuer.moddingframework.customskills`.
Files: `src/Api/CustomSkills.cs`, `src/Api/CustomSkillsPatches.cs`, `src/Api/CustomSkillsMath.cs`.

A mod declares its own skills. Each one gets a row in the character panel's skills list, drawn with the game's own skill row, and the game's level-up banner. The game's own skills are in [Player](player.md#skills).

## API

```csharp
public static class CustomSkills
{
    public static bool IsAvailable { get; }
    public static bool Register(string modId, string skillId, string displayName, float maxValue);
    public static bool Clear(string modId);
    public static float Get(string modId, string skillId);
    public static int Level(string modId, string skillId);
    public static bool Set(string modId, string skillId, float value);
    public static bool Add(string modId, string skillId, float amount);
    public static bool QueueBanner(string text);
    public static event Action<CustomSkillArgs> LevelChanged;
}

public sealed class CustomSkillArgs
{
    public string ModId { get; }
    public string SkillId { get; }
    public float Value { get; }
    public int Level { get; }
    public int PreviousLevel { get; }
}
```

- `Register` declares a skill for the session. Call it at start. Calling it again for the same id replaces the name and the maximum. `modId` is your plugin GUID. `skillId` is 1 to 64 characters of letters, digits, `.`, `_` or `-`. `displayName` must not be empty. `maxValue` must be a finite number above zero and at most 1,000,000. Use `10f` to match the game's `PlayerStats.maxLevel`.
- `Register` returns `false` when the mod already has 64 registered skills (`CustomSkillsMath.MaxPerMod`) and this skill id is new. Replacing an existing id does not count against the cap. Otherwise it returns `true`.
- Bad arguments throw `ArgumentException`: a `modId` or `skillId` that is not valid (see above), an empty `displayName`, a `maxValue` that is not a finite number above zero or is above 1,000,000 (the XP text is the value times 1000, and must fit an int), and a `value` or `amount` that is not finite.
- A skill that is not registered, including one removed by `Clear`, is not an error. `Get` and `Level` return `0`, and `Set` and `Add` return `false`.
- The level is the floor of the value, as the game's `PlayerSkill.Level` is. The value is kept between 0 and `maxValue`, as `IncreaseSkill` keeps the game's skills between 0 and `maxLevel`.
- `Set` and `Add` return `false` while a save loads or saves (`SaveData.Busy`), or when the skill is not registered. Otherwise they return `true` when the stored value changed. `Add` takes a negative amount to lower the skill.
- `Clear(modId)` unregisters the mod's skills for the session and returns `true` when at least one was removed. The saved values are kept, and registering the skill again brings them back. Its rows are hidden the next time the panel refreshes.
- `LevelChanged` is raised by `Set` and `Add` when the integer level changes. A change inside one level does not raise it.
- `IsAvailable` is `false` when the panel and banner hooks did not install, or the owner was disabled. `Register`, `Get`, `Set`, `Add` and `Clear` still work then, but no rows appear.

## Character panel rows

- A row is an instance of the game's `CharacterPanelSkill` prefab, added under `skillList` after the game's rows. It shows the display name as the title, `LVL` and XP text, and the game's ten level bars.
- The hook is a postfix on `CharacterPanel.UpdateSkills`. `OnEnable` runs `Init` once and then `UpdateSkills`, so the rows exist before the first refresh.
- Rows do not duplicate. Each refresh finds a row by its name (`CustomSkill:` plus the skill's key) under `skillList`, and creates one only when none is there. A row for a cleared skill is set inactive.
- A registered skill always shows its row, even at 0. The game hides a row until its skill is above 1.
- When a custom row is shown, the panel's skill group is shown too, even when the player has no learned game skills.
- The bars and the maximum marker follow `value / maxValue` on the game's ten bars. The `LVL` and XP text use the mod's own value.

## Banners

- The game shows a banner through the private `PlayerSkillIncreaseUI.ShowNotification`. It sets the text, restarts a coroutine that waits 8 seconds in real time and then fades the alpha by 0.1 each frame, about ten frames. A second call replaces the first at once, so banners would overwrite each other.
- The framework's queue shows one banner every 9 seconds, in the order they were queued. A banner's time is its 8 second hold and the fade. The queue advances once per frame.
- The queue keeps three kinds of banner apart. Skill banners (from `Add`) have their own budget of 5 (`CustomSkillsMath.BannerQueue.MaxCustom`) and queued text banners (from `QueueBanner`) have their own budget of 5 (`CustomSkillsMath.BannerQueue.MaxQueued`), so one kind can never push the other out. A new banner for a skill that already has one waiting replaces that banner's text, so the newest level shows. When a sixth skill banner is queued, the oldest waiting skill banner is dropped. A queued text banner is never dropped to make room: `QueueBanner` returns `false` and queues nothing when 5 text banners already wait. Game banners are never dropped.
- `Add` queues a banner when the floor of the value rises. A change that takes the skill from level 0 to any higher level queues the game's new-skill text, `New skill`, `Name`, `learned!`, instead of the level-up banner for that change. Any other rise queues `Name`, `increased to`, `level N!`. `Set` and a save load show no banner.
- `QueueBanner(text)` queues `text` as a banner. It uses the same queue and timing as the skill banners, but its own budget of 5 waiting text banners. It throws `ArgumentException` when `text` is null or empty. Otherwise it returns `true` when the text was queued. It returns `false`, and queues nothing, when 5 text banners already wait, while a save loads or saves, or when the owner was disabled. It also returns `false` when no banner UI is on screen or `ShowNotification` is missing, and logs that once.
- The game's own skill banners use the same timing. Prefixes on `PlayerSkillIncreaseUI.ShowSkillIncrease` and `ShowNewSkill` check the queue. When no banner is showing and nothing is queued, the prefix marks 9 seconds busy and the game's own banner runs unchanged, as in vanilla. When a banner is showing or the queue is not empty, the game's text is queued and the game's call is skipped, so it waits behind the banners already queued. Banners only wait when two overlap.
- Queued banners are dropped when a save loads.
- `ShowNotification` is found once by reflection and cached. When it is missing, the two game prefixes are not installed, so the game's banners are unchanged, and custom banners are dropped and logged once. When no banner UI is on screen, a banner is dropped and logged once.

## Saving

- The values are saved in `obenseuer.moddingframework.customskills.json` in the save's mods folder, one entry per mod and skill: the mod id, the skill id and the value.
- Every save writes every entry, including the entries of mods that are not installed now.
- On load, every entry with a valid mod id, skill id and value is kept as it is, including the entries of mods that are not installed. A value is read only for a registered skill, so an entry waits until its mod registers the skill again, and then `Add` or `Set` changes that same entry. If a file holds two entries for one skill, the last one is kept.
- The values are emptied when the framework resets its save data, which happens when the game loads the main menu.
