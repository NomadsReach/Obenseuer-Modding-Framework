---
title: Jobs API
parent: API reference
nav_order: 36
---

# Jobs API

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Patch owner: `obenseuer.moddingframework.jobs`.
Files: `src/Api/Jobs.cs`, `src/Api/JobsPatches.cs`.

Everything not listed here is internal. Items marked **experimental** may change.

## If a hook cannot be installed

The framework installs the `Worked` hook under the owner. If the hook is missing, the owner is removed, `Jobs.IsAvailable` becomes `false`, and one warning is logged to `BepInEx\logs\obenseuer.moddingframework.jobs.log`. The reads keep working; only `Worked` stops.

## Threading and safety

- Main thread only.
- `Jobs.IsAvailable` is false when the hook was not installed or the owner was disabled.
- Every member is safe at any time. With no scene loaded, or on the main menu, reads return an empty list or `null`.
- `Worked` is raised synchronously from the game's own wait completion, so a handler that throws is logged and the other handlers still run.

## Jobs

```csharp
public static class Jobs
{
    public static List<JobInfo> All();
    public static JobInfo Get(string id);            // null when no job has the id

    public static event Action<WorkedArgs> Worked;

    public static bool IsAvailable { get; }
}

public sealed class JobInfo
{
    public string Id { get; }                 // a job's id (GUID when the object has one, else scene path)
    public int SalaryPerHourOc { get; }       // InteractableWaitWork.SalaryPerHour.OC, the game's own money field
    public float SalaryPerHourRm { get; }     // InteractableWaitWork.SalaryPerHour.RM
    public Skill Skill { get; }               // the skill the job tests; Skill.None when the job tests none
    public float Hardness { get; }            // InteractableWaitWork.JobHardness
}

public sealed class WorkedArgs
{
    public string Id { get; }                 // the same id as JobInfo.Id
    public float Hours { get; }               // the wait length in hours
    public float PayOc { get; }               // OC the wait added to the player's balance (0 when none)
    public float PayRm { get; }               // RM the wait added to the player's balance, to 0.1 (0 when none)
}
```

- `All` returns every active `InteractableWaitWork` in the active scene. `Get` returns `null` for an unknown id.
- `Skill` uses the `Skill` enum from `src/Api/Skills.cs`, cast from the game's `PlayerStats.PlayerSkillType` the same way the Skills facade does it.
- `Worked` fires when the game calls `InteractableWaitWork.WaitDone`, after the game has paid the salary and applied the skill and addiction changes. The game calls `WaitDone` from `WaitingController.Wakeup`, so it fires for a wait that is cut short too, with the time actually waited. It does not fire for a wait of zero seconds. `Hours` is the game's `timeWaited` in seconds divided by 3600.
- `PayOc` and `PayRm` are the change in the player's OC and RM balance across `WaitDone`. That is the salary the game pays for the wait. Both are 0 when the job pays nothing. `PayRm` is rounded to 0.1, the game's RM precision.
- `Worked` does not fire on load. Nothing on the load path calls `Wakeup`.

## Not included

- **Start and stop of a job.** Not added. `Interact` and `Wait` are the game's own user flow, and the wait can be refused by the game (`CanWait`), so a write would need the same checks.

## Ceilings and risks

- `All` and `Get` scan `FindObjectsByType<InteractableWaitWork>` on each call. Do not poll them every frame.
- `PayOc` and `PayRm` are a balance difference, not the salary formula. Any money the scene changes through the wait's `onWaitDone` relay during the same `WaitDone` is counted too. The game code does not show such a relay, because it is set per scene.
- `Worked` depends on `InteractableWaitWork.WaitDone` being the only completion path. The game code has one override and one call site, in `WaitingController.Wakeup`.
- `Id` for an object with no GUID is a scene path, so it changes if the hierarchy is edited.

## Save loads

The framework does not gate this event on a save load. A handler that should ignore events raised while a save loads can check `SaveData.Busy`, which is true while a save is loading or being written.
