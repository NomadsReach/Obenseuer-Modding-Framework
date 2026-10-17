---
title: CoreMod
parent: Framework helpers
grand_parent: Framework basics
nav_order: 2
---

# CoreMod

This page uses `KeyCode` from Unity. The [glossary](../../glossary.md#types-that-come-from-unity) says what each one is.
{: .unity-types}

`CoreMod` is a handle for one mod. It remembers your mod id and passes it on to the framework's save data, keybind, log and patch calls. One handle belongs to one mod id.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>CoreMod</code> <span>a handle for one mod</span></p>

Call `CoreMod.For` once with your plugin, then use the handle for `Save`, `Key`, `Log` and the patch calls.

<div class="api-pair" markdown="1">

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

| Method | Returns | What it does |
|---|---|---|
| `For(plugin)` | `CoreMod` | The handle for a BepInEx plugin. |
| `Save<T>(key, save, load)` | `void` | Registers something to save in the player's save. |
| `Key(id, displayName, defaultKey)` | `ModKeybind` | Registers a keybind. |
| `Prefix(target, method, handlerType, handlerName)` | `bool` | Runs your method before a game method. |
| `Prefix(target, method, args, handlerType, handlerName)` | `bool` | The same, for a game method with several versions. |
| `Postfix(target, method, handlerType, handlerName)` | `bool` | Runs your method after a game method. |
| `Postfix(target, method, args, handlerType, handlerName)` | `bool` | The same, for a game method with several versions. |
| `Finalizer(target, method, handlerType, handlerName)` | `bool` | Runs your method when a game method throws an error. |
| `Finalizer(target, method, args, handlerType, handlerName)` | `bool` | The same, for a game method with several versions. |

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | Your mod id. |
| `Log` | `CoreLog` | Your mod's log. |

</div>
</div>

## Things to know

- A framework mod already has its handle, in `Mod`. Only a BepInEx plugin calls `CoreMod.For`, because `For` needs a BepInEx plugin object, and a framework mod has no BepInEx plugin class. <span class="tech">(A framework mod gets its handle from `FrameworkMod.Mod`.)</span>
- Asking for the handle twice gives you the same handle. The mod id is the plugin's GUID, and capital letters do not matter. <span class="tech">(`For` keys the handle on `plugin.Info.Metadata.GUID`, compared without case.)</span>
- `Id` is the mod id the handle was first made with. It is the id the patch helpers file their changes under, and the id `Key` uses for the keybind. See [Mod id](../../glossary.md#mod-id) and [Owner](../../glossary.md#owner).
- `Log` writes to your mod's own log file, and it stops a flood of errors from filling the file. It writes the same error only once in 10 seconds, no more than 20 errors in 10 seconds, and no more than 100 lines in a second. When it drops lines, it writes one summary line, such as `5 error lines suppressed` or `5 lines suppressed`, with the next line it writes or when the game quits. <span class="tech">(`Log` is `CoreLog.For(Id)`. The `Error(message, exception)` overload counts an error as the same one when the exception type and first stack frame match.)</span>
- `Save` registers something to save in the player's save, under a name made from your mod id and your key. Use a key of letters, digits, `.`, `_` and `-`. It follows the same rules as `SaveData.Register` (see [Game save data](../savedata-game.md)). <span class="tech">(It calls `SaveData.Register` with the name `<Id>.<key>.json`.)</span>
- `Key` registers a keybind and gives you what `Keybinds.Register` gives you, which can be `null`. <span class="tech">(It calls `Keybinds.Register(Id, id, displayName, defaultKey)`.)</span>
- `Prefix`, `Postfix` and `Finalizer` change a game method by name, using your mod id. They are the same as the `CorePatcher` short helpers in the next section, with your mod id filled in for you.

## Limits

- Passing `null` to `For` throws an `ArgumentNullException`.
- You can register each `Save` name only once. The second time throws an `InvalidOperationException`.
