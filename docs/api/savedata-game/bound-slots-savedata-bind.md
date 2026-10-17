---
title: "Bound slots: SaveData.Bind"
parent: Game save data
grand_parent: Framework basics
nav_order: 5
---

# Bound slots: SaveData.Bind

`Bind` is the shortest way to keep data in the player's save. You get a slot whose `Value` is saved and loaded for you.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>SaveData</code> <span>make a slot that is saved and loaded for you</span></p>

`Bind<T>(modId, key)` is the only method here. It gives you a `SaveSlot<T>` that the framework saves and loads for you.

<div class="api-pair" markdown="1">

```csharp
public static class SaveData
{
    public static SaveSlot<T> Bind<T>(string modId, string key) where T : class, new();
}
```

| Method | Returns | What it does |
|---|---|---|
| `Bind<T>(modId, key)` | `SaveSlot<T>` | Makes a slot that is saved and loaded for you. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>SaveSlot&lt;T&gt;</code> <span>your saved data</span></p>

`Bind` gives you one of these. Keep your data in `Value`, and read `Value` again after a load, because a load replaces the object.

<div class="api-pair" markdown="1">

```csharp
public sealed class SaveSlot<T> where T : class, new()
{
    public T Value { get; set; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Value` | `T` | Your data. Never `null`. Setting it to `null` throws `ArgumentNullException`. |

</div>
</div>

### Register and Bind side by side

Here is the same data kept with `Register` and with `Bind`:

```csharp
MyState state = new MyState();
SaveData.Register<MyState>("com.example.mymod.state", () => state, s => state = s ?? new MyState());
state.Coins++;
```

```csharp
SaveSlot<MyState> slot = SaveData.Bind<MyState>("com.example.mymod", "state");
slot.Value.Coins++;
```

### Things to know

- `Bind` saves your data in a file named after your mod id and the key. The file is `<Folder>\mods\<modId>.<key>.json`. `modId` and `key` can use letters, digits, `.`, `_` and `-`, and the joined name can be at most 64 characters. `CoreMod.Save(key, ...)` for the same mod uses the same file. <span class="tech">(it registers the name `<modId>.<key>` through `SaveData.Register`.)</span>
- `Value` is a new `T` until a load, and a load replaces it. The load gives you the saved object, or a new `T` when the file is missing or cannot be read. Read `Value` each time instead of keeping a reference to the old object.
- Set `Value` after the load has finished. If a save is already loaded when `Bind` runs, the value loads at once. Otherwise it loads on `Events.Loaded`, which replaces any value you set before it. Set your changes inside `Events.WhenReady`, or after it, so the load does not undo them.
- Your data is saved on the game's normal saves, and sometimes the framework holds a save back to protect it. A level change (going through a door) saves your data even though a save is "busy".
- Going through a door reloads your data from the autosave, so it brings back what was just saved.

### Limits

- Using the same name twice throws `InvalidOperationException`. A second `Bind` or `Save` with the same name does, just as `Register` does.
- A save made while a load from the menu or the pause menu is still running writes no `Register` or `Bind` values, because they still hold the previous save's data. A load that fails ends that wait, so saves write again.
- A save is also skipped for a file that could not be read at the last load, until a load reads it again. A load is never skipped. <span class="tech">(`SaveData.Busy` does not skip a save. A level change saves while `Busy` is true. A level change is not skipped this way unless it starts while such a load is still running.)</span>
- A change you make between the save and the load of that door change is lost. For example, a change made in an `Events.SceneLoaded` handler during a door change is overwritten by the saved value. Make such changes in `Events.Loaded` or `Events.WhenReady`, or ignore that window. A save that fails keeps the previous file, so the reload brings back the previous value. <span class="tech">(a door change (`SaveController.ChangeLevel`) runs `SaveGame` before it loads the new level. `SaveGame` copies the loaded save's files into the older autosave slot (`Autosave` or `Autosave2`) and raises `SavingDone`, which writes every `Register` and `Bind` value to that slot's `mods\<modId>.json`. The level load then reads those files back. A failed save is an exception in a save callback or a file over 64 MB.)</span>
