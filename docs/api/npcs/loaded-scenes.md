---
title: NPCs in the loaded scenes
parent: NPCs
grand_parent: People
nav_order: 3
---

# NPCs in the loaded scenes

`Npcs.InScene()` finds every NPC object in loaded scenes.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Npcs</code> <span>the scene search</span></p>
<div class="api-pair" markdown="1">

```csharp
public static class Npcs
{
    public static List<SceneNpcInfo> InScene();
}
```

| Method | Returns | What it does |
|---|---|---|
| `InScene()` | `List<SceneNpcInfo>` | Every NPC object, whether it is switched on or off. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>SceneNpcInfo</code> <span>one NPC object</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class SceneNpcInfo
{
    public string Scene { get; }
    public string Path { get; }
    public string Handle { get; }
    public bool IsDirector { get; }
    public int Id { get; }
    public string Name { get; }
    public string DisplayName { get; }
    public Vector3 Position { get; }
    public bool Active { get; }
    public bool Present { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Scene` | `string` | The scene the object is in. |
| `Path` | `string` | Where the object is in its scene, like `Root/Child`. |
| `Handle` | `string` | A name that is kept across sessions. |
| `IsDirector` | `bool` | It is `true` when `All()` lists the NPC. |
| `Id` | `int` | The character number, or `-1` if there is none. |
| `Name` | `string` | The first name, or the object's name if there is none. |
| `DisplayName` | `string` | The full name, or `null` if there is none. |
| `Position` | `Vector3` | The position when `InScene()` ran. |
| `Active` | `bool` | It is `true` when the object is switched on. |
| `Present` | `bool` | It is `true` when the NPC is really there, even if it is switched off. |

</div>
</div>

## Putting it together

Logs NPCs with no game record and counts those really present.

<div class="api-example" markdown="1">

```csharp
using ObenseuerCore;

[ModInfo("example.scenenpcs", "Scene NPCs", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class SceneNpcs : FrameworkMod
{
    protected override void OnLoad()
    {
        Events.WhenReady(() =>
        {
            int present = 0;
            foreach (SceneNpcInfo npc in Npcs.InScene())
            {
                if (npc.Present) present++;
                if (!npc.IsDirector) Log.Info(npc.Name + " in " + npc.Scene + " at " + npc.Path + " has no entry in Npcs.All()");
            }
            Log.Info(present + " NPCs are really in the loaded scenes.");
        });
    }
}
```

| Step | What it does |
|---|---|
| `Npcs.InScene()` | Searches loaded scenes once. |
| `npc.IsDirector` | It is `false` when the NPC has no record. |

</div>

## Things to know

- `InScene()` is slow; call it on an event or timer.
- NPCs with no record appear here but not in `All()`. Objects with no NPC component appear in neither.
- The game switches off NPCs beyond 20 metres or off camera; they still count as `Present` unless they are sent away.

## Limits

- If another mod hides an NPC on purpose while the distance hide is on, `Present` still says `true`.
- `Handle` is stable only for map-placed NPCs. `Path` can repeat for objects with odd names.
