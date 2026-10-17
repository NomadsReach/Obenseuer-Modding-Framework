---
title: Start choices
parent: Framework basics
grand_parent: API reference
nav_order: 11
---

# Start choices

Status: experimental.

## What it is

When the player starts a new game, the game shows a start selection screen where they pick how their character begins: a family, a partner, a police record and so on. This API lets your mod read those picks, check whether a certain pick was made, see whether the character starts married, in a relationship or single, and hear about each pick as the player makes it. It only reads. It cannot add, remove or change a pick.

## When to use it

- Change what your mod does based on how the player started, such as a different welcome message for a character with a police record.
- Check whether the player picked a certain start with `Has`.
- React the moment the player makes a pick on the start screen.

## Quick example

This mod writes a log line each time the player makes a pick on the start screen. Once a save is loaded, it also logs the picks so far and whether the character is married.

```csharp
using ObenseuerCore;

[ModInfo("example.startwatch", "Start Watch", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class StartWatch : FrameworkMod
{
    protected override void OnLoad()
    {
        StartChoices.Chosen += args => Log.Info("The player picked " + args.Title + " (" + args.Id + ").");
        Events.WhenReady(() =>
        {
            foreach (StartChoiceInfo pick in StartChoices.Selected())
                Log.Info("Start pick: " + pick.Title);
            Log.Info("Relationship: " + StartChoices.Relationship);
        });
    }
}
```

`Chosen` is an event: the framework calls your code each time it happens. A game that is loaded from a save never fires it, which is why the example reads `Selected()` as well. See [Using events](../guides/getting-started/using-events.md).

## Common tasks

Check whether the player picked a certain start. The id is the pick's id in the game, for example `Police`.

```csharp
if (StartChoices.Has("Police")) Log.Info("This character has a police record.");
```

Add up the money the player's picks gave them.

```csharp
int oc = 0;
int rm = 0;
foreach (StartChoiceInfo pick in StartChoices.Selected())
{
    oc += pick.Oc;
    rm += pick.Rm;
}
Log.Info("The picks gave " + oc + " OC and " + rm + " RM.");
```

Say what the character's relationship is.

```csharp
string relationship = StartChoices.Relationship;
Log.Info(relationship == null ? "The start manager is not loaded yet." : "The character is: " + relationship);
```

## Reference

This is the full list of what the API gives you.

### Start picks

A pick is one choice the player made on the start screen. Each pick you get is a `StartChoiceInfo`, a copy taken when you asked. Changing the game's list later does not change a copy you already hold.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>StartChoices</code> <span>read the picks and hear about new ones</span></p>
<div class="api-pair" markdown="1">

```csharp
public static class StartChoices
{
    public static bool IsAvailable { get; }
    public static List<StartChoiceInfo> Selected();
    public static bool Has(string id);
    public static string Relationship { get; }
    public static event Action<StartChoiceArgs> Chosen;
}
```

| Method | Returns | What it does |
|---|---|---|
| `Selected()` | `List<StartChoiceInfo>` | The picks so far, in the order the player made them. |
| `Has(string id)` | `bool` | `true` when a pick has this id, for example `"Police"`. |

| Property | Type | What it means |
|---|---|---|
| `IsAvailable` | `bool` | `false` when the framework could not set up the `Chosen` event. |
| `Relationship` | `string` | `"Married"`, `"In a relationship"` or `"Single"`. `null` before the start manager exists. |

| Event | Gives you | When it fires |
|---|---|---|
| `Chosen` | `StartChoiceArgs` | The player has just made a pick. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>StartChoiceInfo</code> <span>a copy of one pick</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class StartChoiceInfo
{
    public string Id { get; }
    public string Title { get; }
    public string Description { get; }
    public int Oc { get; }
    public int Rm { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | The pick's id in the game. |
| `Title` | `string` | The name shown on the start screen. |
| `Description` | `string` | The pick's description text, without the addiction lines the start screen adds. |
| `Oc` | `int` | The money the pick gives, in OC (Open Sewer Coins). |
| `Rm` | `int` | The money the pick gives, in RM (Real Money). |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>StartChoiceArgs</code> <span>what Chosen tells you</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class StartChoiceArgs
{
    public string Id { get; }
    public string Title { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | The pick's id. |
| `Title` | `string` | The name shown on the start screen. |

</div>
</div>

#### Things to know

- This API only reads. It does not add, remove or change picks. Each call gives you a copy.
- `Selected()` gives an empty list before the game's start manager exists, and when no pick was made. Empty entries are left out. <span class="tech">(The start manager is `StartManager`. The picks are in its `selectedSelections` list, which the game saves with the game.)</span>
- `Has(id)` is `true` when any pick has that id. Passing `null` gives back `false` and writes one line to the log.
- `Relationship` says how the character starts out. It is `Married` when a pick has the id `Family`, `In a relationship` when a pick has the id `Girlfriend` or `Boyfriend`, and `Single` otherwise. The first matching pick in the list decides, as it does in the game. It is `null` before the start manager exists. It is worked out from the picks as they are now, so it is right straight after a pick. <span class="tech">(The game's own `StartManager.relationship` field is only set at the start and on load, so it can be out of date.)</span>
- `Chosen` fires once for each pick, after the game has applied the pick. By then the game has given the money, addictions, items and relationships that go with it, and recorded the pick. <span class="tech">(It is a postfix on `StartManager.Next`.)</span>
- The picks are saved with the game and read back on load, so `Selected`, `Has` and `Relationship` give the same answers after a load.

#### Limits

- A game that is loaded from a save never fires `Chosen`. To react to a loaded game, read `Selected()`.
- `Chosen` only fires from the new-game start screen. If that screen is missing, the pick is recorded but `Chosen` does not fire for it.
- `IsAvailable` is `false` when the `Chosen` event could not be set up. `Selected`, `Has` and `Relationship` do not depend on it. See [If something fails to set up](#if-something-fails-to-set-up).

## Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\obenseuer.moddingframework.startchoices.log`.

Everything not listed in this reference is internal and may change without notice. This page is experimental and may change too.

The framework changes one of the game's methods when it starts, so it can tell your mod when a pick is made. This page calls that a feature group. Its name, the one you will see in the log, is `obenseuer.moddingframework.startchoices`.

### When you can call it

- Call everything from your mod's normal code, not from a background thread you started. The game and Unity only work safely on one thread, the main thread (see the [glossary](../glossary.md#main-thread)). That includes the code that handles `Chosen`.
- It is safe to call at any time. If the game's start manager is missing, you get an empty list, `false` or `null` instead of an error.
- If one of your `Chosen` handlers throws an error, the framework logs it and still runs the other handlers.

### If something fails to set up

The framework changes some of the game's methods when it starts. If a game update breaks one of them, the framework removes all the changes of the [feature group](../glossary.md#feature-group) that depends on it, turns the group off, and writes error lines and a warning to the group's log file in `BepInEx\logs`. The rest of the framework keeps working.

- If the pick change cannot be made, the feature group is turned off and `Chosen` stops firing. `StartChoices.IsAvailable` becomes `false`. `Selected`, `Has` and `Relationship` keep working. <span class="tech">(The game method involved is `StartManager.Next`. The log line reads "start choice events not installed, Chosen is disabled".)</span>
