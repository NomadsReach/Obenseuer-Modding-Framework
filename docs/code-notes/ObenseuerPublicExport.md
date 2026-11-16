# Obenseuer Modding Framework code notes

## `src/Ui/DesktopUi.cs:DesktopUi`

This initial uGUI desktop API provides the visual primitives used by the OS prototype. It uses the game's Unity UI components and does not include a browser engine. The prototype remains responsible for its own layout, content, assets, and application behavior.

## `src/Ui/DesktopUi.cs:DesktopDrag.OnDrag`

Pointer positions are converted into the window parent's local coordinates before applying the drag, so movement follows the canvas coordinate system.
