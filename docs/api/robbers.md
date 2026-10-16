---
title: Obenseuer Modding Framework robbers API (robbers)
parent: API reference
nav_order: 98.1
---

# Obenseuer Modding Framework robbers API (robbers)

Status: experimental.
Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Log owner: `obenseuer.moddingframework.robbers`. Logs go to `BepInEx\logs\obenseuer.moddingframework.robbers.log`.

Ids are the NPC ids from the `Npcs` facade (`NpcInfo.Id`). A robber is an NPC whose current activity is the game's robber activity.

## Rules

- **Main thread only.** Call every member and handle the event on Unity's main thread.
- **Reads work off-screen.** `All` and `State` read the game's NPC data, so they also see a robber that is not in the loaded scene.
- **Writes need a live robber.** `Aggro`, `Rob`, `Negotiate` and `SetAloof` work only on a robber in the loaded scene. An off-screen robber returns `false`.
- **Owner.** A write takes the `modId` of your mod. An invalid `modId` throws `ArgumentException`, as the other owner calls do.
- **Bad arguments and game failures.** Otherwise a member returns `false` or `null` and nothing throws. The first failure of each member is logged at Error.
- **Save loads and saves.** Writes return `false` while a save loads or saves.

## Reads

```csharp
public sealed class RobberInfo
{
    public int NpcId { get; }
    public string State { get; }     // Idle, Stand, Patrol, Wander, Chase or Aggro
}

public static class Robbers
{
    public static bool IsAvailable { get; }
    public static List<RobberInfo> All();
    public static string State(int npcId);   // null when the NPC is not a robber
}
```

- **IsAvailable** is `false` when the robbed hook was not installed or its owner was disabled. The reads and the other writes keep working.

## Writes

```csharp
public static class Robbers
{
    public static bool Aggro(string modId, int npcId);
    public static bool Rob(string modId, int npcId);
    public static bool Negotiate(string modId, int npcId);
    public static bool SetAloof(string modId, int npcId, bool aloof);
}
```

- **Aggro** makes the robber attack the player, as the game's own aggro call does. It returns `true` only when the robber is now in the Aggro state. The game does not aggro a robber that has seen a police officer.
- **Rob** runs the game's own robbery on the player and ends the robber's activity. It returns `false` and does nothing unless the player holds at least the robber's minimum in both currencies (1000 OC and 33 RM in this build). The game leaves the player a random amount below the minimum in each currency, so with at least the minimum in both, the robbery only takes money. The game's own robbery dialogue is not changed: it still starts on either currency and can add money to a player who holds less. It returns `true` once the game's call has run.
- **Negotiate** runs the game's own bribe attempt for the robber. It returns `true` when the attempt succeeds. On success the game takes a small share of the player's money and the robber leaves.
- **SetAloof** sets the robber's `Aloof` dialogue field. It returns `false` when the robber has no conversation trigger.

## Events

```csharp
public static class Robbers
{
    public static event Action<int> Robbed;   // NPC id of the robber
}
```

- **Robbed** fires when the game's robbery finishes, whether the game or a mod started it. It is raised from a postfix on the game's robbery call, on the main thread. Handlers run one at a time, so a handler that throws is logged and the others still run. It is not raised when the robber has no NPC reference.

## Not included

- **StateChanged.** Not added. The robber's state is set in several places, and some of them do not call the shared `StartState` method, so one hook would miss changes.
