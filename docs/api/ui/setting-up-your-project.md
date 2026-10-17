---
title: Setting up your project
parent: UI
grand_parent: Screen and UI
nav_order: 2
---

# Setting up your project

This is the full list of what the API gives you, one section for each part. In every code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

- **Reference two game files in your project.** They are `Unity.TextMeshPro.dll` and `UnityEngine.UI.dll`, both in `Obenseuer_Data\Managed` in the game folder. `ModUi.Title` and `ModUi.Text` give back a `TextMeshProUGUI`, and `ModUi.Button` gives back a `UnityEngine.UI.Button`. Without both references the compiler reports error CS0012, which means a type is defined in a file your project does not reference.
- **Add `using ObenseuerCore.Ui;` to each file that uses the kit.**
- **`KeyBinding` needs one more `using`, and only a BepInEx plugin can call it.** `KeyBinding` takes a `KeyboardShortcut` from BepInEx, so a file that calls it also needs `using BepInEx.Unity.Mono.Configuration;` on BepInEx 6, or `using BepInEx.Configuration;` on BepInEx 5. A framework mod has no BepInEx reference, so it cannot call `KeyBinding`. See [framework mods](../framework-mods/loader-specific-members.md#loader-specific-members).

```csharp
using BepInEx.Unity.Mono.Configuration; // BepInEx 6
// using BepInEx.Configuration;         // BepInEx 5
using ObenseuerCore.Ui;
```
