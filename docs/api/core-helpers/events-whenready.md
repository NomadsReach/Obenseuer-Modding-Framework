---
title: Events.WhenReady
parent: Framework helpers
grand_parent: Framework basics
nav_order: 4
---

# Events.WhenReady

`WhenReady` runs your code once a save is ready to use. A save is ready when the player has loaded one, or has started a new game and reached the first level after the intro.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Events</code> <span>run code when a save is ready</span></p>
<div class="api-pair" markdown="1">

```csharp
public static class Events
{
    public static void WhenReady(Action action);
}
```

| Method | Returns | What it does |
|---|---|---|
| `WhenReady(Action action)` | `void` | Runs your code when a save is ready, or now if it already is. |

</div>
</div>

### Things to know

- If a save is ready now, your code runs straight away. Otherwise it runs once, the next time one is ready. It is then removed, so it does not run again. <span class="tech">(It counts when `SaveData.Folder` is not `null` and no load is running. `Folder` is set when a load completes.)</span>
- A saved game counts as loaded once its load has finished. A load that fails does not count. A save that is still being written does not matter.
- A new game counts once its first level after the intro has loaded. It stops counting when a load starts, and counts again if that load fails.
- If a save or a new game was ready before that load started, the code you queued while it ran runs once the failure is found. When a load from the menu or a level change finds the failure, your code runs when that load completes, or when it fails and the failure is found.
- A failing action does not stop the others. The error is logged once for that action, and later failures of the same action are not logged.
- Only the first ready point is used. A later load does not run the same action again.
- `WhenReady` also covers a save that was loaded before you asked. Using `Events.Loaded += OnLoaded` instead would miss a save that was loaded before your handler was added.

### Limits

- A `null` action throws an `ArgumentNullException`.

### Example

Adding a handler to `Events.Loaded` misses a save that was loaded before the handler was added. The second line runs once a save is ready, whenever it was loaded.

```csharp
Events.Loaded += OnLoaded;
```

```csharp
Events.WhenReady(() => Logger.LogInfo("save ready"));
```
