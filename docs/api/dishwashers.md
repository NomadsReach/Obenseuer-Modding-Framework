# Dishwashers API

Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 5.4.23.5.
Namespace: `ObenseuerCore`. Patch owner: `obenseuer.moddingframework.dishwashers`.
Files: `src/Api/Dishwashers.cs`, `src/Api/DishwashersPatches.cs`.

Everything not listed here is internal. Dishwasher speed stays in `TweaksDishwasher`. The general station list is `Crafting.All` (`docs/api/recipes.md`), which also includes dishwashers. This area adds the dishwasher-specific reads and the cycle event.

## Wiring

```csharp
internal static void Dishwashers.Init();
```

Core calls this from `Core.Awake`.

`Init` installs one hook, `manufacturing.CraftDelayed`, and reads the station's private craft list. If either is missing, the owner is disabled and one warning is logged to `BepInEx\logs\obenseuer.moddingframework.dishwashers.log`. The reads keep working; `Progress` returns `0` without the craft list, and `Finished` stops.

## Threading and safety

- Main thread only.
- `Dishwashers.IsAvailable` is false when the hook was not installed or the owner was disabled.
- Every member is safe at any time. With no save loaded, or on the main menu, reads return an empty list, `false` or `0`.
- Nothing throws. There are no writes, and no argument throws.
- `Finished` is raised synchronously after the game has finished the cycle and created its outputs. A handler that throws is logged and the other handlers still run (`Events.Raise`).

## API

```csharp
public static class Dishwashers
{
    public static List<DishwasherInfo> All();
    public static DishwasherInfo Get(string id);
    public static List<ItemStackInfo> Contents(string id);
    public static bool Ready(string id);
    public static float Progress(string id);

    public static bool IsAvailable { get; }
    public static event Action<DishwasherInfo> Finished;
}

public sealed class DishwasherInfo
{
    public string Id { get; }              // CraftingBase.GUID
    public Vector3 Position { get; }       // world position
    public bool Busy { get; }              // a wash is running (CraftingBase.CraftingIProgress)
}
```

- `All` lists every dishwasher in the scene that has a `GUID`. A dishwasher with no `GUID` cannot be addressed, so it is left out.
- `Contents` lists the items in the dishwasher's input/output storage (`CraftingBase.InputOutputStorage`). It reuses `Containers.GetItems`, so the list matches the container view. It holds the dishes and the washing powder.
- `Get(id)` returns one `DishwasherInfo`, or `null` when no dishwasher has that id.
- `Ready` means powered and idle: `true` when the dishwasher is powered and switched on (`CraftingBase.PoweredAndActive`) and no wash is running at all. It equals powered and not `Busy`. A machine that allows several washes at once (`manufacturing.AllowSimultaneous` above 1) is not `Ready` while any wash runs, although the game would accept another. `Ready` does not check that the contents can be washed (the washing powder, the recipe, the skill). Use `Contents` for that.
- `Progress` is the running wash's progress, `0` to `1`, from the first wash in the machine's craft list. It is `0` when no wash is running. A machine that allows several washes at once reports only its first one.
- `Finished` fires after a wash cycle ends (`manufacturing.CraftDelayed`), for dishwashers only. The payload is the dishwasher's `DishwasherInfo` after the wash, so `Busy` is `false` unless another wash is still running.

## Overlap with Crafting

`Crafting.Crafted` (`docs/api/recipes.md`) also fires when a dishwasher creates its output, because a dishwasher is a `manufacturing` station, so one wash raises both events: `Crafting.Crafted` first (from `CreateItems`, with the recipe and the results), then `Dishwashers.Finished` (from the end of `CraftDelayed`, with the dishwasher's id and state). Use `Crafting.Crafted` for the washed items and recipe, and `Dishwashers.Finished` when you only want dishwashers and their id. `Dishwashers.Finished` is not raised for any other crafting station.

## Not wrapped

- Starting a wash. `manufacturing.Craft()` is driven by the manufacturing panel and reads its selection, so a mod cannot call it safely. This matches the decision in `docs/api/recipes.md`.
- The dishwasher's washing powder, categories and blacklist. They are set in the game's data and are not exposed.
- The recipe for a dishwasher item (`Dishwasher.GenerateRecipe`). It is built from the item, not from a stored recipe, and the speed tweak is in `TweaksDishwasher`.
