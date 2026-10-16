---
title: Patching
parent: Getting started
nav_order: 7
---

# Patching

Patch a game method only when no event or helper does what you need. Check the game build first.

```csharp
using System;
using System.Reflection;
using BepInEx;
using BepInEx.Unity.Mono;
using ObenseuerCore;

[BepInPlugin("example.hello", "Example Hello", "0.1.0")]
[BepInDependency("obenseuer.moddingframework", ">=0.11.0")]
public class ExampleHello : BaseUnityPlugin
{
    private static readonly CoreLog Log = CoreLog.For("example.hello");

    private void Awake()
    {
        if (!Core.PatchingAllowed)
        {
            Log.Warn("patching refused on " + Core.GameBuild);
            return;
        }
        MethodInfo postfix = typeof(ExampleHello).GetMethod("AfterDayChanged", BindingFlags.Static | BindingFlags.NonPublic);
        bool ok = CorePatcher.Patch(typeof(TimeOfDayAzure), "IncreaseDayCounter", Type.EmptyTypes, null, postfix, null, false, "example.hello");
        if (!ok || CorePatcher.IsDisabled("example.hello")) Log.Warn("day patch unavailable");
    }

    private static void AfterDayChanged()
    {
        Log.Info("day counter increased");
    }
}
```

On BepInEx 5, write `"0.11.0"` instead, because BepInEx 5 cannot parse a version range, and drop `using BepInEx.Unity.Mono;` (`BaseUnityPlugin` then comes from `BepInEx`). See [Supporting BepInEx 5 too](packaging.md#supporting-bepinex-5-too).

- `CorePatcher.Patch` adds a prefix, a postfix or a finalizer to a game method. It returns `true` if the patch was applied.
- Pass `null` for the parts you do not use. Patch methods are `static`.
- Pass `core: false` for a mod patch. If it fails, only that patch is skipped.
- Pass `core: true` only when the mod cannot work without the patch. A failed core patch removes every patch made for your mod, and disables it.
- `owner` is your mod id. It must not start with `obenseuer.moddingframework` or `nomad.obenseuer.core`. Both prefixes are reserved for the framework.
- `Core.IsSupportedBuild` is `true` on 0.5.08. On another build, patches still run by default and the log gets a warning. If the player sets `[Safety] RefuseUnknownBuild` to `true`, every patch is refused.
- Check the return value, and fall back to the normal game behaviour when a patch is not applied.

Patches use [Harmony](../../glossary.md#harmony) under the hood. Do not write raw Harmony patches in a mod. `CorePatcher` checks the build, and it removes your patches when a core patch fails.

Next: [Packaging](packaging.md).
