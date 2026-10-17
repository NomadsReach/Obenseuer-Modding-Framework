---
title: Dishwashers
parent: Objects and devices
grand_parent: API reference
nav_order: 4
---

# Dishwashers

Status: experimental.

## What it is

A dishwasher in the game is a machine that washes dirty dishes. You put the dishes and washing powder in, and after a while clean dishes come out. This API lets a mod list the dishwashers in the scene you are in, see whether each one is running, how far along it is and what is inside, and get told when a wash finishes.

Dishwashers are also crafting stations, so the general crafting list in [Recipes and crafting](recipes.md) (`Crafting.All`) includes them too. This page adds the things that are only for dishwashers. The speed of dishwashers is set by the framework's own tweak, not by this API.

## When to use it

- Show or log a message when a dishwasher finishes a wash.
- Check whether a dishwasher is free to use, or how far through its wash it is.
- Look at the dishes and washing powder inside a dishwasher.

## Quick example

This mod writes a log line each time a dishwasher finishes a wash. Once a save is loaded, it also lists the dishwashers it can see and whether each one is running.

```csharp
using ObenseuerCore;

[ModInfo("example.dishwatch", "Dishwasher Watch", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class DishWatch : FrameworkMod
{
    protected override void OnLoad()
    {
        Dishwashers.Finished += info => Log.Info("Dishwasher " + info.Id + " finished a wash at " + info.Position);
        Events.WhenReady(() =>
        {
            foreach (DishwasherInfo dishwasher in Dishwashers.All())
                Log.Info(dishwasher.Id + " busy: " + dishwasher.Busy + ", ready: " + Dishwashers.Ready(dishwasher.Id));
        });
    }
}
```

`Finished` is an event: the framework calls your code each time it happens. `WhenReady` runs your code once a save is loaded (see [Using events](../guides/getting-started/using-events.md)).

## Common tasks

Look at how far a dishwasher is through its wash. The number goes from 0 to 1.

```csharp
foreach (DishwasherInfo dishwasher in Dishwashers.All())
    Log.Info(dishwasher.Id + " is " + Dishwashers.Progress(dishwasher.Id) * 100f + "% done");
```

List what is inside one dishwasher (the dishes and the washing powder).

```csharp
foreach (ItemStackInfo stack in Dishwashers.Contents("some-dishwasher-id"))
    Log.Info("item " + stack.ItemId + " x" + stack.Count);
```

Find one dishwasher by its id. `Get` gives `null` when there is no dishwasher with that id.

```csharp
DishwasherInfo dishwasher = Dishwashers.Get("some-dishwasher-id");
if (dishwasher != null) Log.Info("Found at " + dishwasher.Position);
```

Check that the wash events work before you rely on them.

```csharp
if (!Dishwashers.IsAvailable) Log.Warn("Dishwasher events are off");
```

## Reference

This is the full list of what the API gives you, one section for each part. In every code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

### Dishwashers

`Dishwashers` finds the dishwashers in the scene you are in. Each one is a `DishwasherInfo`, a snapshot of it at the moment you asked. The snapshot does not update by itself, so ask again for fresh values. Items are described by `ItemStackInfo`, which is on the [Inventory](inventory.md) page.

```csharp
public static class Dishwashers
{
    public static List<DishwasherInfo> All();               // every dishwasher in the scene you are in
    public static DishwasherInfo Get(string id);            // the dishwasher with this id; null when there is none
    public static List<ItemStackInfo> Contents(string id);  // the items inside the dishwasher; an empty list when there is none
    public static bool Ready(string id);                    // true when the dishwasher is switched on, has power and is not washing
    public static float Progress(string id);                // how far through the wash it is, from 0 to 1; 0 when it is not washing

    public static bool IsAvailable { get; }                 // false when the wash event could not be set up
    public static event Action<DishwasherInfo> Finished;    // a dishwasher has just finished a wash
}

public sealed class DishwasherInfo    // a snapshot of one dishwasher
{
    public string Id { get; }              // this dishwasher's id; use it with Get, Contents, Ready and Progress
    public Vector3 Position { get; }       // where the dishwasher is, in world coordinates
    public bool Busy { get; }              // true when a wash is running
}
```

- **`All()` lists the dishwashers in the scene you are in, but leaves out any that have no id.** A dishwasher with no id can't be found again later, so it is not listed.
  - Technical: the id is the station's `CraftingBase.GUID`. Dishwashers with an empty `GUID` are skipped.
- **`Get(id)` gives you one dishwasher, or `null` when no dishwasher has that id.**
- **`Contents(id)` lists what is in the dishwasher: the dishes and the washing powder.** It gives an empty list when there is no such dishwasher. It shows the same list that `Containers.GetItems` shows for the dishwasher's storage.
  - Technical: it reads `CraftingBase.InputOutputStorage` and reuses `Containers.GetItems`.
