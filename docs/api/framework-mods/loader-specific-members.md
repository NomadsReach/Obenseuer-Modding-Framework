---
title: Loader-specific members
parent: Framework mods
grand_parent: Framework basics
nav_order: 8
---

# Loader-specific members

Some parts of the framework use types from BepInEx. A framework mod has no reference to BepInEx, so it can't call these, and the compiler stops with error CS0012. Only BepInEx plugins can call them. The right column says what a framework mod does instead.

| Member | BepInEx type it needs | A framework mod uses |
|---|---|---|
| `CoreMod.For(BaseUnityPlugin)` | `BaseUnityPlugin`, the base class of a BepInEx plugin | `FrameworkMod.Mod` |
| `Loader.Find(string)` | `PluginInfo`, BepInEx's record of a plugin | `Loader.Has(string)` |
| `Loader.All()` | `PluginInfo` | nothing |
| `ModUi.KeyBinding(...)` | `KeyboardShortcut`, BepInEx's key and modifier combination | nothing |
| `ModKeyBinding.Set(KeyboardShortcut)` | `KeyboardShortcut` | nothing |
| `Core` base type | `BaseUnityPlugin` | its static members work |
| `WebUiPlugin` | `BaseUnityPlugin` | nothing, mods never use it |
