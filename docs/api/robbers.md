---
title: Robbers
parent: People
grand_parent: API reference
nav_order: 13
---

# Robbers

Status: experimental.

## What it is

Some NPCs in the game are robbers. A robber hangs around, follows the player and may demand money. This API lists the robbers, reads what each one is doing, lets your mod make a robber attack, rob the player or accept a bribe, and tells you when the player has been robbed. Robbers are NPCs, so you name one with the NPC id from the [NPCs](npcs.md) page (`NpcInfo.Id`).

## When to use it

- Log or react when the player is robbed, for example to give a reward or a penalty.
- Check which NPCs are robbers and whether one is chasing or attacking the player.
- Start a robbery, an attack or a bribe from your own quest or event.

## Quick example

This mod logs the name of the robber each time the player is robbed. It also logs how many robbers the game has once a save is loaded.

```csharp
using ObenseuerCore;

[ModInfo("example.robberwatch", "Robber Watch", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class RobberWatch : FrameworkMod
{
    protected override void OnLoad()
    {
        Robbers.Robbed += npcId =>
        {
            NpcInfo npc = Npcs.Get(npcId);
            Log.Info("The player was robbed by " + (npc == null ? "NPC " + npcId : npc.DisplayName ?? npc.Name));
        };
        Events.WhenReady(() => Log.Info("The game has " + Robbers.All().Count + " robbers."));
    }
}
```

`Robbed` is an event: the framework calls your code each time it happens. `WhenReady` runs your code once the game is ready. See [Using events](../guides/getting-started/using-events.md) for more on both.

## Common tasks

List every robber and what each one is doing.

```csharp
foreach (RobberInfo robber in Robbers.All())
    Log.Info("NPC " + robber.NpcId + " is in the state " + robber.State);
```

Make a robber attack the player. The first argument is your mod's id. `Aggro` returns `true` when the robber is now attacking.

```csharp
int npcId = 123;
bool attacking = Robbers.Aggro(Mod.Id, npcId);
Log.Info("Robber is attacking: " + attacking);
```

Try to bribe a robber so it leaves.

```csharp
int npcId = 123;
if (Robbers.Negotiate(Mod.Id, npcId)) Log.Info("The robber took the bribe and left.");
```

## Reference

This is the full list of what the API gives you. In every code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

### Reading robbers

`Robbers.All()` and `Robbers.State()` tell you which NPCs are robbers and what they are doing. They also see a robber that is far away and not in the scene you are in.

```csharp
public sealed class RobberInfo    // one robber
{
    public int NpcId { get; }       // the robber's NPC id; use it with the other members
    public string State { get; }    // what the robber is doing: Idle, Stand, Patrol, Wander, Chase or Aggro
}

public static class Robbers
{
    public static bool IsAvailable { get; }          // false when the Robbed event could not be set up
    public static List<RobberInfo> All();            // every robber the game knows
    public static string State(int npcId);           // what that robber is doing; null when the NPC is not a robber
}
```

- **A robber is an NPC whose current activity is the game's robber activity.** When a robber is doing something else, such as another job, it is not listed.
  - Technical: `All` looks at each NPC's `state.CurrentActivity` in `NPCDirector.globalDatabase` and keeps the ones that are an `Act_Robber`.
- **The states mean what the robber is doing now.** `Idle` and `Stand` are waiting, `Patrol` is walking its route, `Wander` is walking around, `Chase` is following the player, and `Aggro` is attacking the player.
- **`IsAvailable` is `false` when the `Robbed` event does not work.** The reads and the other writes keep working.

### Making a robber act

These calls make a robber do something. The first argument is your mod's id (`Mod.Id`). They only work on a robber that is in the scene you are in. For a robber that is far away, they return `false`.

```csharp
public static class Robbers
{
    public static bool Aggro(string modId, int npcId);                  // make the robber attack the player; true when it is now attacking
    public static bool Rob(string modId, int npcId);                    // rob the player now; true when the robbery ran
    public static bool Negotiate(string modId, int npcId);              // try to bribe the robber; true when it worked
    public static bool SetAloof(string modId, int npcId, bool aloof);   // change the robber's "aloof" dialogue flag; true when it was set
}
```

