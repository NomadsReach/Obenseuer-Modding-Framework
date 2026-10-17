---
title: CorePatcher
parent: Framework core classes
grand_parent: Framework basics
nav_order: 3
---

# CorePatcher

The framework changes some of the game's methods so it can tell your mod what is happening. `CorePatcher` lets your mod change a game method the same way: run your code just before it, just after it, or around it. Use it instead of calling Harmony (the patching library BepInEx ships) yourself. The framework keeps track of every change, undoes them together if a game update breaks one, and keeps different mods from stepping on each other. See [Patching](../guides/getting-started/patching.md) for a longer walkthrough.

A change that runs before the game method is called a prefix, one that runs after is a postfix, and one that runs even when the game method throws an error is a finalizer. Pass `null` for the ones you do not need.

```csharp
public static class CorePatcher
{
    // Change a game method given as a method object.
    public static bool Patch(MethodBase target, MethodInfo prefix, MethodInfo postfix, MethodInfo finalizer, bool core, string owner);
    // The same, but find the game method by its type, name and parameter types.
    public static bool Patch(Type type, string name, Type[] args, MethodInfo prefix, MethodInfo postfix, MethodInfo finalizer, bool core, string owner);
    // The same as the first, with a priority that decides the order against other mods' changes.
    public static bool Patch(MethodBase target, MethodInfo prefix, MethodInfo postfix, MethodInfo finalizer, bool core, string owner, int priority);
    // The same as the second, with a priority.
    public static bool Patch(Type type, string name, Type[] args, MethodInfo prefix, MethodInfo postfix, MethodInfo finalizer, bool core, string owner, int priority);
    // True when the framework turned off all changes made under this mod id.
    public static bool IsDisabled(string owner);
}
```

Every `Patch` call returns `true` when the change was made and `false` when it was not. In these calls, `owner` is your mod's id (see [Owner](../glossary.md#owner) in the glossary).

- **Your mod id must not start with `obenseuer.moddingframework` or `nomad.obenseuer.core`, and it must not be empty.** Those two starts are reserved for the framework's own changes, in any capital or small letters. If you break this rule, all four calls throw an `ArgumentException` before they do anything else.
- **The short calls (without `priority`) keep the priority attributes you put on your methods.** That means `[HarmonyPriority]`, `[HarmonyBefore]` and `[HarmonyAfter]`.
  - Technical: checked with HarmonyX 2.9.
- **`priority` decides which of several mods' changes runs first.** It applies to your prefix, postfix and finalizer, and it replaces a `[HarmonyPriority]` attribute. Pass `-1` to leave the attributes alone. A higher number runs earlier for prefixes. The normal value is 400 (`Priority.Normal`) and the highest common value is 800 (`Priority.First`).
  - Technical: it is the HarmonyX priority, `HarmonyLib.Priority`.
- **A prefix that returns `false` skips the game method, but it does not stop other mods' prefixes.** Every prefix still runs, even when an earlier one returned `false`. A higher priority makes yours run before the others. It cannot stop them.
  - Technical: this is how HarmonyX works. A `false` from a prefix only skips the original method.
- **Each mod id gets its own Harmony instance.** Your changes are kept apart from everyone else's.
- **When a change fails, you get `false` and a line in your mod's log.** The log is `BepInEx\logs\<owner>.log`.
- **`core: true` means this change is critical for your mod.** If it fails, the framework removes every change it made under your mod id, and `IsDisabled(owner)` becomes true. Changes you made yourself with `new Harmony(owner)` are not touched.
- **Once your mod id is disabled, it stays disabled until the game restarts.** Later `Patch` calls return `false` and write nothing to the log.
- **The framework refuses to change anything when `Core.PatchingAllowed` is false.** The call returns `false` and logs `patch skipped, unsupported game build`. That only happens when the game build is not 0.5.08 and `[Safety] RefuseUnknownBuild` is set. By default an unknown build is changed anyway.

This plugin runs a small method after the game counts a new day. If the change cannot be made, it logs a warning.

```csharp
using System;
using System.Reflection;
using BepInEx;
using BepInEx.Unity.Mono;
using ObenseuerCore;

public class MyMod : BaseUnityPlugin
{
    private void Awake()
    {
        MethodInfo postfix = typeof(MyMod).GetMethod("AfterDayChanged", BindingFlags.Static | BindingFlags.NonPublic);
        bool ok = CorePatcher.Patch(typeof(TimeOfDayAzure), "IncreaseDayCounter", Type.EmptyTypes, null, postfix, null, false, "example.mymod");
        if (!ok || CorePatcher.IsDisabled("example.mymod")) Logger.LogWarning("MyMod patch unavailable");
    }

    private static void AfterDayChanged() { }
}
```
