---
title: Finding NPCs
parent: NPCs
grand_parent: People
nav_order: 2
---

# Finding NPCs

Each `NpcInfo` is a snapshot; see [Characters](../characters.md).

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Npcs</code> <span>NPC list and events</span></p>
<div class="api-pair" markdown="1">

```csharp
public static class Npcs
{
    public static List<NpcInfo> All();
    public static NpcInfo Get(int id);
    public static NpcInfo Find(string name);
    public static bool IsAvailable { get; }
    public static event Action<NpcInfo> Spawned;
    public static event Action<NpcInfo> Despawned;
    public static event Action<NpcInfo> Died;
}
```

| Method | Returns | What it does |
|---|---|---|
| `All()` | `List<NpcInfo>` | Every NPC the game knows about. |
| `Get(int id)` | `NpcInfo` | The NPC with this id, or `null` if there is none. |
| `Find(string name)` | `NpcInfo` | The first NPC whose first or full name matches, or `null` if none. Throws if the name is empty. |

| Property | Type | What it means |
|---|---|---|
| `IsAvailable` | `bool` | It is `false` when the spawn, despawn or death events are off. |

| Event | Gives you | When it fires |
|---|---|---|
| `Spawned` | `NpcInfo` | An NPC appears. |
| `Despawned` | `NpcInfo` | An NPC leaves the scene. |
| `Died` | `NpcInfo` | It fires once, when an NPC dies. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>NpcInfo</code> <span>one NPC snapshot</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class NpcInfo
{
    public int Id { get; }
    public string Name { get; }
    public string DisplayName { get; }
    public Vector3 Position { get; }
    public bool Alive { get; }
    public string CurrentScene { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `int` | The number you pass to `Npcs.Get`. |
| `Name` | `string` | The first name, or `null` if there is none. |
| `DisplayName` | `string` | The full name, or `null` if there is none. |
| `Position` | `Vector3` | The live position, or the last known one if the NPC is not active in the scene. |
| `Alive` | `bool` | It is `false` only for a dead NPC that is in a scene. |
| `CurrentScene` | `string` | The scene the game reports, or `null` if it is unknown. |

</div>
</div>

## Putting it together

Logs each spawn, then finds one NPC by name.

<div class="api-example" markdown="1">

```csharp
using ObenseuerCore;

[ModInfo("example.npcroster", "NPC Roster", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class NpcRoster : FrameworkMod
{
    protected override void OnLoad()
    {
        Npcs.Spawned += npc => Log.Info("Spawned: " + (npc.DisplayName ?? "an NPC with no character") + " in " + npc.CurrentScene);
        Events.WhenReady(() =>
        {
            NpcInfo npc = Npcs.Find("Some Name");
            if (npc == null)
            {
                Log.Info("Nobody has that name.");
                return;
            }
            NpcInfo exact = Npcs.Get(npc.Id);
            if (exact != null)
                Log.Info(exact.Name + " is at " + exact.Position + " in " + exact.CurrentScene);
        });
    }
}
```

| Step | What it does |
|---|---|
| `Npcs.Spawned` | Logs each spawn. |
| `Npcs.Find("Some Name")` | Returns the first match, or `null` if there is none. |

</div>

## Things to know

- `All()` misses NPCs placed straight into a scene; use `InScene()`.
- `Spawned` fires before a `Character.Spawn` NPC starts; both it and `Despawned` fire during save loads.
- `Died` does not repeat on load. `Despawned` ignores NPCs that were only switched off because they walked away.

## Limits

- The game forgets deaths of away NPCs, so `Died` misses them and `Alive` stays `true`.
- In 0.5.08 the game never kills NPCs, so `Alive` changes mostly when a mod kills one.
- Two NPCs can share a name; use `Get(id)`.
