---
title: MainMenuButtons
parent: Main menu
grand_parent: Framework basics
nav_order: 2
---

# MainMenuButtons

This is the full list of what the API gives you.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>MainMenuButtons</code> <span>adds buttons and holds the controls</span></p>
<div class="api-pair" markdown="1">

```csharp
public static class MainMenuButtons
{
    public static bool Add(string id, string label, Action onClick);
    public static bool Add(string modId, string id, string label, Action onClick);
    public static void SetControlsEnabled(bool enabled);
    public static void SetControlsEnabled(string modId, bool enabled);
    public static bool IsOpen { get; }
}
```

| Method | Returns | What it does |
|---|---|---|
| `Add(string id, string label, Action onClick)` | `bool` | Adds a button with no mod id. Returns `true` when added. |
| `Add(string modId, string id, string label, Action onClick)` | `bool` | Adds a button for your mod. Returns `true` when added. |
| `SetControlsEnabled(bool enabled)` | `void` | Switches the menu's controls off or on, with no mod id. |
| `SetControlsEnabled(string modId, bool enabled)` | `void` | Switches the menu's controls off or on, counted for your mod. |

| Property | Type | What it means |
|---|---|---|
| `IsOpen` | `bool` | `true` when the main menu is up and has your buttons. |

</div>
</div>
