---
title: Surfaces
parent: Objects and devices
grand_parent: API reference
nav_order: 17
---

# Surfaces

## What it is

Walls, floors and ceilings in a player's room can be covered with wallpaper, and each wallpaper comes in one or more looks (skins). This API lets your mod list those surfaces, see which wallpaper is on each one, put a wallpaper on, strip one off, and get told when either happens. It does not cover the plain paint or material of a surface that has no wallpaper, the scraping the player does to take wallpaper off, or the wallpaper items themselves.

## When to use it

- Redecorate a room as part of a quest or a gift.
- Reset a surface to bare when a tenant moves out.
- React when the player puts up or strips wallpaper.

## Quick example

This mod writes a log line each time wallpaper is put on or taken off a surface. Once a save is loaded, it also logs how many surfaces already have wallpaper.

```csharp
using ObenseuerCore;

[ModInfo("example.wallpaperwatch", "Wallpaper Watch", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class WallpaperWatch : FrameworkMod
{
    protected override void OnLoad()
    {
        Surfaces.Changed += args =>
        {
            if (args.WallpaperItemId == 0) Log.Info("Wallpaper removed from " + args.Id);
            else Log.Info("Wallpaper " + args.WallpaperItemId + " (skin " + args.SkinIndex + ") put on " + args.Id);
        };
        Events.WhenReady(() =>
        {
            int covered = 0;
            foreach (SurfaceInfo surface in Surfaces.All())
                if (surface.WallpaperItemId != 0) covered++;
            Log.Info(covered + " of " + Surfaces.All().Count + " surfaces have wallpaper.");
        });
    }
}
```

`Changed` is an event: the framework calls your code each time it happens. A `WallpaperItemId` of `0` means the surface has no wallpaper. `Events.WhenReady` waits until a save is loaded before it runs your code. See [Using events](../guides/getting-started/using-events.md).

## Common tasks

Read what is on one surface. Take the id from `Surfaces.All()`. `Get` gives `null` when no surface has that id.

```csharp
string id = "some id from Surfaces.All()";

SurfaceInfo surface = Surfaces.Get(id);
if (surface != null) Log.Info("Wallpaper " + surface.WallpaperItemId + ", skin " + surface.SkinIndex);
```

Put White Wallpaper (item number 19470) on a surface in its first skin. The call gives `false` when the game would not allow it.

```csharp
bool put = Surfaces.SetWallpaper(id, 19470, 0);
Log.Info(put ? "The wallpaper is on." : "The game would not put that wallpaper there.");
```

Strip the wallpaper off a surface.

```csharp
bool stripped = Surfaces.Remove(id);
Log.Info(stripped ? "The surface is bare." : "Could not strip the wallpaper.");
```

Check whether the change event works.

```csharp
if (!Surfaces.IsAvailable) Log.Warn("Wallpaper events are off.");
```

## Reference

Text in brackets at the end of a note is extra detail for experienced modders, and you can skip it on a first read.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Surfaces</code> <span>the surfaces in the scene you are in</span></p>

This is the class to start from. `All()` and `Get(id)` read the surfaces, `SetWallpaper` puts a wallpaper on one, `Remove` strips it, and `Changed` tells you when either happens.

<div class="api-pair" markdown="1">

```csharp
public static class Surfaces
{
    public static List<SurfaceInfo> All();
    public static SurfaceInfo Get(string id);
    public static bool SetWallpaper(string id, int itemId, int skinIndex);
    public static bool Remove(string id);

    public static event Action<SurfaceArgs> Changed;

    public static bool IsAvailable { get; }
}
```

| Method | Returns | What it does |
|---|---|---|
| `All()` | `List<SurfaceInfo>` | Every surface in the scene you are in. |
| `Get(id)` | `SurfaceInfo` | The surface with this id. It is `null` when no surface has it. |
| `SetWallpaper(id, itemId, skinIndex)` | `bool` | Puts a wallpaper on the surface in that look. It is `true` when the surface now shows it. |
| `Remove(id)` | `bool` | Strips the wallpaper. It is `true` when the surface has none afterwards. |

| Property | Type | What it means |
|---|---|---|
| `IsAvailable` | `bool` | `false` when the `Changed` event could not be set up. |

| Event | Gives you | When it fires |
|---|---|---|
| `Changed` | `SurfaceArgs` | A surface's wallpaper or skin has just changed. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>SurfaceInfo</code> <span>a snapshot of one surface</span></p>

