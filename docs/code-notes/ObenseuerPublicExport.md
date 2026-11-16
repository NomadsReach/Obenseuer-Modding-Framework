# Obenseuer Modding Framework code notes

## `src/Ui/DesktopUi.cs:DesktopUi`

This initial uGUI desktop API provides the visual primitives used by the OS prototype. It uses the game's Unity UI components and does not include a browser engine. The prototype remains responsible for its own layout, content, assets, and application behavior.

## `src/Ui/DesktopUi.cs:DesktopDrag.OnDrag`

Pointer positions are converted into the window parent's local coordinates before applying the drag, so movement follows the canvas coordinate system.
# Current game power grid

- `src/Api/BuildingAreas.cs:Building.Snapshot` reads the current manager's grid balance when a grid exists; a manager without a grid reports zero.
- `src/Api/PowerDevices.cs:PowerDevices.Components` and `PowerDevices.ToInfo` use the current game build's grid listeners for furniture. A listener is powered when switched on and either has no grid, has unlimited supply, or is powered by its grid.
- `src/Api/PowerDevicesPatches.cs:PowerDevicesPatches.InstallFurniture` observes grid power callbacks; it does not report every manual switch change. Fuel and battery hooks retain their own feature owners.
- `src/Ui/DesktopUi.cs:DesktopUi.MakeSelectable` adds desktop-style hover and selection feedback to a tile. A double-click invokes its optional activation callback; selection follows Unity's current event system.
- `src/Ui/ToolkitUi.cs:ToolkitUi.CreateOverlay` creates a UI Toolkit screen overlay owned by the supplied host. The panel and its settings follow the host lifecycle. This first API targets a full-screen overlay; in-world rendering, input coordination, and behavior alongside other panels require game validation.
- `src/ComputerApps.cs:ComputerApps.SetCloseGuard` lets an app veto standalone close-button, Escape, and replacement-open requests. Programmatic close, scene changes, and unload remain forced; the guard does not intercept leaving a computer desk. An exception in a guard refuses the user request and is logged.
