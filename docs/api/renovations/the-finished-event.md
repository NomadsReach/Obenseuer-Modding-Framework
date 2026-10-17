---
title: The Finished event
parent: Renovations
grand_parent: Places and travel
nav_order: 5
---

# The Finished event

`Finished` fires when a renovation is finished and installed.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Renovations</code> <span>a renovation is finished</span></p>

Subscribe to `Renovations.Finished` to react when a renovation is installed. The `RenovationInfo` it gives you has `State` set to `Installed`.

<div class="api-pair" markdown="1">

```csharp
public static class Renovations
{
    public static event Action<RenovationInfo> Finished;
}
```

| Event | Gives you | When it fires |
|---|---|---|
| `Finished` | `RenovationInfo` | A renovation has just been finished. |

</div>
</div>

## Things to know

- `Finished` fires when the game puts a renovation into the building after the player's final payment. The `RenovationInfo` you get has the state `Installed`. <span class="tech">(It fires after `TenementUpgradeProgress.FinishUpgrade` has run.)</span>

## Limits

- `Finished` does not fire for renovations the game puts in silently. The game does this when it sets up a scene, and such a renovation has no payment, task, or achievement. <span class="tech">(It does not run for `TenementUpgradeProgress.FinishUpgrade(true)`, which scene set-up uses through `TenementUpgradeStart`.)</span>
