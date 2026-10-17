---
title: TweakState
parent: Framework core classes
grand_parent: Framework basics
nav_order: 12
---

# TweakState

`TweakState` lets your own code check whether a tweak is on. Today it has one member, for the renovations tweak.

```csharp
public static class TweakState
{
    public static bool RenovationsMoneyOnly { get; }  // true when Tweaks.Renovations MoneyOnly is on, as the game sees it
}
```

- **If your mod takes renovation items itself, check this first.** In your own prefix on `TenementController.RemoveRequiredItems`, return `false` early when it is `true`, before you take anything. The framework cannot do this for you, because HarmonyX runs every prefix and so yours runs no matter what the tweak says.
- **Read the flag from a small helper, and call the helper from inside try/catch.** If your mod runs with an older framework that does not have `RenovationsMoneyOnly`, the mod fails when the code that reads it is first run. That is a `TypeLoadException` or a `MissingMethodException`. It is thrown at the call into the helper, so a try/catch inside the helper does not catch it.
  - Technical: checked with a two-version probe on .NET Framework.

```csharp
private static bool MoneyOnlyOn()
{
    return ObenseuerCore.TweakState.RenovationsMoneyOnly;
}

private static bool MyPrefix()
{
    try
    {
        if (MoneyOnlyOn()) return false;
    }
    catch (Exception)
    {
    }
    return true;
}
```
