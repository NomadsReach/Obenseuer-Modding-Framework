---
title: Framework helpers API
parent: API reference
nav_order: 94
---

# Framework helpers API

Experimental.
Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Log: `CoreMod.Log` writes to `BepInEx\logs\<your guid>.log`.

Three additions that cut the setup code a mod repeats. Each one forwards to an existing API and changes none of them: `CoreMod` wraps `SaveData.Register`, `Keybinds.Register` and `CorePatcher.Patch`, the `CorePatcher` short helpers wrap `CorePatcher.Patch`, and `Events.WhenReady` runs its action at the ready point that follows `Events.Loaded`, or at a new game's first level. `SaveData.Bind` is on the [game save data page](savedata-game.md).

## CoreMod

```csharp
public sealed class CoreMod
{
    public static CoreMod For(BaseUnityPlugin plugin);
    public string Id { get; }
    public CoreLog Log { get; }
    public void Save<T>(string key, Func<T> save, Action<T> load) where T : class, new();
    public ModKeybind Key(string id, string displayName, KeyCode defaultKey);
    public bool Prefix(Type target, string method, Type handlerType, string handlerName);
    public bool Prefix(Type target, string method, Type[] args, Type handlerType, string handlerName);
    public bool Postfix(Type target, string method, Type handlerType, string handlerName);
    public bool Postfix(Type target, string method, Type[] args, Type handlerType, string handlerName);
    public bool Finalizer(Type target, string method, Type handlerType, string handlerName);
    public bool Finalizer(Type target, string method, Type[] args, Type handlerType, string handlerName);
}
```

- `For` is loader-specific: it takes a `BaseUnityPlugin`, so only BepInEx plugins can call it. A framework mod gets its handle from `FrameworkMod.Mod`.
- `For` keys on `plugin.Info.Metadata.GUID`. The same GUID (any letter case) returns the same handle, and a null plugin throws `ArgumentNullException`.
- `Id` is the GUID the handle was first made with. It is the owner for the patch helpers and the id for `Key`.
- `Log` is `CoreLog.For(Id)`, so it writes to the mod's own log file. Its `Error(message, exception)` overload writes an error once per exception type and first stack frame in each 10 seconds, and every logger writes at most 20 errors in each 10 seconds and 100 lines a second, then one summary line, `N error lines suppressed` for the error cap and `N lines suppressed` for the line cap, written with that logger's next line or at quit.
- `Save` calls `SaveData.Register` with the name `<Id>.<key>.json`. It has the same rules as `Register`: one registration per name, and a second one throws `InvalidOperationException`. `key` uses letters, digits, `.`, `_` and `-`.
- `Key` calls `Keybinds.Register(Id, id, displayName, defaultKey)` and returns what it returns, `null` included.
- The patch helpers call the `CorePatcher` short helpers below with `Id` as the owner, so they keep the owner rules of `Patch`.

Before:

```csharp
ModKeybind open = Keybinds.Register("com.example.mymod", "open", "Open menu", KeyCode.F7);
SaveData.Register<MyState>("com.example.mymod.state", () => state, s => state = s ?? new MyState());
bool ok = CorePatcher.Postfix(typeof(TimeOfDayAzure), "IncreaseDayCounter", typeof(MyMod), "AfterDay", "com.example.mymod");
```

After:

```csharp
CoreMod mod = CoreMod.For(this);
ModKeybind open = mod.Key("open", "Open menu", KeyCode.F7);
mod.Save<MyState>("state", () => state, s => state = s ?? new MyState());
bool ok = mod.Postfix(typeof(TimeOfDayAzure), "IncreaseDayCounter", typeof(MyMod), "AfterDay");
mod.Log.Info("ready");
```

## CorePatcher short helpers

```csharp
public static class CorePatcher
{
    public static bool Prefix(Type target, string method, Type handlerType, string handlerName, string owner);
    public static bool Prefix(Type target, string method, Type[] args, Type handlerType, string handlerName, string owner);
    public static bool Postfix(Type target, string method, Type handlerType, string handlerName, string owner);
    public static bool Postfix(Type target, string method, Type[] args, Type handlerType, string handlerName, string owner);
    public static bool Finalizer(Type target, string method, Type handlerType, string handlerName, string owner);
    public static bool Finalizer(Type target, string method, Type[] args, Type handlerType, string handlerName, string owner);
}
```

- Each one is `CorePatcher.Patch(target, method, args, hook, null, null, false, owner)` for the one hook it names. `handlerName` is a static method on `handlerType`, public or not. A name that is not there throws `MissingMethodException`.
- The return value is the `bool` that `Patch` returns: `true` when the patch is in, `false` on failure (logged to `<owner>.log`) or when the owner is disabled.
- The owner rules are those of `Patch`. A reserved owner throws `ArgumentException`.
- Without `args`, the name must match exactly one method on the target, public or not, instance or static. A missing or overloaded name returns `false` and logs once to the owner's log. Use the `Type[]` overload for an overloaded target.
- Harmony attributes on the handler (`[HarmonyPriority]`, `[HarmonyBefore]`, `[HarmonyAfter]`) are kept, the same as `Patch`.

Before:

```csharp
MethodInfo postfix = typeof(MyMod).GetMethod("AfterDay", BindingFlags.Static | BindingFlags.NonPublic);
bool ok = CorePatcher.Patch(typeof(Foo), "Bar", new[] { typeof(int) }, null, postfix, null, false, "com.example.mymod");
```

After:

```csharp
bool ok = CorePatcher.Postfix(typeof(Foo), "Bar", new[] { typeof(int) }, typeof(MyMod), "AfterDay", "com.example.mymod");
```

## Events.WhenReady

```csharp
public static class Events
{
    public static void WhenReady(Action action);
}
```

- When a save is loaded and not loading, or a new game's first level after the intro has loaded, `action` runs at once. Otherwise it runs once, at the next of those, and is then removed.
- A save counts as loaded when `SaveData.Folder` is not `null` and no load is running. A save in progress does not matter. `Folder` is set when a load completes, so a load that fails does not count. A new game counts once its first level after the intro has loaded, until a save load starts, and again if that load fails. If a save or a new game was ready before that load, actions queued while it ran run when the failure is detected. When a load from the menu or a level change is what finds the failure, they run when that load completes, or when that load fails and its failure is detected.
- An exception from `action` is logged once (later failures of the same action are not logged) and does not reach other handlers. A null `action` throws `ArgumentNullException`.
- Only the first ready point is used. A later load does not run the action again.

Before:

```csharp
Events.Loaded += OnLoaded;
```

After:

```csharp
Events.WhenReady(() => Logger.LogInfo("save ready"));
```

The before form misses a save that was loaded before the handler was added. `WhenReady` covers both cases.

## Self-check

`[Debug] CoreModSelfCheck = true` runs the self-check at start and logs `coremod self-check ok` or the first failing case. It checks that the same GUID gives the same `CoreMod` and that a different GUID does not, that `Id` matches the GUID, and that a `SaveSlot` value is never null: a new slot has a value, loading `null` gives a new value, a loaded value is kept, and setting `Value` to `null` throws. It also checks the name lookup behind the patch helpers on `StringBuilder`: `Clear` is found, `Append` (overloaded) and a missing name are not. It only looks the names up, so nothing is patched. It does not call `SaveData.Bind` or `CoreMod.Save`, because that would add a save entry to the player's save folder.
