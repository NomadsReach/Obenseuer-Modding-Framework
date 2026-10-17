---
title: Shared assets
parent: Web UI
grand_parent: Screen and UI
nav_order: 8
---

# Shared assets

The WebUi module ships two shared style files in its `web\` folder, next to `ObenseuerCore.WebUi.dll`. Every view maps that folder at `https://obx-core.obx/`, so any page can link them. The `web\modmenu\` folder in it holds the Mods menu's own page. It can also be reached at `https://obx-core.obx/modmenu/` from any view, but it is not part of the API and may change between versions.

- **`https://obx-core.obx/obx-theme.css` gives your page the framework's look.** It has colours and sizes for light, and for dark under `:root[data-theme="dark"]`. It also switches to dark under `prefers-color-scheme: dark` unless the page sets `data-theme="light"`. The values cover surfaces, text, muted text, lines and controls, amber, red, the two brand colours, fonts, letter spacing, font sizes, spacing, rounded corners and motion. It also has ready-made classes: `.obx-brand`, `.obx-btn` (with `primary`, `danger`, `ghost`, `sm` and `block`), `.obx-tabs` and `.obx-tab`, `.obx-card`, `.obx-label`, `.obx-value`, `.obx-table`, `.obx-input`, `.obx-badge` and `.obx-seg`. It has no rules for plain elements and no game or OS chrome.
- **`https://obx-core.obx/obx-base.css` is an optional set of basic rules for the whole page.** It sets `box-sizing`, `html` and `body` (no scrolling, a see-through background, no text selection), the `[hidden]` rule, the focus outline, and the body transition. A page uses it by linking it. A page that does not link it keeps the browser's defaults.
- **In the default mode, a see-through page background shows black.** Each view is a separate popup window that paints black where the page draws nothing. So give `html` or `body` your own background colour if your page can run in the default mode. In composition mode the page is drawn onto the game window, so the game shows through where the page draws nothing. Windowed mode has not been checked.
- **The framework adds no Content-Security-Policy to your page.** So set your own with a `<meta http-equiv="Content-Security-Policy">` tag, as the framework's own Mods menu page does.
