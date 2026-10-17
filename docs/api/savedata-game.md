---
title: Game save data
parent: Framework basics
grand_parent: API reference
has_children: true
nav_order: 6
---

# Game save data

Status: experimental.

## What it is

The game saves the world in its own save files (they end in `.tnmt`). Each thing the game saves has an entry in that file, with an id (a GUID, see the [glossary](../glossary.md#guid)) and its data. This API lets your mod copy one of those entries into a piece of text and put it back later, without touching the save files on disk. It also lets your mod keep its own saved data next to the game's save: read it early, or bind it to a slot that saves and loads for you.

All of it is on the `SaveData` class, next to `SaveData.Register`. `SaveData.Register` is the simple way for a mod to save its own data, and it is described in [SaveData](../api-core/savedata.md#savedata). Everything here must run from your mod's normal code, not from a background thread you started.

## When to use it

- Keep your mod's own data in the player's save with almost no code (`SaveData.Bind`).
- Read your mod's data before the save finishes loading (`SaveData.Peek`).
- Copy a game object's saved state, for example to sync it between two players, and apply it again (`Capture` and `Apply`).

## Quick example

This mod keeps a counter in the player's save. Each time a save is loaded, it adds one to the counter and logs the result. The counter is saved and loaded for you.

```csharp
using ObenseuerCore;

public sealed class CoinState
{
    public int Coins;
}

[ModInfo("example.coinsave", "Coin Save", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class CoinSave : FrameworkMod
{
    protected override void OnLoad()
    {
        SaveSlot<CoinState> slot = SaveData.Bind<CoinState>(Info.Id, "state");
        Events.WhenReady(() =>
        {
            slot.Value.Coins++;
            Log.Info("This save has been loaded " + slot.Value.Coins + " times.");
        });
    }
}
```

`CoinState` is your own class that holds the data you want to keep. `Bind` gives you a slot whose `Value` is saved with the game and loaded back. `Events.WhenReady` waits until a save is loaded before it runs your code, so the change is not undone by the load. See [Using events](../guides/getting-started/using-events.md).
