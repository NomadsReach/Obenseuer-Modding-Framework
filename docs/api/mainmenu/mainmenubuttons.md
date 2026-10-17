---
title: MainMenuButtons
parent: Main menu
grand_parent: Framework basics
nav_order: 2
---

# MainMenuButtons

This is the full list of what the API gives you. In every code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

```csharp
public static class MainMenuButtons
{
    public static bool Add(string id, string label, Action onClick);                  // adds a button with no mod id; true when added
    public static bool Add(string modId, string id, string label, Action onClick);    // adds a button for your mod; true when added
    public static void SetControlsEnabled(bool enabled);                    // switches the menu's controls off or on, with no mod id
    public static void SetControlsEnabled(string modId, bool enabled);      // the same, counted for your mod
    public static bool IsOpen { get; }                                      // true when the main menu is up and has your buttons
}
```
