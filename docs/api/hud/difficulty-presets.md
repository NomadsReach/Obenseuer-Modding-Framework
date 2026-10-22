---
title: Difficulty presets
parent: HUD
grand_parent: Screen and UI
nav_order: 7
---

# Difficulty presets

Status: experimental.

## What it is

The game has its own difficulty levels, and the player picks one when a game starts. Your mod can offer one of its own: a level you describe, built on one of the game's, with the values you choose. The framework keeps your choice in its own file inside the save folder, and a preset that changes values is also carried into the game's save as the game's own Custom level, so a save still loads when your mod is gone.

The game's own difficulty screen lists your presets, after the game's own levels, so the player can pick one while starting a game. Your mod decides when a loaded save uses its preset.

## When to use it

- Your mod changes the balance enough that the game's own levels no longer fit.
- You ship a few balance modes and want the player's pick to stay with the save.

## Quick example

This mod offers one preset based on the game's Hard level, with needs running 50% faster, and applies it whenever a save is loaded.

```csharp
using ObenseuerCore;

[ModInfo("example.harshernight", "Harsher Night", "0.1.0")]
public sealed class HarsherNight : FrameworkMod
{
    protected override void OnLoad()
    {
        GameDifficulty.Register("example.harshernight", "harsh", "Harsh", "Hard, with needs that run faster.", DifficultyLevel.Hard, settings =>
        {
            settings.NeedsSpeed = 1.5f;
        });
        Events.Loaded += () => GameDifficulty.SetPreset("example.harshernight", "harsh");
    }
}
```

## Common tasks

Offer a preset that only renames one of the game's levels, with no value of its own. Pass `null` for the callback.

```csharp
GameDifficulty.Register("example.plainhard", "plain", "Plain Hard", "The game's Hard level.", DifficultyLevel.Hard, null);
```

Apply your preset now and remember it in the save.

```csharp
if (!GameDifficulty.SetPreset("example.harshernight", "harsh")) Log.Info("The game can't change difficulty right now.");
```

Read which preset the game is running, if any.

```csharp
string preset = GameDifficulty.ActivePreset;
Log.Info(preset == null ? "No mod preset is running." : "Running " + preset);
```

Give up the choice. The game keeps the difficulty it is running until the player or your mod changes it.

```csharp
GameDifficulty.ClearPreset("example.harshernight");
```

## Reference

This is the full list of what the API gives you. Text in brackets at the end of a note is extra detail for experienced modders, and you can skip it on a first read.

### GameDifficulty presets

`GameDifficulty` offers, applies and clears a preset.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>GameDifficulty</code> <span>offer and apply a difficulty preset</span></p>

<div class="api-pair" markdown="1">

```csharp
public static class GameDifficulty
{
    public static bool Register(string modId, string id, string name, string description, DifficultyLevel basedOn, Action<DifficultyPresetSettings> configure);
    public static bool SetPreset(string modId, string id);
    public static bool ClearPreset(string modId);
    public static string ActivePreset { get; }
}
```

</div>

| Method | Returns | What it does |
|---|---|---|
| `Register(modId, id, name, description, basedOn, configure)` | `bool` | Offers a preset. `true` when it was registered. |
| `SetPreset(modId, id)` | `bool` | Makes your preset the difficulty of the loaded save. `true` when it was applied. |
| `ClearPreset(modId)` | `bool` | Drops your preset as the chosen one. `true` when it was the chosen one. |

| Property | Type | What it means |
|---|---|---|
| `ActivePreset` | `string` | The chosen preset as `modId.id` while the game is running it. `null` when none. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>DifficultyPresetSettings</code> <span>the values your preset changes</span></p>

