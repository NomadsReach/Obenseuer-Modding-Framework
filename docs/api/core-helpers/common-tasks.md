---
title: Common tasks
parent: Framework helpers
grand_parent: Framework basics
nav_order: 1
---

# Common tasks

Run a method of yours just before a game method runs. The last two arguments say which method of yours to run: its type and its name.

```csharp
bool ok = Mod.Prefix(typeof(TimeOfDayAzure), "IncreaseDayCounter", typeof(Helpers), "AfterDay");
```

A game method can have several versions with the same name. For those, use the version of `Prefix`, `Postfix` or `Finalizer` that also takes an array of argument types, such as `new[] { typeof(int) }`, to say which version you mean. See the Reference below.

Run something once a save is ready. If a save is already loaded, it runs straight away.

```csharp
Events.WhenReady(() => Log.Info("save ready"));
```

If your mod is a BepInEx plugin and not a framework mod, get the handle from the plugin.

```csharp
CoreMod mod = CoreMod.For(this);
mod.Log.Info("ready");
```
