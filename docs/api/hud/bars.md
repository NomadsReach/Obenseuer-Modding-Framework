---
title: Bars
parent: HUD
grand_parent: Screen and UI
nav_order: 3
---

# Bars

A bar is a strip at the top of the screen with a title above it and a fill, for a value such as an opponent's health. You use `Hud.Bar` to show or update it and `Hud.ClearBar` to remove it. The framework's own UI kit draws the bars, not a copy of a game control.

## Things to know

- `Bar(owner, title, fill)` shows a bar at the top centre of the screen, or updates the one this owner already has. `Bar(owner, title, fill, color)` does the same with a colour of your choice. Without a colour, the bar uses the framework's blue. <span class="tech">(The default is `UiTheme.Accent`.)</span>
- `title` is a line above the bar, and you can leave it out. Pass `null` or an empty string for no title. This is different from `Notify` and `ShowHint`, which need a title.
- `fill` runs from 0 (empty) to 1 (full). A number above 1 or below 0 is held at the edge. A new bar starts at its fill, and when you change it later the bar moves smoothly to the new value over 0.15 seconds.
- A `fill` that is not a number (`NaN`) is an error. <span class="tech">(It throws `ArgumentException`.)</span>
- Each owner has one bar, and at most four bars show at once. Bars stack from the top down in the order they were first shown, so the newest is at the bottom. An owner that already has a bar can always update it.
- `ClearBar(owner)` removes the owner's bar, and the bars below it move up. It gives back `false` when the owner has no bar.
- Bars hide when the game hides its HUD, and when a menu covers the screen. That is during `HideGameUi`, in a game menu such as the shop, while the pause menu is open, while the game's wait clock is up, and while a save is loading or being written. They come back when none of that is true, and they are not removed. The F12 screenshot without UI hides them too. <span class="tech">(The F12 screenshot hides the game's HUD while it captures, and the bars follow the HUD.)</span>
- Loading a new scene removes every bar, just as it removes mod hints. Call `Bar` again from your own `Events.SceneLoaded` handler if you still need one. <span class="tech">(A Single scene load removes every bar.)</span>
- Bars are drawn on their own layer above the game's HUD, and they take no clicks. The framework's computer overlay and mod windows draw above them. <span class="tech">(The overlay has sort order 400, below the framework's computer overlay (500) and mod windows (1000). The game's own HUD order is not recorded, so the bars may draw under a game canvas.)</span>
- `Bar` does not refuse during a save or load. A new scene load that follows removes the bar.

## Limits

- The first call that shows a bar creates its layer. It gives back `false` if the layer cannot be made.
- A fifth `Bar` for a new owner gives back `false`.
- Some game screens may still draw over the bars.
