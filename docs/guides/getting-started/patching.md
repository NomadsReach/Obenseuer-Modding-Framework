---
title: Patching
parent: Getting started
nav_order: 7
---

# Patching

Patching means adding your own code to run just before or just after one of the game's own methods. It is the most fragile way to change the game: a game update can rename or remove the method you patched. Patch a game method only when no [event](using-events.md) or framework helper does what you need.

This page continues the project from [Your first mod](first-mod.md). Replace the code in `Plugin.cs` with the code below, then run `dotnet build -c Release -p:DeployToGame=true` with the game closed. The patch below makes your `AfterDayChanged` method run each time the game raises its day counter. A method that runs after the game's method is called a postfix. `Mod.Postfix` returns `true` when the patch was applied.

```csharp
using ObenseuerCore;

[ModInfo("example.hello", "Example Hello", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public class ExampleHello : FrameworkMod
{
    protected override void OnLoad()
    {
        // The game class and method to patch, then your class and the name of your static method.
        bool ok = Mod.Postfix(typeof(TimeOfDayAzure), "IncreaseDayCounter", typeof(ExampleHello), "AfterDayChanged");
        if (!ok) Log.Warn("day patch unavailable");
    }

    // Your patch method. It must be static.
    private static void AfterDayChanged()
    {
        CoreLog.For("example.hello").Info("day counter increased");
    }
}
```

Start the game and let a day pass. The log `BepInEx\logs\example.hello.log` gets the line `day counter increased`. A static method cannot use `Log`, so it asks for the log by mod id with `CoreLog.For`.

- **`Mod.Prefix`, `Mod.Postfix` and `Mod.Finalizer` add a patch to a game method.** A prefix runs before the game's method, a postfix runs after it, and a finalizer runs even when the game's method throws an error. Each returns `true` if the patch was applied.
- **Your patch method must be `static`.** You name it with its class and its name as text, as in the code above.
- **If a name has more than one version in the game, pass the parameter types.** Each of the three has an overload that takes a `Type[]` of parameter types after the method name. The patch is skipped and logged when the name is ambiguous.
- **A misspelled name for your own method throws an error.** The name is text, so the compiler cannot check it. Copy it exactly.
- **Always check the return value.** If a patch is not applied, fall back to the normal game behaviour, as the code above does by logging and carrying on.
- **A failed patch only skips that patch.** If the game's method has changed, the framework writes an error line to your mod's log and your mod keeps running. The patch itself does not run.
- **Patches work on other game builds unless the player forbids it.** `Core.IsSupportedBuild` is `true` on 0.5.08. On another build, patches still run by default and the log gets a warning. If the player sets `[Safety] RefuseUnknownBuild` to `true` in the framework's settings, every patch is refused and `Mod.Postfix` returns `false`.

## Patches your mod cannot work without

`Mod.Prefix` and the others make a normal patch. If your mod is useless without one patch, call `CorePatcher.Patch` instead and pass `true` for `core`. A failed core patch removes every patch made for your mod and turns the mod's patches off. The last argument is your mod id. It must not start with `obenseuer.moddingframework` or `nomad.obenseuer.core`, because the framework keeps both for itself.

```csharp
// Put this in OnLoad. The arguments are: game class, method name, parameter types (none here),
// prefix, postfix, finalizer (null for the ones you do not use), core, and your mod id.
MethodInfo postfix = typeof(ExampleHello).GetMethod("AfterDayChanged", BindingFlags.Static | BindingFlags.NonPublic);
bool ok = CorePatcher.Patch(typeof(TimeOfDayAzure), "IncreaseDayCounter", Type.EmptyTypes, null, postfix, null, true, Mod.Id);
if (!ok || CorePatcher.IsDisabled(Mod.Id)) Log.Warn("day patch unavailable");
```

This needs `using System;` and `using System.Reflection;` at the top of the file.

Patches use [Harmony](../../glossary.md#harmony), the library that changes game methods. Do not write raw Harmony patches in a mod. `CorePatcher` checks the game build first, and it removes your patches when a core patch fails.

## Where to go next

Next: [Packaging](packaging.md).
