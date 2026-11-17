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
- `src/CloseRequestGate.cs:CloseRequestGate.Check` blocks nested close checks and releases the gate even if a callback throws. `src/ComputerApps.cs:ComputerApps.CanCloseOverlay` also rejects a request if a callback changes the active overlay.
- `src/Ui/ToolkitUi.cs:ToolkitUi.MakeDraggable` focuses a Toolkit window on pointer down and drags it from its title bar while leaving button clicks alone. `src/Ui/ToolkitWindowBounds.cs:ToolkitWindowBounds` keeps part of the title bar visible, matching the desktop's existing limits.

- `src/Ui/ToolkitUi.cs:ToolkitUi.MakeResizable`, `src/Ui/ToolkitResizeHandle.cs:ToolkitResizeHandle` - The caller supplies the visual grip and minimum dimensions. The returned handle owns pointer callbacks and capture; cancel releases an active drag and disposal unregisters callbacks. Pointer deltas use the window parent's local coordinates. `src/Ui/ToolkitWindowBounds.cs:ToolkitWindowBounds.Resize` clamps sizes. Real Unity input, scaling, and layout need in-game verification.
- `checks/ToolkitResize/Program.cs:Program.Main` - The public check runs the resize handle against UI stubs for bounds, capture, a disabled guard, reparenting, and disposal. The stubs do not model Unity transforms or event propagation.
- `src/Ui/UiAppearance.cs:UiAppearance` owns the framework theme preference and dispatches setting changes from the main update loop. `src/Ui/UiAppearanceRules.cs:Initial` prefers the new core key, then the WebUi key, then the legacy core key. `webui/src/WebUi.cs:OnAppearanceChanged` synchronizes the legacy WebUi setting and updates existing web views.
- `src/Ui/ToolkitUi.cs:ToolkitUi.CreatePinnedOverlay` owns a screen-space panel under the caller's host and positions its child using projected target corners. `src/Ui/ToolkitPinnedBounds.cs:Project` follows `webui/src/WebView.cs:Project` for an inside rectangle and rejects off-camera or undersized screens. `checks/ToolkitWindow/Program.cs:Program.Main` checks projection bounds; Unity scaling, input routing, and camera motion require in-game verification.
- `src/Ui/ToolkitUi.cs:ToolkitPanel.Create` acquires the framework UI event system for a pinned panel; `ToolkitPanel.Release` releases it on host destruction or a failed creation.

- `src/ComputerApps.cs:ComputerApps.OpenPinned` owns a registered app host without acquiring the standalone camera control lock. `ComputerApps.PinnedScreen` supplies its projection target during app construction; `ComputerApps.RequestClose` respects its close guards. The caller owns camera movement, controls and exit input. Target loss and scene unload force cleanup. Runtime behavior requires in-game verification.

- `src/Ui/ModWindow.cs:UiEvents.Acquire` counts a user only after EventSystem setup succeeds so a failed acquisition does not strand the shared count.

- `src/ComputerApps.cs:OpenPinned` rejects a busy game before opening a new pinned host, and checks again after closing an existing overlay because its control lock may have masked other game UI.

- `checks/ComputerApps/Program.cs:Main` runs `src/ComputerApps.cs:OpenPinned` and `RequestClose` against a fake Unity object tree in public CI, covering busy state, guard veto and reentrancy, target deactivation, scene transitions, and construction failure. It does not validate Unity rendering, input, or real object lifecycle.

- `src/ComputerAppsDesk.cs:Show` brings an already-active host forward without clearing its children or calling the app builder again. Inactive hosts are still cleared before rebuilding. Desk use, visual stacking and close behavior need in-game verification.
