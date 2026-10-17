---
title: CoreMod
parent: Framework helpers
grand_parent: Framework basics
nav_order: 2
---

# CoreMod

This is the full list of what the API gives you, one section for each part. In every code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

`CoreMod` is a handle for one mod. It remembers your mod id and passes it on to the framework's save data, keybind, log and patch calls. One handle belongs to one mod id.

```csharp
public sealed class CoreMod
{
    public static CoreMod For(BaseUnityPlugin plugin);   // the handle for a BepInEx plugin
    public string Id { get; }                            // your mod id
    public CoreLog Log { get; }                          // your mod's log
    public void Save<T>(string key, Func<T> save, Action<T> load) where T : class, new();    // registers something to save
    public ModKeybind Key(string id, string displayName, KeyCode defaultKey);                // registers a keybind
    public bool Prefix(Type target, string method, Type handlerType, string handlerName);                       // runs your method before a game method
    public bool Prefix(Type target, string method, Type[] args, Type handlerType, string handlerName);          // the same, for a game method with several versions
    public bool Postfix(Type target, string method, Type handlerType, string handlerName);                      // runs your method after a game method
    public bool Postfix(Type target, string method, Type[] args, Type handlerType, string handlerName);         // the same, for a game method with several versions
    public bool Finalizer(Type target, string method, Type handlerType, string handlerName);                    // runs your method when a game method throws an error
    public bool Finalizer(Type target, string method, Type[] args, Type handlerType, string handlerName);       // the same, for a game method with several versions
}
```

- **A framework mod already has its handle, in `Mod`. Only a BepInEx plugin calls `CoreMod.For`.** `For` needs a BepInEx plugin object, so a framework mod (which has no BepInEx plugin class) can't use it.
  - Technical: a framework mod gets its handle from `FrameworkMod.Mod`.
- **Asking for the handle twice gives you the same handle.** The mod id is the plugin's GUID, and capital letters do not matter. Passing `null` throws an `ArgumentNullException`.
  - Technical: `For` keys the handle on `plugin.Info.Metadata.GUID`, compared without case.
- **`Id` is the mod id the handle was first made with.** It is the id the patch helpers file their changes under, and the id `Key` uses for the keybind. See [Mod id](../../glossary.md#mod-id) and [Owner](../../glossary.md#owner).
- **`Log` writes to your mod's own log file, and it stops a flood of errors from filling the file.** It writes the same error only once in 10 seconds, no more than 20 errors in 10 seconds, and no more than 100 lines in a second. When it drops lines, it writes one summary line, such as `5 error lines suppressed` or `5 lines suppressed`, with the next line it writes or when the game quits.
  - Technical: `Log` is `CoreLog.For(Id)`. The `Error(message, exception)` overload counts an error as the same one when the exception type and first stack frame match.
- **`Save` registers something to save in the player's save, under a name made from your mod id and your key.** Use a key of letters, digits, `.`, `_` and `-`. You can register each name only once: the second time throws an `InvalidOperationException`. It follows the same rules as `SaveData.Register` (see [Game save data](../savedata-game.md)).
  - Technical: it calls `SaveData.Register` with the name `<Id>.<key>.json`.
- **`Key` registers a keybind and gives you what `Keybinds.Register` gives you, which can be `null`.**
  - Technical: it calls `Keybinds.Register(Id, id, displayName, defaultKey)`.
- **`Prefix`, `Postfix` and `Finalizer` change a game method by name, using your mod id.** They are the same as the `CorePatcher` short helpers in the next section, with your mod id filled in for you.
