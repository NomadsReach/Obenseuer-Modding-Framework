---
title: Expiry default
parent: World item events
grand_parent: Items and crafting
nav_order: 4
---

# Expiry default

New items in the world have a timer after which they go away. This part lets your mod change that timer for items the game creates without a time of its own.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>WorldItems</code> <span>the expiry timer for new items</span></p>
Use `SetExpiryOverride` to change the timer for new items, `ExpiryMinutes` to read it, and `ClearExpiryOverride` to take your change back off.

<div class="api-pair" markdown="1">

```csharp
public static class WorldItems
{
    public static int ExpiryMinutes { get; }
    public static bool SetExpiryOverride(int minutes);
    public static bool SetExpiryOverride(string owner, int minutes);
    public static void ClearExpiryOverride();
    public static bool ClearExpiryOverride(string owner);
}
```

| Method | Returns | What it does |
|---|---|---|
| `SetExpiryOverride(minutes)` | `bool` | Sets the timer for new items, in the shared slot that older mods use. `true` when it was set. |
| `SetExpiryOverride(owner, minutes)` | `bool` | The same, for your own mod id. |
| `ClearExpiryOverride()` | `void` | Puts back the game's timer (the shared slot). |
| `ClearExpiryOverride(owner)` | `bool` | Removes your mod's timer. `true` when it had one. |

| Property | Type | What it means |
|---|---|---|
| `ExpiryMinutes` | `int` | The level's normal item timer, in minutes, without any change from a mod. `-1` when it cannot be read. |

</div>
</div>

## Things to know

- `ExpiryMinutes` is the level's normal item timer in minutes. It is `-1` when the level has no logic object or the read fails, and a failure goes to the log once. <span class="tech">(It is `info_game_logic.itemExpirationTimeMinutes`.)</span>
- `SetExpiryOverride(minutes)` sets the timer for new items that the game starts without a time of their own. Each item gets a random time between half of `minutes` and one and a half times `minutes`, so `minutes` is an average, not an exact time.
- A value below `1` means those items never expire.
- Pass your mod id as the `owner` argument so mods do not overwrite each other. Each mod id keeps its own timer, and the newest `SetExpiryOverride` wins.
- `ClearExpiryOverride(owner)` removes only your timer, and the timer of the mod that set one before you applies again. It returns `false` when you had none.
- The versions without `owner` share one slot for older mods. An `owner` that is not a valid mod id throws an `ArgumentException`.
- `ClearExpiryOverride()` puts back the game's own timer. It works at any time, including inside `Events.SceneLoaded`.
- Set the timer again in your `Events.SceneLoaded` handler, which runs after the clear, or in `Events.WhenReady`, which runs after the load.
- It applies to new items the game creates without a time. That means items dropped or spawned by the game's own code (including `Spawn.Item` and coins), perished items that leave a replacement, and spawner items when the spawner is set to let items expire. Those spawns also run while a save loads, so the timer applies to them too. <span class="tech">(The calls are `ItemOperations.SetCollectibleItemValues` and the default-time calls to `Collectible.StartExpirationTimer`. The spawner setting is `expireItems`.)</span>
- If something goes wrong while the framework applies the timer, the game's own timer is used. The failure goes to the log once and never reaches the game.

## Limits

- A value above 21,474,836 is refused: it returns `false`, writes one line to the log and keeps the current timer, because the game's timer would overflow. It also returns `false` when the framework could not set up this feature.
- The timer is cleared whenever a save starts loading and on every new level. A call made while a save is loading can be wiped by these clears. <span class="tech">(It is cleared on `SaveController.PlayerWillLoadGame` and on each `LoadSceneMode.Single` scene load (a level change or the main menu).)</span>
- It does not apply to items that already have a time, or to items the game makes with no timer. Items in an expiration zone have their own time. Items from crimes and animal drops have no timer. A timer that is already running is not restarted. <span class="tech">(Items with `noExpirationTimer` are skipped.)</span>
- The timer is kept in memory only. It is lost when the game restarts.
