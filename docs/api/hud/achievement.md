---
title: Achievement
parent: HUD
grand_parent: Screen and UI
nav_order: 5
---

# Achievement

`Achievement` reads the game's achievements and lets your mod unlock them or move their progress. An unlock counts for the real Steam achievement too, so it cannot be taken back from a mod.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Achievement</code> <span>the game's achievements</span></p>

This is the class you start from. `All()` and `Get` read the achievements, `Trigger` unlocks one, `AddProgress` and `SetProgress` move its progress, and `ItemLinks()` lists the items that trigger them.

<div class="api-pair" markdown="1">

```csharp
public static class Achievement
{
    public static List<AchievementInfo> All();
    public static AchievementInfo Get(string id);
    public static bool IsUnlocked(string id);
    public static bool Trigger(string id);
    public static bool AddProgress(string id, float amount);
    public static bool SetProgress(string id, float value);
    public static List<AchievementItemLink> ItemLinks();
}
```

| Method | Returns | What it does |
|---|---|---|
| `All()` | `List<AchievementInfo>` | Every achievement. It is empty until the game has loaded them. |
| `Get(id)` | `AchievementInfo` | One achievement. `null` when the id is not known. |
| `IsUnlocked(id)` | `bool` | `true` when it is unlocked. `false` for an id the game does not know. |
| `Trigger(id)` | `bool` | Asks the game to unlock it. `true` when it is unlocked afterwards. |
| `AddProgress(id, amount)` | `bool` | Adds to its progress. `true` when the game was told. |
| `SetProgress(id, value)` | `bool` | Sets its progress. `true` when the game was told. |
| `ItemLinks()` | `List<AchievementItemLink>` | Which items trigger which achievements. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>AchievementInfo</code> <span>one achievement</span></p>

`All()` and `Get(id)` give you this, one per achievement, read from the game's own achievements.

<div class="api-pair" markdown="1">

```csharp
public sealed class AchievementInfo
{
    public string Id { get; }
    public float Progress { get; }
    public float MaxProgress { get; }
    public bool Unlocked { get; }
    public bool Enabled { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | The achievement's id. |
| `Progress` | `float` | How far along it is. |
| `MaxProgress` | `float` | The progress it needs to unlock. |
| `Unlocked` | `bool` | `true` when it has been unlocked. |
| `Enabled` | `bool` | `true` when the achievement is switched on in the game. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>AchievementItemLink</code> <span>an item that triggers an achievement</span></p>

`ItemLinks()` gives you one of these for each item that triggers an achievement. `OnConsumed` says whether the achievement fires when the item is used up or when it is picked up.

<div class="api-pair" markdown="1">

```csharp
public sealed class AchievementItemLink
{
    public int ItemId { get; }
    public string AchievementId { get; }
    public bool OnConsumed { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `ItemId` | `int` | The item's id. |
| `AchievementId` | `string` | The achievement it triggers. |
| `OnConsumed` | `bool` | `true` when it fires when the item is consumed, `false` when the item is collected. |

</div>
</div>

## Things to know

- `All` and `Get` read the game's achievements, and you can't change them directly. `Unlocked` is the game's own flag.
- `Trigger` only unlocks an achievement when its progress is full. An achievement with no progress counts as full, so it unlocks at once. When it unlocks, the game also gives its rewards. <span class="tech">(It calls the game's `TriggerAchievement(id)`.)</span>
- `AddProgress` and `SetProgress` move the progress, and the game updates the Steam stat. The achievement unlocks once the progress is full. <span class="tech">(Both call `TriggerAchievement(id, progress, set)`.)</span>
- `AddProgress` and `SetProgress` give back `true` when the call was passed to the game. A number that is `NaN`, infinite or below zero is refused with `false` before the game is called, so a bad number cannot get into your save or the Steam stat.
- `ItemLinks()` lists the links for the scene the player is in. <span class="tech">(It reads the item achievement list of the current scene.)</span>
- The class is called `Achievement`, not `Achievements`, so it does not clash with the game's own `Achievements` class. Inside the framework's own code `Achievements` must keep meaning the game class.

## Limits

- `Trigger`, `AddProgress` and `SetProgress` do nothing and give back `false` for an id the game does not know, an achievement that is turned off, a game that has not loaded its achievements yet, or while the developer console is open. <span class="tech">(The console case is the game's `globalDisable`.)</span>
