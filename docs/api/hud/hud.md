---
title: Hud
parent: HUD
grand_parent: Screen and UI
nav_order: 2
---

# Hud

This is the full list of what the API gives you. `Hud` shows things on screen and hides parts of the game's own screen, and every call gives back `true` when it worked and `false` when it did not.

An `owner` is your mod's id. It is how the framework keeps your hides and bars apart from other mods' (see [Owner](../../glossary.md#owner) in the glossary). A call that cannot do its job, for example because the part of the game it needs is not loaded yet, gives back `false`. See [When you can call it](details.md#when-you-can-call-it) for the full rules.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Hud</code> <span>what you can show and hide on screen</span></p>

This is the class you start from. It shows notifications, subtitles, hints, notes, the tooltip and a bar, and it hides the crosshair or the game's HUD for your owner.

<div class="api-pair" markdown="1">

```csharp
public static class Hud
{
    public static bool Notify(string text);
    public static bool Notify(string title, string body);
    public static bool ShowSubtitle(string text, float fadeSeconds);
    public static bool ShowHint(string modId, string title, string[] lines);
    public static bool HideHint(string modId);
    public static bool ShowNote(string noteId);
    public static bool ShowCrosshair();
    public static bool HideCrosshair();
    public static bool HideCrosshair(string owner);
    public static bool ShowCrosshair(string owner);
    public static bool ShowTooltip(string title, string details);
    public static bool HideTooltip();
    public static bool HideGameUi(string owner);
    public static bool ShowGameUi(string owner);
    public static bool Bar(string owner, string title, float fill);
    public static bool Bar(string owner, string title, float fill, Color color);
    public static bool ClearBar(string owner);
}
```

| Method | Returns | What it does |
|---|---|---|
| `Notify(text)` | `bool` | Shows your text in the game's notification box, under the heading `Notice`. |
| `Notify(title, body)` | `bool` | Shows your title and body in the same box. |
| `ShowSubtitle(text, fadeSeconds)` | `bool` | Types a line at the bottom of the screen, then fades it. |
| `ShowHint(modId, title, lines)` | `bool` | Puts a hint in the card game's hint box. |
| `HideHint(modId)` | `bool` | Removes your mod's hint. |
| `ShowNote(noteId)` | `bool` | Opens the Notes page at a note the player already has. |
| `ShowCrosshair()` | `bool` | Shows the crosshair. It has no owner, so it is shared. |
| `HideCrosshair()` | `bool` | Hides the crosshair. It has no owner, so it is shared. |
| `HideCrosshair(owner)` | `bool` | Hides the crosshair for this owner. |
| `ShowCrosshair(owner)` | `bool` | Takes back this owner's hide. |
| `ShowTooltip(title, details)` | `bool` | Shows the game's tooltip box with your text. |
| `HideTooltip()` | `bool` | Hides the tooltip box. |
| `HideGameUi(owner)` | `bool` | Hides the whole HUD for this owner. |
| `ShowGameUi(owner)` | `bool` | Takes back this owner's hide. |
| `Bar(owner, title, fill)` | `bool` | Shows or updates this owner's bar. |
| `Bar(owner, title, fill, color)` | `bool` | Shows or updates this owner's bar, in your own colour. |
| `ClearBar(owner)` | `bool` | Removes this owner's bar. |

</div>
</div>

## Things to know

- `Notify(title, body)` uses your own heading, so say where the message comes from. Both `title` and `body` must be text, not empty. `Notify` plays the game's notification sound.
- The notification box has no icon, so `Notify` has no icon option. <span class="tech">(The game's box has no icon slot, and neither overload has an icon parameter.)</span>
- `ShowSubtitle` types your text at 20 characters a second, then fades it out over `fadeSeconds`. The total time on screen is the typing time plus `fadeSeconds`. Several subtitles stack on screen, oldest at the top. <span class="tech">(Before showing, the framework strips the Pixel Crushers Dialogue System RPG Maker codes with the game's own `UITools.StripRPGMakerCodes`. That removes `\,` (quarter pause), `\.` (full pause), `\^` (skip to end), `\>` (instant open) and `\<` (instant close). Other backslashes stay. The stack is sorted by start time.)</span>
- `ShowHint` puts your lines in the card game's hint box. The player can hide that box with the Hints key, which is K by default and can be changed in the game's Controls settings. <span class="tech">(The box is the game's `HintOverlay`, the rules box of the card game. The game's `Tutorial` class, the pop-up key-binding prompts, is a different system that mods cannot show yet.)</span>
- Each mod has one hint, and showing a new one replaces the old one. Each line is a plain paragraph, not a bullet. Write `{key:Action Name}` in a line to show the key the player has set for that action, as the game does.
- Only one hint shows at a time. The box shows the most important hint, and the newest when two are equal. All mod hints are equally important, so the newest mod hint wins. A game hint, such as the card game rules, takes over the same way when it is shown later. <span class="tech">(The overlay shows the hint with the highest priority, and among equals the newest. Mod hints all use priority 0.)</span>
- A hint stays until you hide it or the player loads a new scene. Loading a new scene hides every mod hint, so show it again from your `Events.SceneLoaded` handler if you still need it. `HideHint` gives back `false` if your mod never showed a hint. <span class="tech">(A Single scene load hides every mod hint.)</span>
- `ShowNote` opens the Notes page at a note the player already has. `noteId` must match exactly one note or book page the player has collected. If it matches none, or more than one, you get `false`. <span class="tech">(It matches `NoteInfo.Id`, which is the note's `id`, else its addressable path, else its title.)</span>
- `ShowCrosshair()` and `HideCrosshair()` switch the crosshair on and off, with no owner. The game itself never calls these two methods. <span class="tech">(`HideCrosshair()` holds one shared entry, and `ShowCrosshair()` releases it and shows the crosshair only when no owner hides it. A Single scene load drops every hide and shows the crosshair.)</span>
- `HideCrosshair(owner)` and `ShowCrosshair(owner)` count hides for each owner, so two mods do not undo each other. The crosshair comes back only when every owner has taken back all of its hides.
- `ShowTooltip` shows the game's tooltip box with your heading and text, exactly as you give it, so rich text such as `<b>` works. The game's own item tooltips make their heading bold, but yours is not wrapped in bold. An empty `details` hides the body of the box.
- `HideTooltip` hides the box, and it clears what the game thinks the cursor is hovering over. A game tooltip under the cursor stays hidden until the cursor moves onto something again.
- The tooltip is the game's one shared box. Your text stays until you call `HideTooltip` or the game hides the box. The box follows the mouse only while the player uses the mouse. With a controller it stays where it was last placed. This tooltip suits a mod's own screen where the player has a free cursor.
- `HideGameUi(owner)` hides the game's whole HUD, and `ShowGameUi(owner)` brings it back. Each owner counts its own hides, so two `HideGameUi` calls from one owner need two `ShowGameUi` calls. The HUD comes back when every owner's count is back at zero. `owner` must be a valid mod id. <span class="tech">(They call `GameUIController.HideGameUI` and `ShowGameUI`, which set the HUD canvas alpha.)</span>
- A `HideGameUi` that gives back `false` counts nothing, so it needs no matching `ShowGameUi`.
- A screenshot with F12 taken during a hide does not leave the HUD stuck. The game's no-UI screenshot hides the HUD itself and then shows it again only if it was visible. The framework cleans up after that when the last owner shows the HUD. <span class="tech">(`TakeScreenshot.ScreenshotNoUI` leaves an entry in the game's list. The framework records that object, and when the last owner shows the HUD it removes those entries.)</span>

## Limits

- A subtitle with no visible text shows nothing and gives back `false`. That is a text that is empty, or only spaces once the codes are removed.
- Two subtitles that start at the very same moment can clash, and the framework does not handle this. <span class="tech">(The game keys each line by `DateTime.Now.Ticks`, so two subtitles started in the same clock tick collide.)</span>
- Pressing the Hints key while a hint is showing hides all hints, and the game remembers that between sessions. A hint you show later stays hidden until the player presses the key again. The game has no hints option in its settings menu, so if a hint does not appear, tell the player to press the Hints key.
- `ShowHint` gives back `false` when the hint box is not loaded yet. Call it after the game scene has loaded, for example inside `Events.WhenReady`.
- `ShowNote` cannot show text of your own. The only way to show other text is to give the player a note, which saves it with the game, and this API does not give notes.
- `HideCrosshair(owner)` gives back `false` and counts nothing when the crosshair is not in the scene. `ShowCrosshair(owner)` gives back `false` for an owner with no hide. `owner` must be a valid mod id (1 to 64 letters, digits, `.`, `_` or `-`).
- `title` must not be empty, and a `null` `details` is an error. It gives back `false` when the game's tooltip is not in the scene. <span class="tech">(A null `details` throws `ArgumentException`. The box is the game's `ToolTip`.)</span>
- `HideTooltip` gives back `false` when the tooltip is not in the scene.
- The tooltip is one shared box, so the game can replace your text. When the player hovers over an item or other game tooltip, your text is replaced, and when that hover ends, the box hides.
- `ShowGameUi` gives back `false` for an owner with no hide. Both `HideGameUi` and `ShowGameUi` give back `false` when the game's HUD is not in the scene.
- Loading a new scene ends every hide, so hide again if you still need it. The game replaces its HUD controller then, so every owner is dropped and the HUD shows. Call `HideGameUi` again from your own `Events.SceneLoaded` handler. <span class="tech">(A Single scene load ends every hide.)</span>

## Mods menu settings

The Mods menu has a `Hints` section with two settings that control what mods may show. Both are on by default. They are the framework's own settings, so they do not change the game's own hints.

- `ShowModHints` shows hints from mods. It is separate from the game's own Hints setting, so turning one off leaves the other alone. While it is off, `ShowHint` gives back `false` and the hint does not show. <span class="tech">(`HudHints.Show` reads it, and the log line says the setting is off. A `GameHintBox` hint is still kept, and it appears when the player turns the setting back on.)</span>
- `ShowModPrompts` shows key prompts from mods. While it is off, `ShowPrompt` gives back `false` and the prompt does not show, and `PromptShowing` gives back `false`. <span class="tech">(`HudHints.Prompt` and `HudHints.PromptShowing` read it.)</span>

The Mods menu has a `UI` section with two settings for the New Game screen. Both are on by default.

- `NewGameRowsOnAllDifficulties` shows the rows a mod registered for the New Game screen on every difficulty, not only on Custom, where the game shows its own difficulty settings. Turn it off if the rows overlap the difficulty description. The framework's own rows follow the setting; the game's own setting rows still show only on Custom. <span class="tech">(`NewGameOptionsScreen.Keep` reads it, section `UI`, on by default.)</span>
- `ModPresetsInSelector` lists the difficulty presets mods registered in the game's own difficulty selector, after the game's own levels, so the player can pick one while starting a game. Turn it off to leave the selector with the game's own levels. See [Difficulty presets](difficulty-presets.md). <span class="tech">(`DifficultyModPresetsPatches` reads it, section `UI`, on by default. When the core plugin cannot be found the setting is not in the config and the listing stays on.)</span>
