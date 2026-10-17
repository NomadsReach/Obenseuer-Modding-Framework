---
title: Hud
parent: HUD
grand_parent: Screen and UI
nav_order: 2
---

# Hud

This is the full list of what the API gives you. In every code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

A call that cannot do its job, for example because the part of the game it needs is not loaded yet, gives back `false`. See [When you can call it](details.md#when-you-can-call-it) for the full rules.

An `owner` in these calls is your mod's id. It is how the framework keeps your hides and bars apart from other mods'. See [Owner](../../glossary.md#owner) in the glossary.

`Hud` shows things on screen and hides parts of the game's own screen. Every call gives back `true` when it worked and `false` when it did not.

```csharp
public static class Hud
{
    public static bool Notify(string text);                               // the game's notification box, with "Notice" as its heading
    public static bool Notify(string title, string body);                 // the same box, with your own heading
    public static bool ShowSubtitle(string text, float fadeSeconds);      // types a line at the bottom of the screen, then fades it
    public static bool ShowHint(string modId, string title, string[] lines);   // puts a hint in the card game's hint box
    public static bool HideHint(string modId);                            // removes your mod's hint
    public static bool ShowNote(string noteId);                           // opens the Notes page at a note the player already has
    public static bool ShowCrosshair();                                   // shows the crosshair (shared, no owner)
    public static bool HideCrosshair();                                   // hides the crosshair (shared, no owner)
    public static bool HideCrosshair(string owner);                       // hides the crosshair for this owner
    public static bool ShowCrosshair(string owner);                       // takes back this owner's hide
    public static bool ShowTooltip(string title, string details);         // shows the game's tooltip box with your text
    public static bool HideTooltip();                                     // hides the tooltip box
    public static bool HideGameUi(string owner);                          // hides the whole HUD for this owner
    public static bool ShowGameUi(string owner);                          // takes back this owner's hide
    public static bool Bar(string owner, string title, float fill);       // shows or updates this owner's bar
    public static bool Bar(string owner, string title, float fill, Color color);   // the same, with your own colour
    public static bool ClearBar(string owner);                            // removes this owner's bar
}
```

- **`Notify(text)` shows the game's notification box with the heading `Notice` and your text as the message.** It plays the game's notification sound.
- **`Notify(title, body)` shows the same box with your own heading.** Use the heading to say where the message comes from. Both `title` and `body` must be text, not empty.
- **The notification box has no icon, so `Notify` has no icon option.**
  - Technical: the game's box has no icon slot, and neither overload has an icon parameter.
- **`ShowSubtitle` types your text on screen, then fades it out.** It types 20 characters a second, then fades over `fadeSeconds`. The total time on screen is the typing time plus `fadeSeconds`. Several subtitles stack on screen, oldest at the top.
  - Technical: before showing, the framework strips the Pixel Crushers Dialogue System RPG Maker codes with the game's own `UITools.StripRPGMakerCodes`. That removes `\,` (quarter pause), `\.` (full pause), `\^` (skip to end), `\>` (instant open) and `\<` (instant close). Other backslashes stay. The stack is sorted by start time.
- **A subtitle with no visible text shows nothing and gives back `false`.** That is a text that is empty, or only spaces once the codes are removed.
- **Two subtitles that start at the very same moment can clash.** The framework does not handle this.
  - Technical: the game keys each line by `DateTime.Now.Ticks`, so two subtitles started in the same clock tick collide.
- **`ShowHint` puts your lines in the card game's hint box, and the player can hide that box with the Hints key.** The Hints key is K by default and can be changed in the game's Controls settings. Pressing it while a hint is showing hides all hints, and the game remembers that between sessions, so a hint you show later stays hidden until the player presses the key again. The game has no hints option in its settings menu, so if a hint does not appear, tell the player to press the Hints key.
  - Technical: the box is the game's `HintOverlay`, the rules box of the card game. The game's `Tutorial` class, the pop-up key-binding prompts, is a different system that mods cannot show yet.
- **Each mod has one hint, and showing a new one replaces the old one.** Each line is a plain paragraph, not a bullet. Write `{key:Action Name}` in a line to show the key the player has set for that action, as the game does.
- **Only one hint shows at a time.** The box shows the most important hint, and the newest when two are equal. All mod hints are equally important, so the newest mod hint wins. A game hint, such as the card game rules, takes over the same way when it is shown later.
  - Technical: the overlay shows the hint with the highest priority, and among equals the newest. Mod hints all use priority 0.
- **`ShowHint` gives back `false` when the hint box is not loaded yet.** Call it after the game scene has loaded, for example inside `Events.WhenReady`.
- **A hint stays until you hide it or the player loads a new scene.** Loading a new scene hides every mod hint. If you still need it, show it again from an `Events.SceneLoaded` handler. `HideHint` gives back `false` if your mod never showed a hint.
  - Technical: a Single scene load hides every mod hint.
- **`ShowNote` opens the Notes page at a note the player already has, but it cannot show text of your own.** `noteId` must match exactly one note or book page the player has collected. If it matches none, or more than one, you get `false`. The only way to show other text is to give the player a note, which saves it with the game, and this API does not give notes.
  - Technical: it matches `NoteInfo.Id`, which is the note's `id`, else its addressable path, else its title.
- **`ShowCrosshair()` and `HideCrosshair()` switch the crosshair on and off, with no owner.** The game itself never calls these two methods.
- **`HideCrosshair(owner)` and `ShowCrosshair(owner)` count hides for each owner, so two mods do not undo each other.** The crosshair comes back only when every owner has taken back all of its hides. `HideCrosshair(owner)` gives back `false` and counts nothing when the crosshair is not in the scene. `ShowCrosshair(owner)` gives back `false` for an owner with no hide. `owner` must be a valid mod id (1 to 64 letters, digits, `.`, `_` or `-`).
  - Technical: the no-owner `HideCrosshair()` holds one shared entry, and `ShowCrosshair()` releases it and shows the crosshair only when no owner hides it. A Single scene load drops every hide and shows the crosshair.
- **`ShowTooltip(title, details)` shows the game's tooltip box with your heading and text.** `title` must not be empty. It is shown exactly as you give it, so rich text such as `<b>` works. The game's own item tooltips make their heading bold, but yours is not wrapped in bold. An empty `details` hides the body of the box. A `null` `details` is an error. It gives back `false` when the game's tooltip is not in the scene.
  - Technical: a null `details` throws `ArgumentException`. The box is the game's `ToolTip`.
- **`HideTooltip` hides the box.** It also clears what the game thinks the cursor is hovering over, so a game tooltip that is under the cursor stays hidden until the cursor moves onto something again. It gives back `false` when the tooltip is not in the scene.
- **The tooltip is the game's one shared box, so the game can replace your text.** When the player hovers over an item or other game tooltip, your text is replaced, and when that hover ends, the box hides. Your text stays until you call `HideTooltip` or the game hides the box. The box follows the mouse only while the player uses the mouse. With a controller it stays where it was last placed. This tooltip suits a mod's own screen where the player has a free cursor.
- **`HideGameUi(owner)` hides the game's whole HUD, and `ShowGameUi(owner)` brings it back.** Each owner counts its own hides: two `HideGameUi` calls from one owner need two `ShowGameUi` calls. The HUD comes back when every owner's count is back at zero. `owner` must be a valid mod id. `ShowGameUi` gives back `false` for an owner with no hide. Both give back `false` when the game's HUD is not in the scene. A `HideGameUi` that gives back `false` counts nothing, so it needs no matching `ShowGameUi`.
  - Technical: they call `GameUIController.HideGameUI` and `ShowGameUI`, which set the HUD canvas alpha.
- **Loading a new scene ends every hide, so hide again if you still need it.** The game replaces its HUD controller then, so every owner is dropped and the HUD shows. Call `HideGameUi` again from your own `Events.SceneLoaded` handler.
  - Technical: a Single scene load ends every hide.
- **A screenshot with F12 taken during a hide does not leave the HUD stuck.** The game's no-UI screenshot hides the HUD itself and then shows it again only if it was visible. The framework cleans up after that when the last owner shows the HUD.
  - Technical: `TakeScreenshot.ScreenshotNoUI` leaves an entry in the game's list. The framework records that object, and when the last owner shows the HUD it removes those entries.
