---
title: CorePatcher short helpers
parent: Framework helpers
grand_parent: Framework basics
nav_order: 3
---

# CorePatcher short helpers

A prefix runs your method before the game method, a postfix runs it after, and a finalizer runs it when the game method throws an error. These helpers set one of those up in one line. The `owner` is your mod id (see [Owner](../../glossary.md#owner)).

<div class="api-type" markdown="1">
<p class="api-type-head"><code>CorePatcher</code> <span>one-line prefix, postfix and finalizer helpers</span></p>

Call one of these with the game method, your handler's type and name, and your mod id as `owner`.

<div class="api-pair" markdown="1">

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

| Method | Returns | What it does |
|---|---|---|
| `Prefix(target, method, handlerType, handlerName, owner)` | `bool` | Runs your method before a game method. |
| `Prefix(target, method, args, handlerType, handlerName, owner)` | `bool` | The same, for a game method with several versions. |
| `Postfix(target, method, handlerType, handlerName, owner)` | `bool` | Runs your method after a game method. |
| `Postfix(target, method, args, handlerType, handlerName, owner)` | `bool` | The same, for a game method with several versions. |
| `Finalizer(target, method, handlerType, handlerName, owner)` | `bool` | Runs your method when a game method throws an error. |
| `Finalizer(target, method, args, handlerType, handlerName, owner)` | `bool` | The same, for a game method with several versions. |

</div>
</div>

### Things to know

- The result is `true` when the change was made and `false` when it was not. It is `false` when it failed, and the reason goes to `<owner>.log`, or when the owner has been turned off. <span class="tech">(Each helper calls `CorePatcher.Patch` with only the one handler it names filled in and its `core` argument set to `false`, and the result is the `bool` that `Patch` returns.)</span>
- The handler can keep its Harmony settings. `[HarmonyPriority]`, `[HarmonyBefore]` and `[HarmonyAfter]` on your method still apply, the same as with `Patch`. See [Harmony](../../glossary.md#harmony).

### Limits

- `handlerName` is the name of a static method on `handlerType`, and it may be private. If there is no such method, you get a `MissingMethodException`.
- Use an owner that is not reserved. A reserved owner throws an `ArgumentException`. The rules for the owner are the rules of `CorePatcher.Patch`, described on the [framework core page](../../api-core/corepatcher.md#corepatcher).
- Without `args`, the name must match exactly one method on the target. The method may be public or private, and an instance method or a static one. A name that is missing, or that has several versions, returns `false` and writes one line to the owner's log. For a method with several versions, use the version with `Type[] args`.

### Example

The long way and the short way to add a postfix, side by side:

```csharp
MethodInfo postfix = typeof(Helpers).GetMethod("AfterDay", BindingFlags.Static | BindingFlags.NonPublic);
bool longWay = CorePatcher.Patch(typeof(TimeOfDayAzure), "IncreaseDayCounter", null, null, postfix, null, false, "example.helpers");
bool shortWay = CorePatcher.Postfix(typeof(TimeOfDayAzure), "IncreaseDayCounter", typeof(Helpers), "AfterDay", "example.helpers");
```
