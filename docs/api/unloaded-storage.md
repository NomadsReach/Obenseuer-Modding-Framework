---
title: Unloaded storage
parent: Inventory
grand_parent: Items and crafting
nav_order: 6
---

# Unloaded storage

Status: experimental. Not yet tested in the game.
Target: Obenseuer 0.5.08 (buildid 25969064), BepInEx 6 Unity.Mono or BepInEx 5.
Namespace: `ObenseuerCore`. Log owner: `obenseuer.moddingframework.unloadedstorage`.

## What it is

A storage in a scene the player is not in, such as the chest in another building. `UnloadedStorage` reads what is inside it from the save, and lets your mod queue a take or a give. The change is applied when that scene next loads, in the same save. Everything the framework writes goes into its own file in the save folder. The game's own files are only read, never written.

## When to use it

- Check what a chest in another building holds before the player goes there.
- Remove the items a recipe uses from a storage the player is not standing in.
- Put items into a storage that the player will find when they open it.

## Quick example

This mod checks a storage in another scene, and queues the take of three items of one kind. The change happens when the player goes into that scene.

```csharp
using ObenseuerCore;

[ModInfo("example.craftstorage", "Craft From Storage Example", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.12.0")]
public sealed class CraftFromStorageExample : FrameworkMod
{
    private const string Scene = "Interior Bazaar Storage";
    private const string Box = "00000000-0000-0000-0000-000000000000";

    protected override void OnLoad()
    {
        UnloadedStorage.Applied += info => Log.Info("Applied " + info.Count + " of item " + info.ItemId + " in " + info.Scene);
        UnloadedStorage.Failed += info => Log.Warn("Dropped a queued change in " + info.Scene);
    }

    public bool TakeThree(int itemId)
    {
        UnloadedContainer box = UnloadedStorage.Read(Scene, Box);
        if (box == null || box.Amount(itemId) < 3) return false;
        return UnloadedStorage.QueueTake("example.craftstorage", Scene, Box, itemId, 3);
    }
}
```

`Box` is the save id of a container in that scene. Get it once while the scene is loaded, from `ContainerInfo.Guid` in `Containers.All()`, and keep it. `ListGuids(scene)` also lists the ids in a scene file.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>UnloadedStorage</code> <span>read, queue and watch storage in other scenes</span></p>

This is the class you start from. `Read` and `ListGuids` look at a scene file of the current save, `QueueTake` and `QueueGive` add a change for that scene, `Pending` lists your changes that are not yet applied, and `Applied` and `Failed` tell you what happened when the scene loaded.

<div class="api-pair" markdown="1">

```csharp
public static class UnloadedStorage
{
    public static UnloadedContainer Read(string scene, string guid);
    public static List<string> ListGuids(string scene);
    public static bool QueueTake(string modId, string scene, string guid, int itemId, int count);
    public static bool QueueGive(string modId, string scene, string guid, int itemId, int count);
    public static List<UnloadedStorageInfo> Pending(string modId);

    public static event Action<UnloadedStorageInfo> Applied;
    public static event Action<UnloadedStorageInfo> Failed;
}
```

| Method | Returns | What it does |
|---|---|---|
| `Read(scene, guid)` | `UnloadedContainer` | The stacks of one storage in a scene the player is not in. It is `null` when it cannot be read, with one log line. |
| `ListGuids(scene)` | `List<string>` | The save ids of the storages in a scene file. An empty list when it cannot be read. |
| `QueueTake(modId, scene, guid, itemId, count)` | `bool` | Queues taking `count` items out when the scene loads. It is `false` when refused. |
| `QueueGive(modId, scene, guid, itemId, count)` | `bool` | Queues putting `count` items in when the scene loads. It is `false` when refused. |
| `Pending(modId)` | `List<UnloadedStorageInfo>` | The changes of this mod that have not been applied yet. |

| Event | Gives you | When it fires |
|---|---|---|
| `Applied` | `UnloadedStorageInfo` | A queued change was made in the scene. |
| `Failed` | `UnloadedStorageInfo` | A queued change was dropped, because its container was not found for 10 seconds after the scene loaded, or the container refused the change, or its guid matched more than one container in the scene. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>UnloadedContainer</code> <span>what one storage held when the save was written</span></p>

