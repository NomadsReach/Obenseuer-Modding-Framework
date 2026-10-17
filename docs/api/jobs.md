---
title: Jobs
parent: Player life and skills
grand_parent: API reference
nav_order: 4
---

# Jobs

## What it is

A job in the game is a place where the player can wait out a stretch of time and get paid for it. Each job has an hourly wage, may test one of the player's skills, and has a hardness. This API lets your mod list the jobs in the scene you are in, read their wage, skill and hardness, and get told each time the player finishes working, with how long they worked and how much money it paid.

## When to use it

- Show a message or write a log line when the player finishes a shift.
- Pay a bonus on top of a job's wage.
- Show the wages of the jobs in a scene.
- Count how many hours the player has worked.

## Quick example

This mod writes a log line each time the player finishes working, with the hours worked and the pay. When the game is ready, it also logs every job in the scene with its hourly wage.

```csharp
using ObenseuerCore;

[ModInfo("example.jobwatch", "Job Watch", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class JobWatch : FrameworkMod
{
    protected override void OnLoad()
    {
        Jobs.Worked += args => Log.Info("worked " + args.Hours + " hours and earned " + args.PayOc + " OC and " + args.PayRm + " RM");
        Events.WhenReady(() =>
        {
            foreach (JobInfo job in Jobs.All())
                Log.Info(job.Id + " pays " + job.SalaryPerHourOc + " OC an hour");
        });
    }
}
```

