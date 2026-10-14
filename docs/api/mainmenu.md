---
title: MainMenu API
parent: API reference
nav_order: 44
---

# MainMenu API

Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Files: `src/Api/MainMenuButtons.cs` (the API), `src/ModMenuMainMenu.cs` (the MODS button, which uses the API).

Everything not listed here is internal.

## MainMenuButtons

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

### Add

- Returns `false` when `id` or `label` is null or empty, when `onClick` is null, or when `id` was already added. Otherwise it stores the entry and returns `true`. Entries are never removed.
- Ids are global and the first entry keeps its id. A duplicate logs one warning that names the mod that added the id and the caller. The same mod adding its own id again gets `false` with no warning. The overload without `modId` has no owner, so its duplicates always warn and name "a mod without an id". Prefer the `modId` overload; it throws when `modId` is not a valid mod id.
- Safe to call from `Awake`. Nothing is built until the main menu exists.
- Each time the main menu scene loads (for example, when the player returns from a game), every entry is cloned again from a main menu button: the one whose persistent call is `LoadGame`, otherwise the first one that calls the menu. The clone is named `Mod:<id>` and shows `label` in the template's text. Localization components are removed from the clone.
- Layout: the first row button sits under the "unknown" grid cell (the left column of the grid, which is 3 columns by 2 rows). Before placing, every other `Mod:` button in the row (one this API did not create, such as co-op's "Mod:Multiplayer") marks its grid column as taken. Each API button takes the first column from "unknown" to the right that no taken column covers. When the number of children under the row parent changes, or the sum of the rounded x positions of the foreign `Mod:` buttons changes, the row is laid out again, so a foreign button that appears or moves later moves API buttons out of its column. Foreign buttons are never moved.
- Navigation: Left and Right move along the row by horizontal position, across API and foreign buttons. Down from "unknown" goes to the leftmost row button. Up from a row button goes to whichever of "unknown", "credits" and "quit" is nearest in horizontal position.
- Click: the callback runs inside a try/catch. A throw is logged to `BepInEx\logs\obenseuer.moddingframework.ui.log` as `main menu button <id> failed: <type>: <message>`, and the game is not affected.

### SetControlsEnabled

The `modId` overload counts holds per mod, so one mod's `true` does not release another mod's `false`. The `bool` overload counts under one shared key.

- Calls must be paired: each `false` needs one `true`. Holds are counted, so two `false` calls need two `true` calls before the controls come back.
- `false`: adds one to the hold count. When the count goes from 0 to 1, it saves the main menu navigation handler's `ControlsEnabled` (or `true` when no handler exists). While the count is above 0, the framework sets the handler's `ControlsEnabled` to false each frame, because the game turns its own controls back on when the handler is enabled again.
- `true`: takes one from the count and never goes below 0. When the count reaches 0, it restores the saved value and selects the button that was pressed last (if one was pressed). A `true` with no hold does nothing.
- The count resets to 0 when the main menu reloads (the scene loads again), so a hold never carries over to the next main menu. A `true` that comes after a reload does nothing.

### IsOpen

`true` when the main menu exists and this API's buttons are built on it. `false` before the menu exists, while it is loading, and when no main menu button could be copied.

### MODS

- MODS is `Add("Mods", "MODS", ...)`. Its GameObject is `Mod:Mods`.
- Its click does nothing, and logs `MODS not opened: input is locked or the input patches are not ready` once, while `ModMenuInput.Ready` is false. Otherwise it opens the Mods window and calls `SetControlsEnabled("obenseuer.moddingframework.ui", false)`. Once the window is closed, the framework calls `SetControlsEnabled(true)`, which selects MODS again. The window is the Full dock, so it covers the main menu.
- The id `Mods` is taken by whichever plugin registers it first. If another plugin takes it, MODS is not added, and the framework logs `MODS main menu button not added: id Mods is already taken` to the ui log.

### Threading and safety

- Main thread only.
- The navigation postfixes are on `MainMenuNavigationHelper.GetButtonAbove`, `GetButtonBelow`, `GetButtonOnTheLeft` and `GetButtonOnTheRight`, under the `obenseuer.moddingframework.ui` owner. A throw in one of them turns off this row's navigation for the session and is logged once (`main menu navigation disabled`).
- A failed build is logged once (`main menu buttons not added: ...`) and retried each second for the same menu. A failure outside the build stops the builds for that menu (`main menu buttons failed: ...`).
