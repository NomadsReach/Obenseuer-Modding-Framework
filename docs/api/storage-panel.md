---
title: Storage panel
parent: Items and crafting
grand_parent: API reference
nav_order: 16
---

# Storage panel

Status: experimental.

## What it is

When the player opens a container, the game shows its storage panel with buttons such as Sort and Take all. This API lets your mod add its own button to that panel. When the player presses it, the framework finds the first empty slot of the container and calls your function with the container and that slot. When the container has no empty slot, the framework shows the game's full-storage warning and does not call your function.

## When to use it

- Add a button that moves items from the player into the open container.
- Add a button that acts on the open container and needs to know whether it has room.

## Quick example

This mod adds a "Log room" button to every storage panel. Pressing it logs the free slot of the open container. The button is hidden while the player is trading, as the game does with its own buttons.

```csharp
using ObenseuerCore;

[ModInfo("example.roomcheck", "Room Check", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class RoomCheck : FrameworkMod
{
    protected override void OnLoad()
    {
        Events.WhenReady(() =>
        {
            if (!StoragePanel.AddButton(Mod.Id, "Log room", Report)) Log.Info("The storage panel button was not added.");
        });
    }

    private void Report(StoragePanelPress press)
    {
        Log.Info("Slot " + press.FreeSlot + " of " + press.Container.Name + " is free.");
    }
}
```

`Events.WhenReady` runs your code once a save is loaded, or at once if one already is. See [Using events](../guides/getting-started/using-events.md). `Mod.Id` is your mod's id (see the [glossary](../glossary.md#mod-id)).

## Common tasks

Put an item into the open container from your button. `Containers.Give` puts the items in the first slot that takes them. Your item id comes from your own item, see [Items](items.md).

```csharp
private void Store(StoragePanelPress press)
{
    if (!Containers.Give(press.Container, itemId, 1)) Log.Info("The item did not fit.");
}
```

Remove your button, for example when your mod's option is turned off. `RemoveButton` returns `false` when your mod had no button.

```csharp
bool removed = StoragePanel.RemoveButton(Mod.Id);
```

Ask whether a container has room without a press. `FreeSlot` returns `-1` when it has none.

```csharp
int slot = StoragePanel.FreeSlot(container);
```

## Reference

Text in brackets at the end of a note is extra detail you can skip on a first read.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>StoragePanel</code> <span>a button on the storage panel</span></p>

Call `AddButton` with your mod id, a label and a function. Call `RemoveButton` to take the button away, and `FreeSlot` to find an empty slot.

<div class="api-pair" markdown="1">

```csharp
public static class StoragePanel
{
    public static bool IsAvailable { get; }
    public static bool AddButton(string modId, string label, Action<StoragePanelPress> pressed);
    public static bool RemoveButton(string modId);
    public static int FreeSlot(ContainerInfo container);
}
```

| Method | Returns | What it does |
|---|---|---|
| `AddButton(modId, label, pressed)` | `bool` | Adds your button with its label. `true` when it was added. Calling it again with the same mod id changes the label and the function. |
| `RemoveButton(modId)` | `bool` | Removes your button. `false` when your mod had none. |
| `FreeSlot(container)` | `int` | The number of the first empty slot, or `-1` when the container has none or cannot be used. |
| `IsAvailable` | `bool` | `false` when the framework could not change the storage panel, so no button appears. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>StoragePanelPress</code> <span>what your function receives when the button is pressed</span></p>

The container that is open and the first empty slot in it.

<div class="api-pair" markdown="1">

```csharp
public sealed class StoragePanelPress
{
    public ContainerInfo Container { get; }
    public int FreeSlot { get; }
}
```

| Property | Type | What it is |
|---|---|---|
| `Container` | `ContainerInfo` | The open container. See [Inventory and containers](inventory.md). |
| `FreeSlot` | `int` | The first empty slot of the container. It is never `-1` when your function runs. |

</div>
</div>

### Things to know

- `AddButton` adds your button the next time the game shows a container in the storage panel. A button added while the panel is already open appears the next time it is shown.
- The button is a copy of the game's Take all button, with your label. It uses the same place and look in the panel. <span class="tech">(The copy is made from the Take all button's `UnityEngine.UI.Button`, its click is replaced, and the `DefaultUIButton` controller glyph is taken off the copy.)</span>
- The button is hidden while the player is trading, and a press during a trade does nothing.
- A press with no empty slot shows the game's full-storage warning, and your function is not called.
- `FreeSlot` counts a slot as empty when it holds no item. A locked container has no free slot.
- A container that belongs to an NPC, such as a shop or a stash that is not the player's, has no free slot either, and the button is hidden for it, so a press cannot reach an NPC owned container. `Containers.Give` itself has no ownership check, which is why the framework keeps the button away from those containers.
- The first empty slot is a hint that there is room. `Containers.Give` puts items in the first slot that takes them, which can be a partly filled stack, so it may not use the slot you were given.
- Your function runs on the game's main thread, from the button's click.
- If your function throws, the framework logs the error and the other mods' buttons still work.

### Limits

- `AddButton` returns `false` and logs once when the mod id is not valid (1 to 64 letters, digits, `.`, `_` or `-`), the label is empty, the function is `null`, or the framework could not change the storage panel.
- `RemoveButton` and `FreeSlot` do not throw. `FreeSlot` returns `-1` for a `null` container.
- The button is <span class="tech">(The hook is `StoragePanelUI.UpdateStoragePanelUI`, decompile `Items/StoragePanelUI.cs`.)</span>
- The button is mouse only. It is not part of the game's controller navigation, and the controller glyph is taken off the copy, so no key prompt is shown for it and the game's controller keys never press it. Use a key your own mod reads if you need a controller path. <span class="tech">(The copy keeps its `DefaultUIButton`, its `controllerButton` is set to `KeyCode.None` and its `controllerImage` object is destroyed, because `DefaultUIButton.InputTypeChanged` turns that object back on for every input type change.)</span>
