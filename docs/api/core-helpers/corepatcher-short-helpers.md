---
title: CorePatcher short helpers
parent: Framework helpers
grand_parent: Framework basics
nav_order: 3
---

# CorePatcher short helpers

A prefix runs your method before the game method, a postfix runs it after, and a finalizer runs it when the game method throws an error. These helpers set one of those up in one line. The `owner` is your mod id (see [Owner](../../glossary.md#owner)).

```csharp
public static class CorePatcher
{
    public static bool Prefix(Type target, string method, Type handlerType, string handlerName, string owner);                    // runs your method before a game method
    public static bool Prefix(Type target, string method, Type[] args, Type handlerType, string handlerName, string owner);       // the same, for a game method with several versions
    public static bool Postfix(Type target, string method, Type handlerType, string handlerName, string owner);                   // runs your method after a game method
    public static bool Postfix(Type target, string method, Type[] args, Type handlerType, string handlerName, string owner);      // the same, for a game method with several versions
    public static bool Finalizer(Type target, string method, Type handlerType, string handlerName, string owner);                 // runs your method when a game method throws an error
    public static bool Finalizer(Type target, string method, Type[] args, Type handlerType, string handlerName, string owner);    // the same, for a game method with several versions
}
```

- **`handlerName` is the name of a static method on `handlerType`, and it may be private.** If there is no such method, you get a `MissingMethodException`.
- **The result is `true` when the change was made and `false` when it was not.** It is `false` when it failed (the reason goes to `<owner>.log`) or when the owner has been turned off.
  - Technical: each helper calls `CorePatcher.Patch` with only the one handler it names filled in and its `core` argument set to `false`, and the result is the `bool` that `Patch` returns.
- **Use an owner that is not reserved.** A reserved owner throws an `ArgumentException`. The rules for the owner are the rules of `CorePatcher.Patch`, described on the [framework core page](../../api-core/corepatcher.md#corepatcher).
- **Without `args`, the name must match exactly one method on the target.** The method may be public or private, and an instance method or a static one. A name that is missing, or that has several versions, returns `false` and writes one line to the owner's log. For a method with several versions, use the version with `Type[] args`.
- **The handler can keep its Harmony settings.** `[HarmonyPriority]`, `[HarmonyBefore]` and `[HarmonyAfter]` on your method still apply, the same as with `Patch`. See [Harmony](../../glossary.md#harmony).

The long way and the short way to add a postfix, side by side:

```csharp
MethodInfo postfix = typeof(Helpers).GetMethod("AfterDay", BindingFlags.Static | BindingFlags.NonPublic);
bool longWay = CorePatcher.Patch(typeof(TimeOfDayAzure), "IncreaseDayCounter", null, null, postfix, null, false, "example.helpers");
bool shortWay = CorePatcher.Postfix(typeof(TimeOfDayAzure), "IncreaseDayCounter", typeof(Helpers), "AfterDay", "example.helpers");
```