`Worked` is an event: the framework calls your code each time it happens. OC is Open Sewer Coins and RM is Real Money (see the [glossary](../glossary.md#oc)). See [Using events](../guides/getting-started/using-events.md) for more on events and `WhenReady`.

## Common tasks

Look up one job by its id. `Get` returns null when no job has that id.

```csharp
JobInfo job = Jobs.Get("some-job-id");
if (job != null) Log.Info(job.SalaryPerHourOc + " OC an hour, hardness " + job.Hardness + ", skill " + job.Skill);
```

Find the best paying job in the scene.

```csharp
JobInfo best = null;
foreach (JobInfo job in Jobs.All())
    if (best == null || job.SalaryPerHourOc > best.SalaryPerHourOc) best = job;
if (best != null) Log.Info("best job: " + best.Id);
```

Pay a bonus of 10 OC every time the player finishes work that paid something.

```csharp
Jobs.Worked += args => { if (args.PayOc > 0f) Cash.Give(MoneyCurrency.OC, 10f); };
```

## Reference

This is the full list of what the API gives you. In every code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

```csharp
public static class Jobs
{
    public static List<JobInfo> All();          // every job in the scene you are in
    public static JobInfo Get(string id);       // the job with this id; null when no job has it

    public static event Action<WorkedArgs> Worked;   // the player finished working

    public static bool IsAvailable { get; }          // false when the Worked event could not be set up
}

public sealed class JobInfo    // a snapshot of one job
{
    public string Id { get; }                 // names this job; see the first bullet below
    public int SalaryPerHourOc { get; }       // the wage in OC for each hour of work
    public float SalaryPerHourRm { get; }     // the wage in RM for each hour of work
    public Skill Skill { get; }               // the player skill the job tests; Skill.None when it tests none
    public float Hardness { get; }            // how hard the job is, as the game sets it
}

public sealed class WorkedArgs    // what you get when the player finishes working
{
    public string Id { get; }                 // the id of the job, as JobInfo.Id
    public float Hours { get; }               // how many hours the player worked
    public float PayOc { get; }               // the OC the work added to the player's balance; 0 when none
    public float PayRm { get; }               // the RM the work added to the player's balance, to 0.1; 0 when none
}
```

- **A job's `Id` names one job in the scene you are in.** If the game gave the job a save id, that is the id. If not, the id is the job's place in the scene, and it changes if someone edits the objects around it.
  - Technical: the id is the object's GUID when it has one, otherwise a scene path such as `Scene:Root[0]/Child[2]`.
- **`All()` lists the jobs in the scene you are in, and it is slow, so do not call it every frame.** `Get` is just as slow, because both search every object in the scene each time. Call them when something happens, or on a timer, and keep the result.
  - Technical: both scan `FindObjectsByType<InteractableWaitWork>` on each call. Objects that are switched off are left out.
- **`Skill` is the player skill the job tests, or `Skill.None` when it tests none.** It is the same `Skill` list the framework uses everywhere else for the player's skills.
  - Technical: it is cast from the game's `PlayerStats.PlayerSkillType`, the same way the skills API does it.
- **`Worked` fires when the player finishes a stretch of work, after the game has already paid the wage.** It fires after the game has applied the skill and addiction changes too. It also fires for a wait that was cut short, with the time actually worked. It does not fire for a wait of zero seconds.
  - Technical: it fires after the game calls `InteractableWaitWork.WaitDone`, which the game calls from `WaitingController.Wakeup`. `Hours` is the game's `timeWaited` in seconds divided by 3600.
- **`PayOc` and `PayRm` are how much the player's balance went up during the work, not a recalculated wage.** Both are 0 when the job pays nothing. `PayRm` is rounded to 0.1, because that is how finely the game counts RM. If a scene gives the player other money in the same moment, that money is counted too.
  - Technical: they are the change in the player's OC and RM balance across `WaitDone`. The game code does not show such a scene relay (the `onWaitDone` output), because it is set per scene.
- **`Worked` does not fire when a save loads.** Loading a save does not finish any work.
  - Technical: nothing on the load path calls `WaitingController.Wakeup`.

## Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\obenseuer.moddingframework.jobs.log`.

Everything not listed in this reference is internal and may change without notice. Items marked experimental may change too.

The framework changes one of the game's methods when it starts, so it can tell your mod when work is finished. This page's feature group is `obenseuer.moddingframework.jobs`, the name you will see in the log.

### When you can call it

- **Call everything from your mod's normal code, not from a background thread you started.** The game and Unity only work safely on one thread, the main thread (see the [glossary](../glossary.md#main-thread)). This goes for every member on this page and for the code that handles `Worked`.
- **It is safe to call any of it at any time, even on the main menu or while a save is loading.** If there is nothing to read yet, you get an empty list or `null`.
- **Your `Worked` handlers run inside the game's own call, one at a time.** If one of your handlers throws an error, the framework logs it and still runs the other handlers.
  - Technical: the event is raised synchronously from the game's wait completion.

### If something fails to set up

The framework changes some of the game's methods when it starts. If a game update breaks one of them, the framework removes all the changes of the [feature group](../glossary.md#feature-group) that depends on it, turns the group off, and writes error lines and a warning to the group's log file in `BepInEx\logs`. The rest of the framework keeps working.

- **If the change behind `Worked` cannot be made, `Jobs.IsAvailable` becomes `false` and `Worked` never fires.** The feature group is turned off and error lines and a warning go to `BepInEx\logs\obenseuer.moddingframework.jobs.log`. `All` and `Get` keep working.
  - Technical: the change is on `InteractableWaitWork.WaitDone`. `Worked` relies on `WaitDone` being the only way a wait finishes. The game code has one override of it and one call, in `WaitingController.Wakeup`.

### Limits

- **`PayOc` and `PayRm` can include money that did not come from the wage.** They are the difference between two balances, not the game's wage formula, so any other money change that happens during the same work is counted too.
- **A job without a save id gets a new id if its object is renamed or moved.** Do not store those ids between sessions.

### What you can't do (yet)

- **You can't start or stop a job from your mod.** The game lets the player start work through its own steps, and it can refuse to let the player wait. A call from a mod would have to repeat those checks, so it is not offered.
  - Technical: `Interact` and `Wait` are the game's own flow, and the wait can be refused (`CanWait`).

### While a save is loading

The framework does not hold back `Worked` while a save is loading. If your handler should ignore events raised during a load, check `SaveData.Busy` first. It is `true` while a save is loading or being written.