The callback you pass to `Register` gets one of these, filled with the base level's own values. Change the ones you want and leave the rest alone. A value written here replaces the game's step of the same size, so `1.5` becomes the game's own 1.5 step. <span class="tech">(The game stores each multiplier as a step from `Disabled` to `Fourfold`, and the framework sorts those steps by value, so a value between two steps becomes the nearer one and an even gap takes the lower one. A value of `0` turns a multiplier off, except for `Prices` and `RenovationSpeed`, which keep the base level's value: the game divides by `RenovationSpeed`, and it treats a switched-off price as normal.)</span>

<div class="api-pair" markdown="1">

```csharp
public sealed class DifficultyPresetSettings
{
    public bool UserSavesDisabled { get; set; }
    public bool MentalHealthDisabled { get; set; }
    public bool MandatoryStartingAddictionsDisabled { get; set; }
    public float NeedsSpeed { get; set; }
    public float AddictionNeedsSpeed { get; set; }
    public float AddictionRecoverySpeed { get; set; }
    public float RenovationCost { get; set; }
    public float RenovationResourceCosts { get; set; }
    public float RenovationSpeed { get; set; }
    public float RentIncome { get; set; }
    public float Prices { get; set; }
    public float RespawningTime { get; set; }
    public float SmvProgressionRate { get; set; }
    public float TraderWealth { get; set; }
    public float StackSize { get; set; }
}
```

</div>

| Property | Type | What it means |
|---|---|---|
| `UserSavesDisabled` | `bool` | `true` when the player cannot save by hand. |
| `MentalHealthDisabled` | `bool` | `true` when mental health is switched off. |
| `MandatoryStartingAddictionsDisabled` | `bool` | `true` when the game's mandatory starting addictions are switched off. |
| `NeedsSpeed` | `float` | How fast needs change. `1` is normal, `0` is off. |
| `AddictionNeedsSpeed` | `float` | How fast addiction needs build up. |
| `AddictionRecoverySpeed` | `float` | How fast the player recovers from addictions. |
| `RenovationCost` | `float` | The money cost of renovations. |
| `RenovationResourceCosts` | `float` | The materials renovations need. |
| `RenovationSpeed` | `float` | How fast renovations get built. |
| `RentIncome` | `float` | The rent the player earns. |
| `Prices` | `float` | Shop prices. |
| `RespawningTime` | `float` | How long things take to respawn. |
| `SmvProgressionRate` | `float` | How fast the game's SMV progression moves. |
| `TraderWealth` | `float` | How much money traders have. |
| `StackSize` | `float` | How big item stacks are. |

These are the same values the [GameDifficulty](gamedifficulty.md) page lists, and they mean the same there. A preset that changes no value behaves as its base level.

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>DifficultyLevel</code> <span>the level your preset is built on</span></p>

`basedOn` is one of the game's own levels. `Custom` is not allowed, because a preset that changes values is kept on the game's Custom level by the framework.

<div class="api-pair" markdown="1">

```csharp
public enum DifficultyLevel
{
    Default,
    Casual,
    Easy,
    Hard,
    Brutal,
    Creative,
    Custom
}
```

</div>

</div>
</div>

#### Things to know

