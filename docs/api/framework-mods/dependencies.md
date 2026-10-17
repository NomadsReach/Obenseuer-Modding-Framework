---
title: Dependencies
parent: Framework mods
grand_parent: Framework basics
nav_order: 6
---

# Dependencies

A dependency says another mod must load before yours, and optionally how new it must be.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Loader</code> <span>finding out which mods are running</span></p>

Use it to check whether another mod is running, for example before you use its features. Call `Has` from `Events.WhenReady`, not from `Start`.

<div class="api-pair" markdown="1">

```csharp
public static class Loader
{
    public static bool Has(string id);
}
```

| Method | Returns | What it does |
|---|---|---|
| `Has(id)` | `bool` | `true` when a mod with this id is running. |

</div>
</div>

## Things to know

- `[ModDependency(id)]` or `[ModDependency(id, minVersion)]` names a framework mod or a BepInEx plugin that must load first. The oldest version is compared with the other mod's version.
- Dependencies load first, and the other mods load in order of their ids.
- To use another mod only when it is there, call `Loader.Has(id)` from inside `Events.WhenReady`. Do not call it in `Start`: it can still be `false` there. The framework loads framework mods after every plugin's `Awake`, so a plugin without a `BepInDependency` on the framework can reach `Start` first.
- `Loader.Has` is `true` when a BepInEx plugin or a framework mod with that id is running. It works from a framework mod and from a BepInEx plugin. It means running, so it is `false` for a framework mod whose `OnLoad` threw an error.
- The framework keeps a mod's saved data when the mod is installed but not running. This covers items, animals, NPCs and landmarks. It means a mod that failed in `OnLoad`, or that is skipped because a dependency is missing, too old or in a loop, keeps its saved data until the problem is fixed. <span class="tech">(The framework's own checks use "installed", not `Loader.Has`. A framework mod that is present on disk but skipped counts as installed.)</span>

## Limits

- If a dependency is missing or too old, your mod is skipped with a warning. The mods that depend on yours are skipped too.
- A BepInEx plugin can't depend on a framework mod. Framework mods load after all plugins, so the plugin would run first.
