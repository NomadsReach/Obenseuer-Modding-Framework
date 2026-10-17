---
title: ModMenu
parent: Framework core classes
grand_parent: Framework basics
nav_order: 13
---

# ModMenu

The Mods menu is the framework's menu for mods. It is a page the player opens from the pause menu or the main menu. Every mod with settings gets a Settings page there for free. `ModMenu` lets your mod add a page of its own. The page's contents are built by your code, with the `ModUi` building blocks described on the [UI kit](../api/ui.md) page.

```csharp
public static class ModMenu
{
    // Add a page. build gets the empty area to fill; it runs each time the page is opened.
    public static void AddPage(string modId, string title, Action<RectTransform> build);
    // Older helper for building rows on a page; null when content is not a page's build area.
    public static ModMenuWidgets WidgetsFor(RectTransform content);
}
```

- **`ModMenuWidgets` is out of date, but still works.** It is marked `[Obsolete("Use ObenseuerCore.Ui.ModUi")]`. New pages should use `ModUi`. A mod that still uses it gets compiler warning CS0618.
- **The Mods menu opens as a web page by default.** That is `[UI] ModMenuStyle = web`. Each plugin with visible config entries gets one "Settings" page. A page you add with `AddPage` appears under your mod id, or as a new entry named after your page's title when no plugin has that mod id.
- **The older window opens when `[UI] ModMenuStyle` is `classic`, or when the web page cannot start.** If the web page does not answer within 8 seconds twice in a row, the menu stays classic until the game restarts.
- **The framework has a Settings page like any plugin, and it also carries actions.** Its `Scene looks` section has a `Run scene looks checks` button. Clicking it runs the check for the loaded scene and shows the result on the menu status line. The classic window shows the same button under the framework's group, with the result next to it. The action needs no `[Debug]` flag.

**How the classic Mods window works.**

- **It is one window with a list of pages on the left and the selected page on the right.** The list starts with "Mod Settings" (the framework's config editor), then every added page sorted by title. `AddPage` only adds a line to this list. It never adds a tab to the game's own menu. The list is read when the window opens, so a page you add while the window is open shows up the next time it opens.
  - Technical: it is one `ModWindow` in the Full dock, used because the Center dock (640 px) cannot hold the sidebar and the settings rows. The `Side` column is the list and `Body` shows the selected page.
- **In the game, the Mods tab is one entry in the pause menu.** Its button opens the window, and arriving at the tab opens it too. Closing the window while the tab is selected returns the pause menu to the page it came from. Two mods never get the same tab id.
  - Technical: the tab is one entry in `UIPageButtons.MenuPages`. Its id is the first unused `MenuPageNames` value above `MenuPageNames.Items`, checked against `MenuPages`.
- **On the main menu, a "MODS" button sits under the main menu grid, in the row below "???".** It opens the same window.
- **The tab is built once the game's menus exist, and built again when the game recreates them.** If a menu's tab failed to build, it is not tried again for that menu. There is no way to remove a page while the game runs.
  - Technical: it is built once `UIPageButtons` and `PauseMenu` exist.
- **The pad shoulder buttons do not reach the Mods tab, but mouse and click do.** The game cycles its menu tabs with a fixed list of six names (`_tabs`), and the framework does not change that list.
- **Esc closes the window, and so does Joystick1Button1.** The exception is when a text field has focus or a key row is waiting for a key: then that field or row uses the press, and the window stays open.
  - Technical: Esc is the game's "Pause Menu" key.

**Your pages.**

- **Your `build` function gets an empty area and fills it each time the page opens.** The area is cleared before each build, so rows are built once for every time the page is opened.
- **If `build` throws, the page shows a notice and the window stays open.** The notice says "This page failed to load. Details are in BepInEx\logs\obenseuer.moddingframework.ui.log", and the error is logged there. The other pages still work.
- **If a control's callback throws, the window closes.** Controls from `ModUi` run their callbacks under `ModWindow.Run`, and a callback that throws closes the window with the reason `Fault`. This is the rule for every window built with the kit, not just for pages.

**The Mod Settings page.**

- **It lists every setting of every loaded plugin.** Settings are grouped by plugin (the first group starts open, the others closed) and then by section. Rows are sorted by section and key.
  - Technical: a setting here is a BepInEx `ConfigEntry`. Entries that ConfigurationManager marks `Browsable = false` are not listed.
- **A search box at the top filters the rows by plugin, section or key.** While a search is active, every group is open.
- **Each kind of setting gets a fitting control.** A yes-or-no is a switch (`ModUi.Toggle`). A choice from a list is a stepper (`ModUi.Stepper`), and so is an enum or a list of acceptable values (`AcceptableValueList<T>`). A number with a range is a slider (`ModUi.Slider`). A keyboard shortcut is a key binding (`ModUi.KeyBinding`): click it, then press a key. Ctrl, Shift and Alt held at the press are kept. Esc cancels, and mouse buttons are ignored. Text, and any other type that can be read from text, is a text box (`ModUi.TextField`). A `[Flags]` enum is a text box too. Other types are shown but cannot be changed.
- **Every changeable row has a Reset button, and its description shows as a tooltip on hover.**
- **Changes are saved to the config file as you make them.** Switches, steppers, key bindings and text boxes save at once. A text box writes when you finish editing (Enter, or a click away), not on each key. Sliders write at once and save 0.25 seconds after the last change. Quitting to the desktop from the pause menu or the main menu saves a change that is still waiting.

**Typing and the inventory.**

- **While you type in a box or set a key, the game's controls are locked.** The game's keys and its controller update are skipped. The lock is released when the box loses focus or the capture ends.
- **Your custom page never reaches the inventory.** While one is selected, the inventory's tab switch and its per-frame input (Drop, Move Item, Use) do nothing.

**Dark theme for the in-game config window.** The setting `[UI] ThemeConfigManager = true` gives the ConfigurationManager (F1) window a dark theme. It takes effect on the next start. Without ConfigurationManager, nothing is changed.

- Technical: the framework patches the private `OnGUI` of ConfigurationManager through `CorePatcher`. A prefix swaps `GUI.skin` for a dark skin built from the menu palette, and a finalizer puts back the previous skin.

**Still to be checked in the game.**

- The in-game Mods tab: the window opens on arrival, closing it returns to the previous page, and the pause menu does not react to keys while the window is open.
- The main menu MODS button: its place under the grid, the click, left and right navigation to Multiplayer, and the navigation restore on close.
- Esc and Joystick1Button1 closing, Esc inside a text box and inside a key capture, and that the Pause Menu does not open on the same Esc press.
- Search, grouping, every kind of row, the 0.25 second save, and Reset.
- The page failure notice, and a throwing callback closing the window with the reason `Fault`.

This plugin adds a page with one line of text. `ModUi.Text` returns a `TextMeshProUGUI`, so your project must reference `Unity.TextMeshPro.dll` and `UnityEngine.UI.dll` from `Obenseuer_Data\Managed`, as the [UI kit](../api/ui.md) page explains.

```csharp
using BepInEx;
using BepInEx.Unity.Mono;
using ObenseuerCore;
using ObenseuerCore.Ui;

public class HelloPage : BaseUnityPlugin
{
    private void Awake()
    {
        ModMenu.AddPage("example.hellopage", "Hello", host => ModUi.Text(host, "Hello from my page.", false));
    }
}
```
