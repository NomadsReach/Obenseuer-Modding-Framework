---
title: Common tasks
parent: Settings
grand_parent: Framework basics
nav_order: 1
---

# Common tasks

Tell the player which key to press. `Label` gives the text for the device in use, so a controller player sees a button name.

```csharp
Hud.ShowHint(Mod.Id, "Door", new[] { "Press [" + Keybinds.Label("Interact") + "] to open." });
```

Find out whether the player runs in toggle or hold mode. `true` means toggle (press once to start, press again to stop).

```csharp
bool toggleSprint = GameSettings.ToggleSprint();
```

Change the field of view without touching the player's saved setting. The value is only in memory, and `ClearFov` hands the camera back.

```csharp
GameSettings.SetFov(Mod.Id, 90);
GameSettings.ClearFov(Mod.Id);
```
