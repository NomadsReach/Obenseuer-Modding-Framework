---
title: The Finished event
parent: Renovations
grand_parent: Places and travel
nav_order: 5
---

# The Finished event

```csharp
public static class Renovations
{
    public static event Action<RenovationInfo> Finished;   // a renovation has just been finished
}
```

- **`Finished` fires when the game puts a renovation into the building after the player's final payment.** The `RenovationInfo` you get has the state `Installed`.
  - Technical: it fires after `TenementUpgradeProgress.FinishUpgrade` has run.
- **`Finished` does not fire for renovations the game puts in silently.** The game does this when it sets up a scene, and such a renovation has no payment, task, or achievement.
  - Technical: it does not run for `TenementUpgradeProgress.FinishUpgrade(true)`, which scene set-up uses through `TenementUpgradeStart`.
