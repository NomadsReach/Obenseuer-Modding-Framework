# Jobs API

Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 5.4.23.5.
Namespace: `ObenseuerCore`. Patch owner: `obenseuer.moddingframework.jobs`.
Files: `src/Api/Jobs.cs`, `src/Api/JobsPatches.cs`.

Everything not listed here is internal. Items marked **experimental** are not checked in game.

## Wiring

```csharp
internal static void Jobs.Init();
```

Core calls this from `Core.Awake`.

`Init` installs the `Worked` hook under the owner. If the hook is missing, the owner is removed, `Jobs.IsAvailable` becomes `false`, and one warning is logged to `BepInEx\logs\obenseuer.moddingframework.jobs.log`. The reads keep working; only `Worked` stops.

## Threading and safety

- Main thread only.
- `Jobs.IsAvailable` is false when the hook was not installed or the owner was disabled.
- Every member is safe at any time. With no scene loaded, or on the main menu, reads return an empty list or `null`.
- `Worked` is raised synchronously from the game's own wait completion, through `Events.Raise`, so a handler that throws is logged and the other handlers still run.

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
    public string Id { get; }                 // a job's SceneIds.Of id (GUID when the object has one, else scene path)
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

- `All` returns every active `InteractableWaitWork` in the loaded scenes. `Get` returns `null` for an unknown id.
- `Skill` uses the `Skill` enum from `src/Api/Skills.cs`, cast from the game's `PlayerStats.PlayerSkillType` the same way the Skills facade does it (`src/Api/Skills.cs:142`).
- `Worked` fires when the game calls `InteractableWaitWork.WaitDone`, after the game has paid the salary and applied the skill and addiction changes. The game calls `WaitDone` from `WaitingController.Wakeup` (`UI/WaitingController.cs:385`), so it fires for a wait that is cut short too, with the time actually waited. It does not fire for a wait of zero seconds. `Hours` is the game's `timeWaited` in seconds divided by 3600.
- `PayOc` and `PayRm` are the change in the player's OC and RM balance across `WaitDone`. That is the salary the game pays for the wait. Both are 0 when the job pays nothing. `PayRm` is rounded to 0.1, the game's RM precision.
- `Worked` does not fire on load. Nothing on the load path calls `Wakeup`.

## Not included

- **Start and stop of a job.** Not added. `Interact` and `Wait` are the game's own user flow, and the wait can be refused by the game (`CanWait`), so a write would need the same checks.

## Ceilings and risks

- `All` and `Get` scan `FindObjectsByType<InteractableWaitWork>` on each call. Do not poll them every frame.
- `PayOc` and `PayRm` are a balance difference, not the salary formula. Any money the scene changes through the wait's `onWaitDone` relay during the same `WaitDone` is counted too. The decompile does not show such a relay, because it is set per scene. Not verified in game.
- `Worked` depends on `InteractableWaitWork.WaitDone` being the only completion path. The decompile shows one override and one call site, in `WaitingController.Wakeup`. Not verified in game.
- `Id` for an object with no GUID is a scene path, so it changes if the hierarchy is edited.
- Not verified in game at all.

## Smoke test

1. Load a save near a work job, for example a wait-work station. `Jobs.All()` lists it. `Jobs.Get(id)` returns a `JobInfo` with the salary, skill and hardness the station shows.
2. Subscribe with `Jobs.Worked += a => Log(a.Id + " " + a.Hours + " " + a.PayOc + " " + a.PayRm);`. Work the station for its full length. `Worked` fires once with that id, about the wait length in hours, and the pay. Your OC and RM rise by `PayOc` and `PayRm`, and `Cash.Balance` before and after the wait differs by the same amounts.
3. Start a work wait and wake up early through the wait UI. `Worked` fires with the shorter time.
4. `Jobs.Get("no-such-id")` is `null`.
