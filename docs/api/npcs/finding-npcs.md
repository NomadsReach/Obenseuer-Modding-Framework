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
| `All()` | `List<NpcInfo>` | Every known NPC. |
| `Get(int id)` | `NpcInfo` | NPC with this id; `null` if none. |
| `Find(string name)` | `NpcInfo` | Name match; `null` if none, throws if empty. |

| Property | Type | What it means |
|---|---|---|
| `IsAvailable` | `bool` | `false` if those events are off. |

| Event | Gives you | When it fires |
|---|---|---|
| `Spawned` | `NpcInfo` | An NPC appears. |
| `Despawned` | `NpcInfo` | An NPC leaves the scene. |
| `Died` | `NpcInfo` | Once, when an NPC dies. |

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
| `Id` | `int` | Number for `Npcs.Get`. |
| `Name` | `string` | First name; `null` if none. |
| `DisplayName` | `string` | Full name; `null` if none. |
| `Position` | `Vector3` | Live position, or last known. |
| `Alive` | `bool` | `false` only for a dead NPC in scene. |
| `CurrentScene` | `string` | Scene the game says; `null` if unknown. |

</div>
</div>

### Putting it together

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
| `Npcs.Find("Some Name")` | First match, or `null`. |

</div>

### Things to know

- `All()` misses NPCs placed straight into a scene; use `InScene()`.
- `Spawned` fires before a `Character.Spawn` NPC starts; both it and `Despawned` fire during save loads.
- `Died` does not repeat on load. `Despawned` ignores NPCs only switched off for walking away.

### Limits

- The game forgets deaths of away NPCs, so `Died` misses them and `Alive` stays `true`.
- In 0.5.08 the game never kills NPCs, so `Alive` changes mostly when a mod kills one.
- Two NPCs can share a name; use `Get(id)`.
