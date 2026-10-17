---
title: Common tasks
parent: Game save data
grand_parent: Framework basics
nav_order: 1
---

# Common tasks

Copy the player's inventory entry out of the game and put it back. Both give you `null` or `false` if the game refuses (see the reference below). This changes the live inventory, so try it on a scratch save.

```csharp
string entry = SaveData.Capture(Inventory.instance);
if (entry != null)
{
    bool applied = SaveData.Apply(Inventory.instance, entry);
    Log.Info(applied ? "Put the inventory back." : "The game refused the entry.");
}
```

Do the same for an object by its GUID. When another player chooses the GUID, always give the list of GUIDs that player may touch.

```csharp
string json = SaveData.CaptureByGuid("some guid");
HashSet<string> allowed = new HashSet<string> { "some guid" };
if (json != null) SaveData.ApplyByGuid("some guid", json, allowed);
```

Read your mod's saved data early, and find the save folder.

```csharp
MyState early = SaveData.Peek<MyState>(Info.Id);
if (early != null) Log.Info("The save being loaded has " + early.Coins + " coins.");
Log.Info("Save folder: " + SaveData.Folder);
```

Keep a counter in the save with `Bind`.

```csharp
SaveSlot<MyState> slot = SaveData.Bind<MyState>(Info.Id, "state");
slot.Value.Coins++;
```

Do the same with `Register`, where you decide how to get and put back your data yourself.

```csharp
MyState state = new MyState();
SaveData.Register<MyState>(Info.Id + ".manual", () => state, s => state = s ?? new MyState());
```

Check whether a save is loading or saving right now.

```csharp
Log.Info(SaveData.Busy ? "A save is loading or saving." : "Free to use.");
```

These examples use a small class of your own, for example `public sealed class MyState { public int Coins; }`.
