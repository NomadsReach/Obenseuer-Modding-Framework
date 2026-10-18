---
title: Conditions
parent: Framework basics
grand_parent: API reference
nav_order: 1
---

# Conditions

Status: experimental.

## What it is

Conditions are the health problems the player can pick up, such as the ones that come from poor hygiene or a high dose of radiation. Each one shows a "New condition!" message when it starts and a "Condition removed!" message when it ends. This API lists the conditions the game knows, and tells you which ones the player has right now. It lets your mod give or take away a condition, and tells you when one starts, stops or changes strength.

## When to use it

- Show or log a message when the player gets or loses a condition.
- Check whether the player has a condition before your mod does something.
- Give the player a condition as a penalty, or remove one as a reward.

## Quick example

This mod logs every change to a condition. Once a save is loaded, it also logs each condition the game knows and whether the player has it.

```csharp
using ObenseuerCore;

[ModInfo("example.conditionwatch", "Condition Watch", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class ConditionWatch : FrameworkMod
{
    protected override void OnLoad()
    {
        Conditions.Changed += args => Log.Info(args.Name + " active=" + args.Active + " severity=" + args.Severity);
        Events.WhenReady(() =>
        {
            foreach (ConditionInfo condition in Conditions.All())
                Log.Info(condition.Name + " active=" + condition.Active + " severity=" + condition.Severity);
        });
    }
}
```

`Changed` is an event: the framework calls your code each time it happens. `WhenReady` runs your code once the game is ready. See [Using events](../guides/getting-started/using-events.md) for more on both. The condition names are the game's own asset names and are not written down anywhere else, so read them from the log first and then use them in your code.

## Common tasks

Check whether the player has a condition. `name` comes from `Conditions.All()`.

```csharp
string name = "SomeCondition";
if (Conditions.Has(name)) Log.Info("The player has " + name);
```

Give the player a condition. `Add` returns `true` when the condition started or got stronger.

```csharp
string name = "SomeCondition";
bool added = Conditions.Add(name);
Log.Info("Added " + name + ": " + added);
```

Take a condition away. `Remove` removes one stack, so a condition that has stacked needs one call for each stack. `Has` tells you whether more are left.

```csharp
string name = "SomeCondition";
while (Conditions.Has(name) && Conditions.Remove(name)) { }
```

## Reference

This is the full list of what the API gives you. A condition is named by its `Name`, the game's internal name for it. That name is the key you pass to `Has`, `Add` and `Remove`.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Conditions</code> <span>list, check, add and remove conditions</span></p>

Use `Has`, `Add` and `Remove` with the game's internal name for a condition, which `ConditionInfo.Name` gives you. Subscribe to `Changed` to hear each change.

<div class="api-pair" markdown="1">

```csharp
public static class Conditions
{
    public static bool IsAvailable { get; }
    public static List<ConditionInfo> All();
    public static bool Has(string name);
    public static bool Add(string name);
    public static bool Remove(string name);
    public static event Action<ConditionChangedArgs> Changed;
}
```

| Property | Type | What it means |
|---|---|---|
| `IsAvailable` | `bool` | `false` when the change event could not be set up. |

| Method | Returns | What it does |
|---|---|---|
| `All()` | `List<ConditionInfo>` | Every condition the framework knows, active or not. |
| `Has(name)` | `bool` | `true` when the player has this condition. |
| `Add(name)` | `bool` | Gives the player the condition. `true` when it started or got stronger. |
| `Remove(name)` | `bool` | Takes away one stack of the condition. `true` when one stack was removed. |

| Event | Gives you | When it fires |
|---|---|---|
| `Changed` | `ConditionChangedArgs` | A condition started, stopped or changed strength. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>ConditionInfo</code> <span>one condition and its state</span></p>

