---
title: Setting and clearing
parent: World sky
grand_parent: Places and travel
nav_order: 2
---

# Setting and clearing

This is the full list of what the API gives you. In the code blocks, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

`WorldSky` is the static class you call. `modId` is your mod's id (see [Mod id](../../glossary.md#mod-id)).

```csharp
public static class WorldSky
{
    public static bool IsAvailable { get; }                      // false when the framework could not set up the sky changes
    public static SkyState Current { get; }                      // what the sky looks like now, with any overrides included; null when it cannot be read
    public static bool Set(string modId, SkyOverride values);    // applies your values; true when they were applied
    public static bool Clear(string modId);                      // removes your values; true when you had some
}
```

- **`Set` applies your values on top of what the game does.** It returns `false` when `IsAvailable` is `false`. If you call `Set` again, your new values replace your earlier ones. The values are copied, so changing your `SkyOverride` afterwards does nothing until you call `Set` again.
- **`Set` throws an `ArgumentException` for a bad call.** That happens for an invalid mod id, a `null` `values`, a number that is not a real number (`NaN` or infinity), or a density, intensity or exposure below `0`.
- **`CloudDensity` is held between `0` and `1`.** A larger or smaller number is clamped to that range.
- **`Clear` removes only your values and returns `true` when you had some.** It throws an `ArgumentException` for an invalid mod id.
- **`Current` is `null` when the framework cannot read the sky.** It never throws.