`Read` gives you this. It is a copy of the scene file, not the live storage, so it shows the contents at the last save of that scene. <span class="tech">(It is read from the game's `<Scene>.tnmt` file, the `storageSlots` of the storage with that id.)</span>

<div class="api-pair" markdown="1">

```csharp
public sealed class UnloadedContainer
{
    public string Guid { get; }
    public List<UnloadedStack> Stacks { get; }
    public int Amount(int itemId);
}

public sealed class UnloadedStack
{
    public int Slot { get; }
    public int ItemId { get; }
    public int Amount { get; }
    public int OwnerId { get; }
}
```

| Member | Type | What it means |
|---|---|---|
| `Guid` | `string` | The save id you asked for. |
| `Stacks` | `List<UnloadedStack>` | The stacks in the storage, without the empty slots. |
| `Amount(itemId)` | `int` | The total of that item over all the stacks. |
| `Slot` | `int` | The slot the stack is in. |
| `ItemId` | `int` | The item id. |
| `Amount` | `int` | How many items the stack holds. |
| `OwnerId` | `int` | The NPC who owns the stack, or `-1`. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>UnloadedStorageInfo</code> <span>one queued change</span></p>

`Pending`, `Applied` and `Failed` give you this. It is a copy, so changing it does nothing.

<div class="api-pair" markdown="1">

```csharp
public sealed class UnloadedStorageInfo
{
    public string ModId { get; }
    public string Scene { get; }
    public string Guid { get; }
    public int ItemId { get; }
    public int Count { get; }
    public bool Give { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `ModId` | `string` | The mod that queued the change. |
| `Scene` | `string` | The scene the change waits for. |
| `Guid` | `string` | The save id of the storage. |
| `ItemId` | `int` | The item the change moves. |
| `Count` | `int` | How many items. |
| `Give` | `bool` | `true` for a give, `false` for a take. |

</div>
</div>

## Things to know

- A queued change is applied when its scene loads, a few frames after the load, once the save has finished loading. The change is all or nothing. A take needs the whole amount in that container, and a give needs room for the whole amount.
- A change whose container is not in the scene when it loads is retried about once a second for 10 seconds. If the container shows up in that time, the change is applied. Otherwise it is dropped, with a log line and a `Failed` event. Nothing is invented. A take from a locked container, or from an NPC's container the player may not take from, is refused by the game and is dropped at once, the same way. Every drop logs a line.
- Queued changes are kept in the framework's file, `mods\obenseuer.moddingframework.unloadedstorage.json`, in the save folder. A save slot copy or an autosave carries them with the rest of the save.
- A change that was applied is kept in that file until the game next saves that scene. This is what makes a quit without saving replay it, because the game's own copy of the scene is still the old one. Once the game saves the scene, the change is removed.
- Each `Read` reads the whole scene file again. For many containers, call `ListGuids` once and then `Read` only the ones you need.
- `Read` and the queue calls work only for a scene the player is not in. For the scene that is loaded, use `Containers`, which works on the live storage.
- Queued changes are checked against the file when you queue them: the container must be in that scene file, or the queue call is refused. The amounts are checked again when the scene loads.
- `QueueTake` and `QueueGive` refuse item ids `0` and `1`, which are money.

## Limits

- A queue call is refused while a save loads or saves, when no save is loaded, when the scene is loaded, when the scene name is not a file name, when the mod keeps 256 changes already, when the framework queue file failed to load, so nothing is queued until the next load, and when the container is not in the scene file. Each refusal returns `false`. A refusal logs one line per distinct reason.
- A crash or kill between the game writing the scene file and the framework writing its queue file, or a save that throws after the scene was written, can apply a change twice.
- The queue file is written at queue time to the current save folder. If the player autosaves to another slot and then quits without saving, the queued take is replayed in that slot without the craft product, which was saved in the other slot.
- A container that respawns (`removeOldItems`) can overwrite a give that was applied before the container's load delay has finished.
- A container guid that appears twice in the same scene is refused, because the framework cannot tell which of the two containers the mod read. The change is dropped at once, without the 10 second retry, with a warning that names the guid.
- A container that is inactive when the scene loads is still changed, but its own spawn logic can overwrite the change.
- `Storage.global` storages are saved in the game's `Globals.tnmt`, not in a scene file, so they are refused. <span class="tech">(The game writes each storage to `Globals.tnmt` when `Storage.global` is set, and to `<Scene>.tnmt` otherwise.)</span>
- If the player saves under another slot name (Save As) after a change was applied, the copy keeps the change pending in the old slot. The file of the new slot is written with the same rules. <span class="tech">(The framework writes its file into the slot named by the character and slot, as the other per-save APIs do.)</span>
- Calls must be made on the Unity main thread.
- `modId` must be 1 to 64 characters, and `count` must be 1 to 100000. A call outside those ranges is refused.
- Each `Read`, `ListGuids` and queue call parses the whole scene file on the main thread, about half a second for a large 5 MB scene. Do not loop over many ids in one frame; read once and keep the result.
- The 10 second retry window uses real time, so a long pause (alt-tab) can use it up.
- There is no way to cancel a queued change. An entry for a scene or mod that no longer exists stays, and counts against the 256 per mod cap.

## Checks

- Set `Debug / UnloadedStorageSelfCheck` to `true` in `BepInEx\config\obenseuer.moddingframework.cfg` and read the log line `unloaded storage self-check ok`. It checks the queue merge, the decision for an applied or dropped change, and the reading of a scene file, on sample text. It does not touch the game.
- In game, with a throwaway save: queue a take from a storage in another scene, load that scene and check that the count went down and that `Applied` fired. Then quit without saving and load again: the change must happen once more, not twice.