`Conditions.All()` gives you one of these for each condition the framework knows. `Name` is the key for `Has`, `Add` and `Remove`, and `DisplayName` is the text for the player. <span class="tech">(It is built from the game's `BaseHealthCondition` objects.)</span>

<div class="api-pair" markdown="1">

```csharp
public sealed class ConditionInfo
{
    public string Name { get; }
    public string DisplayName { get; }
    public bool Active { get; }
    public float Severity { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Name` | `string` | The game's internal name. It is the key for `Has`, `Add` and `Remove`. |
| `DisplayName` | `string` | The text the player sees in messages. It may be empty or shared, so use it only for showing. |
| `Active` | `bool` | `true` when the player has this condition now. |
| `Severity` | `float` | How strong the condition is now. `0` when the player does not have it. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>ConditionChangedArgs</code> <span>what changed</span></p>

`Conditions.Changed` hands you one of these each time a condition starts, stops or changes strength.

<div class="api-pair" markdown="1">

```csharp
public sealed class ConditionChangedArgs
{
    public string Name { get; }
    public bool Active { get; }
    public float Severity { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Name` | `string` | The game's internal name of the condition. |
| `Active` | `bool` | `true` when the player has the condition after the change. |
| `Severity` | `float` | How strong it is after the change. `0` when it was removed. |

</div>
</div>

### Things to know

- `All()` lists the conditions the game keeps ready, not every condition that could exist. That is the conditions that come from low hygiene, the ones that come from a high dose of radiation, and any condition the player has right now. Each one is marked `Active` when the player has it, and `Severity` is its strength right now. <span class="tech">(The list is `PlayerStats.hygiene.lowHygieneConditions`, `PlayerStats.radiation.highDoseConditions` and `PlayerStats.conditions.activeConditions`. `Name` is the Unity object name of the condition asset, and `DisplayName` is its `conditionName`. No game type or game enum appears in the public types.)</span>
- `Add(name)` gives the player the condition, whatever the game's random chance would have been. The game picks how strong it is, starts it, and shows the "New condition!" message if that condition has messages turned on. If the player already has a condition that can stack, `Add` adds another stack with a new random strength. <span class="tech">(It calls `BaseHealthCondition.ApplyCondition(force: true)`. `force` skips the game's random chance. The message shows when the asset has `notify` set.)</span>
- `Add` returns `false` and does nothing when the condition cannot be added. That is when no condition has that name, when no save is loaded, when a save is loading or being written, and when the condition cannot stack and the player does not already have it.
- `Add` returns `false` for that last case because the game would show "New condition!" without adding anything, and the framework refuses before a false message can appear. <span class="tech">(The checks are `Find(name) == null`, `PlayerStats.instance` or its condition list being null, `SaveGate.Busy`, and `canStack` unset with no active entry.)</span>
- `Remove(name)` takes away one stack of the condition. The game shows the "Condition removed!" message if that condition has messages turned on. It returns `false` when the player does not have the condition. <span class="tech">(It calls `BaseHealthCondition.RemoveCondition()` once, which removes one entry from the active list.)</span>
- Fungal disease, radiation sickness and mental health are not conditions here. The game treats them as player stats, not conditions. Treatments for them are on the [Medical](medical.md) page. <span class="tech">(The game has no subclass of `BaseHealthCondition`. Fungal disease is `PlayerStatsMushrooms.SMVProgression` (`Stat.Smv`), radiation sickness is `PlayerStatsRadiation.Dose` (`Stat.Radiation`), and mental health is `PlayerStatsMentalHealth.currentStatus` (`Stat.Mood`).)</span>
- `Changed` fires after the game starts, stops or re-rolls a condition. It also fires when the game removes a condition by itself because its strength reached zero. Removing one stack of a condition that stays active at the same strength does not fire it. <span class="tech">(It is raised after `ApplyCondition` or `RemoveCondition` changes the active state or the strength. It is not worked out at all while nothing is subscribed.)</span>
- `Changed` does not fire when strength slowly drifts, and not when a new game clears the list. To see the strength right now, call `All()`. <span class="tech">(The per-tick drift is in `UpdateStats`.)</span>
- `Changed` does not wait for a save load to finish. A handler that should ignore changes made while a save is loading can check `SaveData.Busy`, which is `true` while a save is loading or being written.
- A `Changed` handler that throws an error does not stop the others. The framework logs the error and still runs the other handlers.

### Limits

- You can't create new conditions. The game ships its conditions as fixed assets and a mod cannot add one to the lists, so `Add` only works for conditions the game already has.
- A handler for `Changed` may call `Add` or `Remove`, but only a few levels deep. If your handler's `Add` or `Remove` causes another `Changed` that calls `Add` or `Remove` again, and this nests more than 8 levels, the deeper call returns `false` and the log gets one line about it.
- Do not use `Add` or `Remove` from inside a `Changed` handler for a condition the game is removing by itself. The game is in the middle of going through its list of conditions then, and changing the list there makes the game throw an error. <span class="tech">(The event can fire from inside the game's loop over the active list (the auto-removal in `UpdateStats`).)</span>

## Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\obenseuer.moddingframework.conditions.log`.

Everything not listed in this reference is internal and may change without notice. Items marked experimental may change too.

The framework changes some of the game's methods when it starts, so it can tell your mod when a condition changes. This page calls that group of changes a feature group: its name is `obenseuer.moddingframework.conditions`, the name you will see in the log.

### When you can call it

- Call everything from your mod's normal code (`OnLoad`, event handlers, `Update`), not from a background thread you started. The game and Unity only run safely on the main thread (see the [glossary](../glossary.md#main-thread)).
- It is safe to call any of it at any time, even on the main menu or while a save is loading. If there is nothing to read yet, you get an empty list or `false`. `Add` and `Remove` also return `false` then.

### What to check for and what comes back

- `Conditions.IsAvailable` tells you whether `Changed` works. It is `false` when the framework could not set up the event, or when the feature group was turned off. `All`, `Has`, `Add` and `Remove` keep working when it is `false`.
- A `name` that is null or empty throws an `ArgumentException`. This is the only error these calls throw to you. If the game itself fails while the framework works, you get the plain default (`false` or an empty list) and the first failure of each member goes to the log as an error.

### If something fails to set up

The framework changes some of the game's methods when it starts. If a game update breaks one of them, the framework turns off the [feature group](../glossary.md#feature-group) that depends on it, and writes error lines to the group's log file in `BepInEx\logs`. The rest of the framework keeps working.

- If the framework cannot change the game's condition methods, `Changed` stops and the rest keeps working. One error goes to the log, and `IsAvailable` becomes `false`. <span class="tech">(The methods are `BaseHealthCondition.ApplyCondition` and `RemoveCondition`.)</span>