- **`Aggro` makes the robber attack the player, the same as the game's own attack call.** It returns `true` only when the robber is now attacking. If the robber has seen a police officer, the game does not let it attack, and you get `false`.
  - Technical: it calls `Act_Robber.Aggro()` and checks `state == Aggro`. The game does nothing when `seenPolice` is set.
- **`Rob` robs the player the way the game does, then ends the robber's job.** It only runs when the player holds at least the robber's minimum in both currencies, which is 1000 OC and 33 RM in this game version. Otherwise it returns `false` and nothing happens. The game leaves the player a random amount below that minimum in each currency. It returns `true` once the robbery has run.
  - Technical: the check is `Money.CheckMoney(minimumMoney)` on the private `Act_Robber.minimumMoney` field, so a player holding at least the minimum in both currencies is only ever robbed, never given money. The game's own robbery talk still starts on either currency alone and can add money for a player who holds less. The framework does not change that. `RobPlayer` also sets the robber's aloof flag and finishes its activity.
- **`Negotiate` tries the bribe the way the game does.** The chance depends on the player's bribery skill and on how much money the player holds: the more money, the less likely it works. It returns `true` when it works. Then the game takes a small share of the player's money and the robber leaves.
  - Technical: it calls `Act_Robber.TryNegotiate()`. On success the game removes 5% of the player's money, finishes the activity and sets the aloof flag.
- **`SetAloof` changes a flag that the robber's dialogue reads.** The game sets this flag itself after a robbery or a bribe. It returns `false` when the robber has no conversation to set it on.
  - Technical: it calls `ChangeRobberAloofStatus`, which sets the actor field `Aloof` for the NPC's `ConversationTrigger`.
- **Your mod's id must be valid.** It must be 1 to 64 letters, digits, dots, underscores or dashes. A bad id throws an `ArgumentException`. Use `Mod.Id` and you are fine.
- **These calls return `false` while a save is loading or being written.**

### Knowing when the player is robbed

`Robbed` tells you each time a robbery finishes.

```csharp
public static class Robbers
{
    public static event Action<int> Robbed;   // a robbery just finished; you get the robber's NPC id
}
```

- **`Robbed` fires when a robbery finishes, whether the game or a mod started it.** Your handler gets the robber's NPC id. It does not fire when the robber has no NPC id.
  - Technical: it is raised from a Harmony postfix on `Act_Robber.RobPlayer`, on the main thread.
- **A `Robbed` handler that throws an error does not stop the others.** The framework logs the error and still runs the other handlers.

### What you can't do (yet)

- **There is no event for a robber changing state.** The game changes a robber's state in several places, and some of them do not go through the shared method the framework could watch, so one watch would miss changes. To follow a robber, ask `State` on a timer.
  - Technical: some state changes skip `StartState`, so a hook on it would miss them.

## Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\obenseuer.moddingframework.robbers.log`.

Everything not listed in this reference is internal and may change without notice. Items marked experimental may change too.

The framework changes one game method when it starts, so it can tell your mod when a robbery finishes. This page calls that a feature group: its name is `obenseuer.moddingframework.robbers`, the name you will see in the log.

### When you can call it

- **Call everything from your mod's normal code (`OnLoad`, event handlers, `Update`), not from a background thread you started.** The game and Unity only work safely on one thread, the main thread (see the [glossary](../glossary.md#main-thread)). This goes for the code that handles `Robbed` too.
- **It is safe to call any of it at any time, even on the main menu or while a save is loading.** Reads give an empty list or `null` when there is nothing to read. Calls that make a robber act give `false`.

### What to check for and what comes back

- **Bad values give `false` or `null` instead of an error.** The only error is the `ArgumentException` for an invalid mod id. If the game itself fails, the first failure of each member goes to the log as an error and you get the same plain default.

### If something fails to set up

The framework changes some of the game's methods when it starts. If a game update breaks one of them, the framework removes all the changes of the [feature group](../glossary.md#feature-group) that depends on it, turns the group off, and writes error lines to the group's log file in `BepInEx\logs`. The rest of the framework keeps working.

- **If the framework cannot change the game's robbery method, `Robbed` stops and the rest keeps working.** `IsAvailable` becomes `false`.
  - Technical: the method is `Act_Robber.RobPlayer`.
