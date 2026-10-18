---
title: Blast sites
parent: Objects and devices
grand_parent: API reference
nav_order: 1
---

# Blast sites

## What it is

A blast site is a spot in the game that takes dynamite. The player places dynamite on it, then sets it off with the detonator. Some blast sites are linked in a group, and all of them need dynamite before any of them will go off. This API lets your mod list the blast sites in the scene you are in, read whether each one has dynamite or has already blown, and get told when one goes off. It only reads. It cannot place dynamite or set a site off.

## When to use it

- Run your own code when the player blows up a blast site, for example to open a new area or give a reward.
- Check whether a blast site has already been blown, so your mod does not offer the same job twice.
- Show the player how many dynamite sticks in a linked group are still missing.

## Quick example

This mod writes a log line each time a blast site goes off. Once a save is loaded, it also logs how many blast sites are in the scene and how many of them have dynamite on them.

```csharp
using ObenseuerCore;

[ModInfo("example.blastwatch", "Blast Watch", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class BlastWatch : FrameworkMod
{
    protected override void OnLoad()
    {
        BlastSites.Detonated += id => Log.Info("Dynamite went off at blast site " + id);
        Events.WhenReady(() =>
        {
            int ready = 0;
            foreach (BlastSiteInfo site in BlastSites.All())
                if (site.DynamitePlaced) ready++;
            Log.Info(BlastSites.All().Count + " blast sites here, " + ready + " with dynamite placed.");
        });
    }
}
```

`Detonated` is an event: the framework calls your code each time it happens. `WhenReady` waits until a save is loaded before it runs your code. See [Using events](../guides/getting-started/using-events.md) for more on both.

## Common tasks

Show how far along each blast site's linked group is.

```csharp
foreach (BlastSiteInfo site in BlastSites.All())
    Log.Info(site.Id + ": " + site.LinkedPlaced + " of " + site.LinkedCount + " linked cones have dynamite");
```

Look up one blast site by the id you got from `All()` or from the `Detonated` event, and check whether it has blown. `Get` returns null when no blast site in the scene has that id.

```csharp
BlastSiteInfo one = BlastSites.Get("some-site-id");
if (one != null && one.Detonated) Log.Info("That site has already blown.");
```

Check that the event works before you rely on it.

```csharp
if (!BlastSites.IsAvailable) Log.Warn("The blast site event is not working in this game version.");
```

## Reference

This is the full list of what the API gives you. Text in brackets at the end of a note is extra detail you can skip on a first read.

The game calls the part you place dynamite on a cone, so the names below say "cone". One cone is one blast site.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>BlastSites</code> <span>list blast sites and listen for them going off</span></p>

Use `All` or `Get` to find blast sites, and listen to `Detonated` for the id of each one that goes off. `IsAvailable` is `false` when that event could not be set up.

<div class="api-pair" markdown="1">

```csharp
public static class BlastSites
{
    public static List<BlastSiteInfo> All();
    public static BlastSiteInfo Get(string id);
    public static event Action<string> Detonated;
    public static bool IsAvailable { get; }
}
```

| Method | Returns | What it does |
|---|---|---|
| `All()` | `List<BlastSiteInfo>` | Every blast site in the scene you are in. |
| `Get(id)` | `BlastSiteInfo` | The blast site with this id. `null` when there is none. |

| Property | Type | What it means |
|---|---|---|
| `IsAvailable` | `bool` | `false` when the Detonated event could not be set up. |

| Event | Gives you | When it fires |
|---|---|---|
| `Detonated` | `string` | A blast site has just gone off. You get its id. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>BlastSiteInfo</code> <span>a snapshot of one blast site</span></p>

`All` and `Get` give you one of these for each site. `DynamitePlaced` and `Detonated` say where the site is in the blast, and the `Linked` counts describe its group.

<div class="api-pair" markdown="1">

```csharp
public sealed class BlastSiteInfo
{
    public string Id { get; }
    public bool DynamitePlaced { get; }
    public bool Detonated { get; }
    public int LinkedCount { get; }
    public int LinkedPlaced { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | A name for this site that stays the same between sessions (see the notes below). |
| `DynamitePlaced` | `bool` | `true` while dynamite sits on the site. `false` after it goes off. |
| `Detonated` | `bool` | `true` once the site has gone off. |
| `LinkedCount` | `int` | How many sites are in this site's linked group, counting this one. |
| `LinkedPlaced` | `int` | How many sites in the group have dynamite on them right now. |

</div>
</div>

### Things to know

- `All()` and `Get()` only see blast sites that are switched on in the scene you are in. A site that the game has switched off, or one in a scene that is not loaded, is not in the list, and `Get` returns null for it. When no save is loaded, `All()` returns an empty list. <span class="tech">(Both use `FindObjectsByType` on `InteractableDynamiteCone`, which skips inactive objects and other scenes.)</span>
- `Get()` returns null for an empty or unknown id. It never throws.
- `Id` is a name that identifies one blast site. It normally stays the same every time the game runs. A site the game has no unique id for gets an id made from its place in the scene, and that id changes when the scene changes, so do not save it. <span class="tech">(The id is the cone's `GUID` field, else the unique id of the world object it sits on, else a scene path.)</span>
- `DynamitePlaced` is `false` once a site has gone off, and `Detonated` is `true`. So a site that blew is no longer "placed". A site the player has not touched has both set to `false`.
- `LinkedCount` and `LinkedPlaced` describe the whole group, not only this site. A site that is not linked to anything counts as a group of one. <span class="tech">(They follow the game's own `CountLinkedCones`, and the site counts itself even when the game's list does not hold it. The framework reads the list without writing to it.)</span>
- `Detonated` fires once for each site that goes from "dynamite placed" to "gone off". A group of N linked sites fires N times, once for each site's id. The linked sites fire first and the site the player pressed fires last.
- `Detonated` does not fire when the group is not ready. If one linked site has no dynamite, the game refuses to set any of them off, and every site keeps its dynamite. It also does not fire for a site that has no dynamite, and it does not fire while a save loads. <span class="tech">(The event is raised from the end of the game's `InteractableDynamiteCone.Detonate(bool)`, only when `dynamitePlaced` was true before and `detonated` is true after. A site that blew in an earlier session and is shown as blown on load goes through the game's visual `Detonated()` method, not `Detonate`.)</span>

## Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\obenseuer.moddingframework.blastsites.log`.

Everything not listed in this reference is internal and may change without notice.

### When you can call it

- Call everything from your mod's normal code (`OnLoad`, event handlers, `Update`), not from a background thread you started. The game only runs safely on the main thread (see the [glossary](../glossary.md#main-thread)).
- It is safe to call at any time, even on the main menu or while a save is loading. If there is nothing to read yet, you get an empty list or null.
- Your `Detonated` handler runs inside the game's own call. If your handler throws an error, the framework logs it and the game carries on.
- Reading blast sites from inside a handler is safe, but the group is still going off: the site the player pressed still reads as having dynamite until its own event fires. Use a timer if you need the final state of the whole group.

### If something fails to set up

The framework changes some of the game's methods when it starts, so it can tell your mod when dynamite goes off. If a game update breaks that change, only the `Detonated` event turns off, and the framework writes error lines and a warning to the log. `All()` and `Get()` keep working. Its feature group is `obenseuer.moddingframework.blastsites`, the name you will see in the log. A feature group is the named set of the framework's changes for one feature.

- `BlastSites.IsAvailable` is `false` when the `Detonated` event is switched off. <span class="tech">(It is true only when the `Detonate` method was changed and the private `linkedCones` field was found.)</span>
