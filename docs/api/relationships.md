---
title: Relationships
parent: People
grand_parent: API reference
nav_order: 12
---

# Relationships

## What it is

This API reads the player's relationship scores and tells you when one changes.

## Reference

Scores run from -100 to 100.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Relationships</code> <span>scores and change event</span></p>

Use `GetDisposition` or `All()` to read the scores, `Has` to check whether the player has one, and `RelationshipChanged` to hear when a score changes.

<div class="api-pair" markdown="1">

```csharp
public static class Relationships
{
    public static List<RelationshipValue> All();
    public static float GetDisposition(string id);
    public static bool Has(string id);
    public static bool IsAvailable { get; }
    public static event Action<RelationshipChange> RelationshipChanged;
}
```

| Method | Returns | What it does |
|---|---|---|
| `All()` | `List<RelationshipValue>` | Every relationship the framework tracks. |
| `GetDisposition(id)` | `float` | The score, or `0` if the player does not have it. |
| `Has(id)` | `bool` | It is `true` when the player has it. |

| Property | Type | What it means |
|---|---|---|
| `IsAvailable` | `bool` | It is `false` when the change event is off. |

| Event | Gives you | When it fires |
|---|---|---|
| `RelationshipChanged` | `RelationshipChange` | It fires when the game's daily check changes a score. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>RelationshipValue</code> <span>one relationship</span></p>

`Relationships.All()` gives you one of these for each relationship the framework tracks. `Disposition` is the score now, and `Target` is the score it moves toward.

<div class="api-pair" markdown="1">

```csharp
public sealed class RelationshipValue
{
    public string Id { get; }
    public string Name { get; }
    public float Disposition { get; }
    public float Target { get; }
    public bool Enabled { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | The relationship's internal name in the game files. |
| `Name` | `string` | The name the game shows the player. |
| `Disposition` | `float` | The current score. |
| `Target` | `float` | The score it moves toward. |
| `Enabled` | `bool` | It is `true` when the player has it. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>RelationshipChange</code> <span>a score change</span></p>

`Relationships.RelationshipChanged` hands you one of these when the game's daily check changes a score. `Previous` and `Current` are the scores before and after.

<div class="api-pair" markdown="1">

```csharp
public sealed class RelationshipChange
{
    public string Id { get; }
    public string Name { get; }
    public float Previous { get; }
    public float Current { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | The relationship's internal name in the game files. |
| `Name` | `string` | The name the game shows the player. |
| `Previous` | `float` | The score before the change. |
| `Current` | `float` | The score after the change. |

</div>
</div>

### Putting it together

Logs any relationship that drops below zero.

<div class="api-example" markdown="1">

```csharp
using ObenseuerCore;

[ModInfo("example.moodwatch", "Mood Watch", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class MoodWatch : FrameworkMod
{
    protected override void OnLoad()
    {
        Relationships.RelationshipChanged += change =>
        {
            if (change.Previous >= 0 && change.Current < 0)
                Log.Info(change.Name + " dropped below zero, from " + change.Previous + " to " + change.Current);
        };
        Events.WhenReady(() =>
        {
            if (!Relationships.IsAvailable) Log.Info("Relationship change events are off, so no changes will be reported.");
            foreach (RelationshipValue relationship in Relationships.All())
            {
                if (!relationship.Enabled) continue;
                Log.Info(relationship.Name + ": " + relationship.Disposition + ", moving toward " + relationship.Target);
            }
        });
    }
}
```

| Step | What it does |
|---|---|
| `Relationships.IsAvailable` | `false` means no changes are reported. |
| `relationship.Enabled` | Skips relationships the player lacks. |

</div>

### Things to know

- `RelationshipChanged` does not fire on add, remove or save load.

### Limits

- Scores are per player, not per NPC.
- You can't set scores or targets, because the framework has nothing safe to call.

## Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later). Logs go to `BepInEx\logs\obenseuer.moddingframework.npcs.log`.

Relationships use feature group `obenseuer.moddingframework.npcs.relationship`.

### When you can call it

- Call from the [main thread](../glossary.md#main-thread), not a thread you started.
- It is safe to call at any time. Empty reads return `0`, `false`, `null` or an empty list.
- If a handler throws, the error is logged and the other handlers still run.
- Don't change scenes or NPCs from a handler.

## Under the hood

How the framework reads the game, for modders who want to dig deeper.

- `Relationship.CheckDateAndTrigger` raises `RelationshipChanged`, only when the score changed.
- Scores live on `Relationship` assets. No game method sets `RelationshipController.RelationshipInfo.disposition` or `dispositionTarget`.
