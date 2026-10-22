---
title: Common tasks
parent: Inventory
grand_parent: Items and crafting
nav_order: 1
---

# Common tasks

Every example on this page is a whole mod. Most of them wait for `Events.WhenReady`, because the player's inventory, the items and the containers in the scene are ready only after a save has loaded. Replace the mod id and the item title with your own. Each example looks its item up with `Items.Find`, so it uses the id the catalogue gives it instead of a number you have to keep in step. Every call that can refuse is checked, and the warning names the page with the limits.

Take items only if the player has all of them. `Take` already refuses when the whole amount is not held, so the `Has` check is only there to let you show your own message first.

```csharp
using ObenseuerCore;

[ModInfo("example.inventorytake", "Take Three Apples", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class TakeThreeApples : FrameworkMod
{
    protected override void OnLoad()
    {
        Events.WhenReady(Pay);
    }

    private void Pay()
    {
        ItemDef apple = Items.Find("Apple");
        if (apple == null)
        {
            Log.Warn("The Apple item was not found, see the limits section of the Item lookup page");
            return;
        }

        if (!PlayerInventory.Has(apple.Id, 3))
        {
            Log.Warn("The player holds fewer than 3 apples, so nothing was taken");
            return;
        }

        if (PlayerInventory.Take(apple.Id, 3)) Log.Info("The player paid 3 apples.");
        else Log.Warn("The take was refused, see the limits section of the Player inventory page");
    }
}
```

`Take` returns `false` unless the player holds the whole amount, and it returns `false` while a trade runs or when a handler calls it too deeply. See [Limits](player-inventory.md#limits) for every reason a call gives `false`.

Look at every stack the player carries, including its quality.

```csharp
using ObenseuerCore;

[ModInfo("example.inventorystacks", "List The Stacks", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class ListTheStacks : FrameworkMod
{
    protected override void OnLoad()
    {
        Events.WhenReady(List);
    }

    private void List()
    {
        foreach (ItemStackInfo stack in PlayerInventory.Items())
            Log.Info(stack.ItemId + " x" + stack.Count + " quality " + stack.Quality);
    }
}
```

`Items()` lists every stack that is not empty, in the main grid, the backpack and the equipment slots. See [Item stacks](item-stacks.md#things-to-know) for what each `ItemStackInfo` holds.

See what is in each hand (`-1` means the hand is empty).

```csharp
using ObenseuerCore;

[ModInfo("example.helditem", "Read The Hands", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class ReadTheHands : FrameworkMod
{
    protected override void OnLoad()
    {
        Events.WhenReady(Read);
    }

    private void Read()
    {
        int right = PlayerInventory.GetHeldItemId(false);
        int left = PlayerInventory.GetHeldItemId(true);
        Log.Info("right hand " + (right == -1 ? "empty" : "item " + right) + ", left hand " + (left == -1 ? "empty" : "item " + left));
    }
}
```

`-1` is an empty hand, not a refusal, so the example reports it rather than logging a warning. See [Player inventory](player-inventory.md#things-to-know) for what a hand holds and what it does not.

Find a container by its save id and list what is in it. Replace `ChestGuid` with the save id of a container in your save; `Containers.All()` gives you the `Guid` of every container in the scene.

```csharp
using ObenseuerCore;

[ModInfo("example.containerlist", "List One Container", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class ListOneContainer : FrameworkMod
{
    // The save id of the container to read. Copy it from ContainerInfo.Guid.
    private const string ChestGuid = "replace-with-a-container-guid";

    protected override void OnLoad()
    {
        Events.WhenReady(List);
    }

    private void List()
    {
        ContainerInfo chest = Containers.FindByGuid(ChestGuid);
        if (chest == null)
        {
            Log.Warn("No container has that save id, see the limits section of the Containers page");
            return;
        }

        Log.Info("Container " + chest.Name + " holds:");
        foreach (ItemStackInfo stack in Containers.GetItems(chest))
            Log.Info(stack.ItemId + " x" + stack.Count + " in slot " + stack.Slot);
    }
}
```

`FindByGuid` returns `null` for an unknown save id, an empty one, and a container that was destroyed. See [Limits](containers.md#limits) for the reasons a container call gives nothing back.

Move one item from a container to the player. `Containers.Take` destroys the items, so give them to the player first.

```csharp
using ObenseuerCore;

[ModInfo("example.containertake", "Move One Apple", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class MoveOneApple : FrameworkMod
{
    private const string ChestGuid = "replace-with-a-container-guid";

    protected override void OnLoad()
    {
        Events.WhenReady(Move);
    }

    private void Move()
    {
        ContainerInfo chest = Containers.FindByGuid(ChestGuid);
        if (chest == null)
        {
            Log.Warn("No container has that save id, see the limits section of the Containers page");
            return;
        }

        ItemDef apple = Items.Find("Apple");
        if (apple == null)
        {
            Log.Warn("The Apple item was not found, see the limits section of the Item lookup page");
            return;
        }

        if (!PlayerInventory.Give(apple.Id, 1))
        {
            Log.Warn("The give was refused, see the limits section of the Player inventory page");
            return;
        }

        if (!Containers.Take(chest, apple.Id, 1))
            Log.Warn("The take from the container was refused, see the limits section of the Containers page");
    }
}
```

`Containers.Take` destroys the items instead of moving them, and it refuses a locked container, one an NPC owns without permission to take, and one that does not hold the whole amount. See [Limits](containers.md#limits) and [Player inventory limits](player-inventory.md#limits).

React when the player opens or closes any container.

```csharp
using ObenseuerCore;

[ModInfo("example.containerwatch", "Watch The Containers", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class WatchTheContainers : FrameworkMod
{
    protected override void OnLoad()
    {
        Containers.Opened += Opened;
        Containers.Closed += Closed;
    }

    private void Opened(ContainerInfo container)
    {
        Log.Info("The player opened " + container.Name + " (" + container.Guid + ").");
    }

    private void Closed(ContainerInfo container)
    {
        Log.Info("The player closed " + container.Name + " (" + container.Guid + ").");
    }
}
```

The two events are static, so this example subscribes in `OnLoad` and catches every container the player opens from the start. See [Containers](containers.md#things-to-know) for what the events cover and what they do not.