`All()` and `Get(id)` give you one of these. `WallpaperItemId` is `0` when there is no wallpaper, and `SkinIndex` is the look that is showing. <span class="tech">(It is built from the game's `FurnitureSurfaceArea`.)</span>

<div class="api-pair" markdown="1">

```csharp
public sealed class SurfaceInfo
{
    public string Id { get; }
    public int WallpaperItemId { get; }
    public int SkinIndex { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | The surface's id: its save id (see the [glossary](../glossary.md#guid)), or its place in the scene if it has none. |
| `WallpaperItemId` | `int` | The item number of the wallpaper on it. `0` means no wallpaper. |
| `SkinIndex` | `int` | Which look of that wallpaper is showing, counting from 0. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>SurfaceArgs</code> <span>what the Changed event tells you</span></p>

The `Changed` event gives you one of these, with the wallpaper and look now on the surface. `WallpaperItemId` is `0` when the wallpaper was removed.

<div class="api-pair" markdown="1">

```csharp
public sealed class SurfaceArgs
{
    public string Id { get; }
    public int WallpaperItemId { get; }
    public int SkinIndex { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | The same id as `SurfaceInfo.Id`. |
| `WallpaperItemId` | `int` | The wallpaper now on the surface. `0` means none. |
| `SkinIndex` | `int` | The look now showing. |

</div>
</div>

### Things to know

- `All()` and `Get(id)` read the surface as it is right now. A `SurfaceInfo` is a snapshot, so ask again for fresh values. `All()` only lists surfaces that are switched on in the scene you are in. <span class="tech">(The id is the object's save GUID, or the scene path when it has none. The surfaces are the game's `FurnitureSurfaceArea` objects.)</span>
- `SetWallpaper(id, itemId, skinIndex)` puts that wallpaper on the surface in that look. It returns `true` when the surface now shows that item and look. If the surface already shows them, nothing is done and you still get `true`.
- `SetWallpaper` gives `false` when the game would not allow it. That is the case for an unknown surface id, an item number that is not a wallpaper, a look the wallpaper does not have, a wallpaper that is not allowed on that kind of surface (for example a wall wallpaper on a floor), and while a save is loading or saving. <span class="tech">(The surface rule is the game's `FurnitureSurfaceArea.AllowedToPlace`, which checks the wallpaper's surface area flags and the surface's allowed surface types. Before it calls the game, the framework loads the wallpaper's look textures (`LoadVariants`, the same as the game's own placing code does). That step has not been tested in a running game.)</span>
- A look number that is below 0 or past the wallpaper's last look is refused. The game would save such a number and then fail with it, so the framework does not let it through. <span class="tech">(A negative index throws when the surface updates, and again on every load. An index past the last look shows no material, and the surface keeps its old one.)</span>
- `Remove(id)` strips the wallpaper and brings back the surface's normal look. It does not give the player an item and does not use anything up. It returns `true` when the surface has no wallpaper afterwards, which is also the case if it had none to begin with. <span class="tech">(It calls the game's `RemoveWallpaper`, which puts back the default material and clears the saved wallpaper.)</span>
- `Remove` does not do what the player does when scraping. The player's scraping wears down the scraper and can destroy it. `Remove` skips all of that. <span class="tech">(It does not run the game's `TryRemoveWallpaper`, which costs scraper integrity.)</span>
- `Changed` fires for every change, whether your mod, the player or the game made it. It fires only when the wallpaper item or the look really changes, so putting the same wallpaper on again raises nothing.
- `Changed` does not fire when a save loads. The game puts the saved wallpaper back before it applies it, so the framework sees no change. If your handler should ignore changes made while a save loads, check `SaveData.Busy` (true while a save is loading or being written). <span class="tech">(It is raised after the game's `SetWallpaper` and `RemoveWallpaper`. On a load, the game restores `savedItemId` and `savedSkinIndex` before it calls `SetWallpaper`, so the check before the call already sees the same values.)</span>
- `IsAvailable` is `false` when the change event could not be set up. Then `Changed` never fires. See "If something fails to set up" below.

### What you can't do (yet)

- You can't change the paint or material of a surface that has no wallpaper. Only the wallpaper and its look are covered.
- You can't make the player's scraping happen or change what it costs. `Remove` is the only way to strip a surface here.
- You can't create or edit wallpaper items here. This page only puts an existing wallpaper item on a surface. The items are part of the game's item data.

## Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\obenseuer.moddingframework.surfaces.log`. Source files: `src/Api/Surfaces.cs` and `src/Api/SurfacesPatches.cs`.

Everything not listed in this reference is internal and may change without notice.

### When you can call it

- Call everything from your mod's normal code, not from a background thread you started. The game only works safely on one thread, the main thread (see the [glossary](../glossary.md#main-thread)). This goes for the code that handles `Changed` too.
- It is safe to call any of it at any time, even on the main menu or with no save loaded. `All()` gives an empty list, `Get` gives `null`, and `SetWallpaper` and `Remove` give `false`. Nothing throws. A `null` or empty id simply finds nothing.
- `SetWallpaper` and `Remove` do nothing while a save is loading or saving. They return `false`.
- Your `Changed` handler runs inside the game's own call, right after the change. If your handler throws an error, the framework logs it and still runs the other handlers.

### If something fails to set up

The framework changes some of the game's methods when it starts, so it can tell your mod when wallpaper changes. If a game update breaks one of them, the framework removes all the changes of the [feature group](../glossary.md#feature-group) that depends on it, turns the group off, and writes error lines to the group's log file in `BepInEx\logs`. The rest of the framework keeps working. The name you will see in the log is the feature group `obenseuer.moddingframework.surfaces`.

- If the change event cannot be set up, `Changed` stops and `IsAvailable` is `false`. <span class="tech">(The framework needs the two private fields `savedItemId` and `savedSkinIndex` on `FurnitureSurfaceArea`, and it changes two game methods: `SetWallpaper(ItemWallpaperData, int, int)` and `RemoveWallpaper()`. If a field or a method is missing, the feature group is turned off. `IsAvailable` is `false` then.)</span>