- **`Ready(id)` is `true` when the dishwasher has power, is switched on and is not washing at all.** It is the same as "has power and is not `Busy`". A dishwasher that can run several washes at once counts as not ready while any wash runs, even though the game would accept another. `Ready` does not check that the contents can be washed (washing powder, the recipe, the skill), so look at `Contents` for that.
  - Technical: it is `CraftingBase.PoweredAndActive` and no `CraftingIProgress`. A machine with `manufacturing.AllowSimultaneous` above 1 is not ready while any wash runs.
- **`Progress(id)` is how far through the wash the dishwasher is, from 0 (just started) to 1 (done).** It is `0` when nothing is washing, or when there is no such dishwasher. A dishwasher that runs several washes at once only reports the first one.
  - Technical: it reads the first entry of the private craft list `craftsInProgresses` of `manufacturing`. Without that list it returns `0`.
- **`Finished` fires after a wash ends, for dishwashers only.** It gives you the dishwasher as it is after the wash, so `Busy` is `false` unless another wash is still running. The wash has finished and the clean dishes are in the dishwasher by the time your code runs.
  - Technical: it fires from the end of `manufacturing.CraftDelayed`, after the game has made the outputs.
- **`IsAvailable` is `false` when the wash event could not be set up.** The reads keep working when it is `false`. See [If something fails to set up](#if-something-fails-to-set-up).

### How this relates to the crafting event

- **One wash raises two events: `Crafting.Crafted` first, then `Dishwashers.Finished`.** A dishwasher is a crafting station, so `Crafting.Crafted` (on the [Recipes and crafting](recipes.md) page) also fires when it makes its clean dishes. Use `Crafting.Crafted` when you want the washed items and the recipe. Use `Dishwashers.Finished` when you only want dishwashers and their id. `Dishwashers.Finished` never fires for any other crafting station.
  - Technical: `Crafting.Crafted` comes from `CreateItems`, with the recipe and the results. `Dishwashers.Finished` comes from the end of `CraftDelayed`, with the dishwasher's id and state.

### What you can't do (yet)

- **You can't start a wash from your mod.** The game starts a wash from its crafting panel and reads the player's choice there, so a mod calling it directly would not be safe. This matches the choice made for crafting stations on the [Recipes and crafting](recipes.md) page.
  - Technical: `manufacturing.Craft()` is driven by the manufacturing panel and reads its selection.
- **You can't read the dishwasher's washing powder, categories or blacklist.** They are part of the game's own data and the framework does not give them to you.
- **You can't read the recipe of a dishwasher item.** The game builds it from the item instead of storing a recipe, and the framework's speed tweak handles the wash speed.
  - Technical: the recipe comes from `Dishwasher.GenerateRecipe`. The speed tweak is `TweaksDishwasher`.

## Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\obenseuer.moddingframework.dishwashers.log`.

Everything not listed in this reference is internal and may change without notice. Items marked experimental may change too.

### When you can call it

- **Call everything from your mod's normal code, not from a background thread you started.** The game and Unity only work safely on one thread, the main thread (see the [glossary](../glossary.md#main-thread)).
- **It is safe to call any of it at any time, even on the main menu or while a save is loading.** If there is nothing to read yet, you get an empty list, `false` or `0`.
- **Nothing here throws an error, and nothing here changes the game.** No argument can make a call throw.
- **Your `Finished` handler runs inside the game's own call, one at a time.** If one of your handlers throws an error, the framework logs it and still runs the other handlers.
  - Technical: `Finished` is raised synchronously after the game has finished the cycle and created its outputs.
- **`Finished` also fires while a save is loading.** If you want to ignore those, check `SaveData.Busy`, which is `true` while a save is loading or being written.

### If something fails to set up

The framework changes some of the game's methods when it starts, so it can tell your mod when a wash finishes. If a game update breaks one of them, only that feature turns off, and the framework writes error lines and a warning to the log. The rest of the API keeps working.

- **The wash event depends on the feature group `obenseuer.moddingframework.dishwashers`.** This is the name you will see in the log. If the framework can't change the wash method, or can't find the dishwasher's craft list, it turns the group off and writes error lines and a warning to `BepInEx\logs\obenseuer.moddingframework.dishwashers.log`. Then `Finished` stops and `IsAvailable` is `false`. The reads keep working, except that `Progress` gives `0` when the craft list is missing.
  - Technical: the framework installs one change, on `manufacturing.CraftDelayed`, and reads the private craft list with `AccessTools.Field`.