- `Register` keeps your preset in memory for the session. Register it again each time the game starts, from your mod's load code. A second call with the same mod id and id is refused, so register once.
- `Register` gives back `false`, and writes one line to the log, for a mod id or id that is not 1 to 64 letters, digits, `.`, `_` or `-`, for a missing name, for a base level of `Custom`, and for a mod id and id that is already registered.
- A preset with a callback is kept on the game's Custom level, with your values written into the game's own custom settings. A preset with no callback uses the base level itself, exactly as the game runs it.
- The framework keeps the chosen preset in its own file, `mods/obenseuer.moddingframework.difficulty.preset.json`, inside the save folder. A preset that changes values is also written into the game's save, as the game's own Custom level, with its name, its description and its values. <span class="tech">(Both parts hold one of the game's own levels and one of its own value steps, so the game's own load can never meet a level or a step it does not have.)</span>
- `SetPreset` gives back `false` on the main menu, while a save is loading or being written, and for a preset your mod id did not register. It writes one line to the log for an unknown preset.
- `SetPreset` works on the save that is loaded. It changes nothing on the game's difficulty screen, and a new game starts on whatever the player picked there. Picking a preset on that screen chooses it the same way `SetPreset` does.
- `ActivePreset` compares the chosen preset with the difficulty the game is running. A preset with a callback is in force while the running level is Custom and carries the preset's name. A preset with no callback is in force while the running level is the level it is based on, whoever picked that level, so the framework cannot tell two plain presets on the same level apart. Anything else gives back `null`.
- The game's own difficulty screen lists your presets, after the game's own levels, in the order of their `modId.id` keys. Picking one there chooses it, so `ActivePreset` reports it. A preset with a callback is listed on the game's Custom level, and a preset with no callback keeps its base level's own tag. The setting `ModPresetsInSelector` in the `UI` section of the framework's config, on by default, turns the listing off. See [Hud](hud.md#mods-menu-settings).
- Editing one of the game's own setting rows while a preset is picked turns that entry into the game's Custom level and renames it, until the screen is built again (`DifficultyUI.ChangeToCustom`). `ActivePreset` gives back `null` from then on, because the name it compares no longer matches. The preset is listed again on the next visit of the screen, and picking it again chooses it.
- Removing your mod does not break the save. The save keeps the difficulty it had, and the game's own difficulty screen shows it as Custom for a preset that changed values, or as the level it was based on for a plain one. Nothing is reverted, and no error is raised. A save made while a preset was picked keeps that preset's position in the screen's list, and the framework puts the game back on a level it still has when that position is past the game's own levels, so the save still loads. <span class="tech">(A preset that no longer exists is never looked up again, and the framework's own file is left alone. The position is put back right after the game reads its save, before the game indexes the list with it.)</span>
- A plain preset that the game is running from its own entry in the list is the one case where that costs something: the screen's next and previous buttons and the game's console difficulty command both land on the preset's own entry, which is past the game's own levels, and the game writes its custom settings only while a Custom level runs, so with the mod gone that save loads on the game's Custom level with the game's default custom values, not on the base level the preset was built on.
- `ClearPreset` forgets the choice. The difficulty the game is running stays until it is changed again, by the player or by you. It gives back `false` when the chosen preset belongs to another mod id, or when none is chosen.

## Limits

- There is no framework screen for presets. The game's own difficulty screen lists them, while `ModPresetsInSelector` is on, and every registered preset is listed there, so a preset cannot be hidden from it. Your mod can still show its own choice, for example a button or a config row, and calls `SetPreset`.
- A preset cannot be based on another mod's preset, and it cannot be based on `Custom`.
- The values a preset can change are the ones listed above. The game's autosave setting and its rent payment period keep the base level's values, and so do the two crafting speed multipliers.
- Changing the difficulty in the game after a preset that changes values is applied replaces it. `ActivePreset` then gives back `null` rather than claiming your preset is running. A preset with no callback stays in force while its base level is the running one.
- Presets are not listed or read back as a group. Keep the ids you registered in your own mod.

## Details

- Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
- Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\obenseuer.moddingframework.difficulty.log`.
- Everything not listed in this reference is internal and may change without notice. Items marked experimental may change too.

### When you can call it

- Call everything from your mod's normal code (`OnLoad`, event handlers, `Update`), not from a background thread.
- `Register` works at any time, including before a save is loaded. `SetPreset` needs a loaded save.

### What to check for and what comes back

- Nothing here throws an error to you. A refusal gives back `false`, `null` or nothing, and one line goes to the log.
- `SetPreset` returning `true` means the game is now running your preset. A save written afterwards keeps it.
- The offline check for the level and step mapping runs with the rest of the framework's checks, so a build with a broken mapping fails that check.

### If something fails to set up

- There is no game method patched for presets, so there is no feature group and no setup failure of its own. `GameDifficulty.FactorsAvailable` covers the separate multiplier factors, not presets.
