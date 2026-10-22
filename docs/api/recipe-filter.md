---
title: Recipe filter
parent: Items and crafting
grand_parent: API reference
nav_order: 17
---

# Recipe filter

Status: experimental.

## What it is

The game shows the recipes of a crafting panel as a list of buttons. This API lets your mod decide which recipes appear in that list. You give the framework a function that is called for each recipe, with the recipe's details, and it returns `true` to keep the recipe or `false` to hide it. A recipe is shown only when every mod's function keeps it.

## When to use it

- Show only the recipes the player has, hiding the recipes that need no learning and that the player has not collected.
- Let the player search the recipe list by name.

## Quick example

This mod hides the recipes that need no learning and that the player has not collected, so the list shows only the recipes the player has or has learned. It is a plain rule, so it runs the same way at every crafting panel.

```csharp
using ObenseuerCore;

[ModInfo("example.knownonly", "Known Only", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class KnownOnly : FrameworkMod
{
    protected override void OnLoad()
    {
        if (!RecipeFilter.Add(Mod.Id, recipe => recipe.Known)) Log.Info("The recipe filter was not added.");
    }
}
```

`Mod.Id` is your mod's id (see the [glossary](../glossary.md#mod-id)). `RecipeInfo` is the recipe the framework gives you, see [Recipes and crafting](recipes.md). `RecipeInfo.Known` is `true` when the player has or has learned the recipe. For the recipes the list shows it is `false` only for the recipes that need no learning, because the game does not list a recipe that needs learning until the player has it.

## Common tasks

Search the list by name. Your mod keeps the text in its own field, and the function keeps the recipes whose name contains it. `Matches` ignores upper and lower case, and an empty text keeps every recipe.

```csharp
private string searchText = string.Empty;

private bool Search(RecipeInfo recipe)
{
    return RecipeFilter.Matches(recipe, searchText);
}

RecipeFilter.Add(Mod.Id, Search);
```

Remove your filter, for example when the player turns your option off. `Remove` returns `false` when your mod had no filter.

```csharp
bool removed = RecipeFilter.Remove(Mod.Id);
```

## Reference

Text in brackets at the end of a note is extra detail you can skip on a first read.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>RecipeFilter</code> <span>deciding which recipes the list shows</span></p>

Call `Add` with your mod id and a function, and `Remove` to take your function away. `Matches` is a helper for name searches.

<div class="api-pair" markdown="1">

```csharp
public static class RecipeFilter
{
    public static bool IsAvailable { get; }
    public static bool Add(string modId, Func<RecipeInfo, bool> keep);
    public static bool Remove(string modId);
    public static bool Matches(RecipeInfo recipe, string text);
}
```

| Method | Returns | What it does |
|---|---|---|
| `Add(modId, keep)` | `bool` | Adds your function. `true` when it was added. Calling it again with the same mod id replaces the function. |
| `Remove(modId)` | `bool` | Removes your function. `false` when your mod had none. |
| `Matches(recipe, text)` | `bool` | `true` when the recipe's name contains the text, ignoring upper and lower case. An empty text matches every recipe. A `null` recipe does not match. |
| `IsAvailable` | `bool` | `false` when the framework could not change the recipe list, so no filter is used. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>RecipeInfo</code> <span>the recipe your function is asked about</span></p>

The details of one recipe, as the framework reads them from the game.

<div class="api-pair" markdown="1">

```csharp
public sealed class RecipeInfo
{
    public int Id { get; }
    public string Name { get; }
    public string Station { get; }
    public bool Known { get; }
    public List<RecipeIngredient> Ingredients { get; }
    public List<ItemCount> Results { get; }
}
```

| Property | Type | What it is |
|---|---|---|
| `Id` | `int` | The recipe's id. |
| `Name` | `string` | The name the list shows. |
| `Station` | `string` | The name of the recipe's type. |
| `Known` | `bool` | `true` when the player has the recipe. |
| `Ingredients` | `List<RecipeIngredient>` | The items the recipe uses. |
| `Results` | `List<ItemCount>` | The items the recipe makes. |

</div>
</div>

### Things to know

- Your function is called for each recipe when the game builds a recipe list. A recipe is shown only when every mod's function keeps it. A recipe that no function refuses is shown as the game would show it.
- The framework does not rebuild the list when your text changes. The list changes the next time the game builds it, for example when the player opens the panel again. This API cannot force a rebuild.
- The filter covers the recipe lists of the manufacturing, manufacturing process and growing panels, which the framework checks in build 25969064 of the game. <span class="tech">(The game fills these lists through `RecipeListUI.UpdateRecipes`, and the filter is on `RecipeListUI.CreateButton`. The basic crafting station panel is not checked.)</span>
- Your function should be cheap. It runs once for each recipe in the list.
- A filter that throws does not hide the recipe. The framework keeps the recipe and logs the error once.
- Filters are not saved. Add yours again when the game starts, for example from `Events.WhenReady`.

### Limits

- `Add` returns `false` and logs once when the mod id is not valid (1 to 64 letters, digits, `.`, `_` or `-`), the function is `null`, or the framework could not change the recipe list.
- `Remove` and `Matches` do not throw.
- A filter that calls `Add` or `Remove` while it runs is safe. The framework runs the filters on a copy it takes before the call, so a change made from inside a filter applies to the next recipe list.
- Hiding a recipe in the list does not stop the game from knowing it. The player can still learn or use it by other means.
- Selecting a recipe with the controller or the keyboard is mapped onto the list you can see, so the same recipe as a mouse click is picked. When the framework cannot map it (a station without a recipe list of its own, or a list that does not match the open panel) the game's own behaviour is used instead.
