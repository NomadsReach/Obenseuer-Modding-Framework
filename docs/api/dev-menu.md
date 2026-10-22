---
title: Developer menu
parent: Framework basics
grand_parent: API reference
nav_order: 3
---

# Developer menu

Status: experimental.

## What it is

The Developer menu is one window for testing a mod in a game save. It lists your quests and lets you restart one, rewind it to an objective, set a counter, finish or fail an objective, or remove it. It also has state tools, such as global flags, money, items, the clock and teleports. Your own mod can add a section to the window, and a live value to a small overlay.

It is off by default. When it is off, the framework builds no window, no key handler and no overlay, and it does not draw your sections. Turn it on with `[Debug] DeveloperMenu = true`. Then press F8, or type `omf_dev` in the game's developer console. You can change the key under Mod Keybinds in the Mods menu.

## When to use it

- Test chapter six of a long quest without playing chapters one to five again.
- Put a quest back to its start after a bad test, without a backup save.
- Remove a quest from a save so `QuestAuthoring.Create` accepts the same key again.
- Add a button that starts one of your test scenes, or a value you want to watch while you play.

## Quick example

This framework mod adds a section with one button and a watch that shows a counter. Both only appear while `[Debug] DeveloperMenu` is on.

```csharp
using System;
using ObenseuerCore;
using ObenseuerCore.Ui;
using UnityEngine;

[ModInfo("example.devtest", "Dev test", "0.1.0")]
public class DevTest : FrameworkMod
{
    private int taps;

    protected override void OnLoad()
    {
        DevMenu.AddWatch("example.devtest", "Taps", () => taps.ToString());
        DevMenu.AddSection("example.devtest", "Dev test", card =>
        {
            ModUi.Button(card, "Count a tap", ButtonStyle.Secondary, () => taps++);
        });
    }
}
```

1. Set `DeveloperMenu = true` in `BepInEx\config\obenseuer.moddingframework.cfg`, under `[Debug]`, and start the game.
2. Load a save and press F8. Open the Mods tab.
3. Press `Count a tap`. The `Taps` watch goes up. Press the Watches button at the bottom to see it live.

The sections and watches are not saved. Your mod adds them again each time the game starts, which is what `OnLoad` does.

## Reference

<div class="api-type" markdown="1">
<p class="api-type-head"><code>DevMenu</code> <span>adds your sections and watches to the Developer menu</span></p>

Use `AddSection` to add a block to the menu's Mods tab, and `AddWatch` to add a value to the Watches overlay.

<div class="api-pair" markdown="1">

```csharp
public static class DevMenu
{
    public static bool AddSection(string modId, string title, Action<RectTransform> build);
    public static bool AddWatch(string modId, string name, Func<string> value);
    public static bool SaveIsDevModified { get; }
}
```

| Method | Returns | What it does |
|---|---|---|
| `AddSection(modId, title, build)` | `bool` | Adds a card named `title` to the Mods tab. `build` gets the card and adds your controls to it. Calling it again with the same `modId` and `title` replaces the first one. |
| `AddWatch(modId, name, value)` | `bool` | Adds a line `name: value` to the Watches overlay. `value` is called about four times a second while the overlay is on. Calling it again with the same `modId` and `name` replaces the first one. |

| Property | Type | What it means |
|---|---|---|
| `SaveIsDevModified` | `bool` | `true` once the Developer menu has changed the state of the loaded save. See [Things to know](#things-to-know). |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>DevMenu.AddSection</code> <span>a block of your own controls</span></p>

<div class="api-pair" markdown="1">

| Argument | Type | What it means |
|---|---|---|
| `modId` | `string` | Your mod id. It must be a valid mod id. |
| `title` | `string` | The card's heading. Up to 64 characters, not blank. |
| `build` | `Action<RectTransform>` | Called each time the window is built. Add buttons and fields to the card it gets. It must not be `null`. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>DevMenu.AddWatch</code> <span>a live value for the overlay</span></p>

<div class="api-pair" markdown="1">

| Argument | Type | What it means |
|---|---|---|
| `modId` | `string` | Your mod id. It must be a valid mod id. |
| `name` | `string` | The label shown before the value. Up to 64 characters, not blank. |
| `value` | `Func<string>` | Returns the text to show. It must not be `null`. If it throws, the overlay shows `error` and the framework logs the exception once. |

</div>
</div>

## What the window does

- **Refusals.** `AddSection` and `AddWatch` return `false` for a bad argument, and log the reason once. A refusal never throws.
- **Quests tab.** Lists your quests from `QuestAuthoring`, grouped by the mod id in the task id. Each quest has Restart, Reset, Remove, Capture and Restore. Each objective has Jump here, Finish and Fail, and a counter field when it has a target. Each button calls the matching `QuestAuthoring` method (`Restart`, `Reset`, `Remove`, `Checkpoint`, `JumpTo`, `SetObjective` or `SetCounter`). The log gets one line for each click, saying whether it was done or refused. See [Rewinding a quest](quest-authoring.md#rewinding-a-quest).
- **Capture and Restore.** These keep one named state per quest, called `devmenu`, in memory. A new capture replaces the one before it.
- **State tab.** Set the time, set or clear the clock's own speed (`TimeControl.SetTimeScale` and `ClearTimeScale`, 0.01 to 100, 1 is normal), fire the framework's `HourPassed` and `DayPassed` events on demand, give money in OC, give an item by id, by part of its name or by `mod:<id>` (the items that mod defined), toggle global flags by key, and teleport to an entrypoint or to a spot you type in as X, Y and Z. It also has the player's health, god mode, no clip and heal, the item stacks a missing mod left behind (remove them per mod), relationship dispositions, and the NPCs a mod spawned (despawn them, move one to your position, go to one, or spawn one at your position). A last card sets any `Survival` stat (hunger, thirst, hygiene, tiredness and the rest) and any skill level (`Skills`).
- **Mods tab.** Shows the watch values, the sections that mods have added, and the framework mods that loaded, with any that failed to load. A section that throws shows a short message, and its mod's log gets the error.
- **Tools tab.** Runs the built-in checks `core_quests_check`, `core_savedata_check` and `scenelooks here` and shows the result. It also reads a mod's log: type part of the mod id, press a log name, and the last 40 lines show. It shows how many lines the log rate limiter has dropped this session.
- **Dev-modified line.** A line at the top of the window says the loaded save is dev-modified, and it appears the moment an action of the menu flags that save. The flag belongs to the save, so a save that was never changed by the menu shows no line.
- **Logging.** Each action writes one line with the source `DevMenu` to `BepInEx\logs\DevMenu.log`, saying what was done and whether it was done or refused.

## Things to know

- **A moved NPC.** `ModNpcs.MoveTo` changes where the loaded NPC stands, not its saved spawn position. The move lasts until the save loads again, and a saved NPC then goes back to its spawn position.
- **Time only goes forward.** The game keeps one day counter that only moves up (`TimeOfDayAzure.IncreaseDayCounter` raises the day-changed event and the achievement checks), and the hour timeline is one value inside a day. An earlier time would fire the minute and hour events again, so the framework refuses it: `WorldClock.SetTimeOfDay` is forward only. Start from a scratch save if you need an earlier time.
- **A fired event is not a clock tick.** `Fire HourPassed` and `Fire DayPassed` call `Events.HourPassed` and `Events.DayPassed` with the hour (`WorldClock.Now.Hour`) and the day (`WorldClock.Now.Day`) the clock is showing and change nothing else, so a handler that reads the world still sees the same time. They are refused with no loaded save, and they do not set the hour the framework's own tick remembers, so the next real hour still fires as usual.
- **A clock speed set here is not saved.** The speed belongs to `TimeControl` and is dropped on a save load or a return to the main menu, like any other mod's speed. `Clear time scale` gives the framework's own claim back; `TimeControl.ClearTimeScale` returns `false` when nothing was set, and the log line then says refused.
- **Teleport to coordinates.** The X, Y and Z fields start at the player's position and are read as invariant numbers; a blank, a `NaN` or an infinity is refused and logged. `PlayerState.Teleport` needs a transform, so the menu moves one empty, hidden carrier object to the spot and hands that to it. The spot is not snapped to the ground or the navmesh, and the player lands there even if it is inside geometry, so type a height near the ground you can see.
- **Go to NPC.** The button puts the player on the NPC's own spot. `ModNpcs.List` prunes every NPC whose object is gone, so only the NPCs loaded in the current scene get a row, and one that unloaded after the page was built is refused and logged. The player keeps its own size, because the move uses the same empty carrier object as Teleport to coordinates rather than the NPC's own transform. The spot is not snapped to the ground or the navmesh, so the player can land inside the NPC.
- **No clip.** The **No clip** toggle on the Player card, next to **God mode**, turns the game's own debug movement on and off, so the player walks through walls and locked doors and can leave the ground. WASD moves the player, Left Shift moves faster, and the game's own Up and Down actions go up and down. The game sets no default key for those two actions, so bind them under Key bindings if nothing happens. A change that succeeds flags the save as dev-modified, like every other action of the menu's own cards. The toggle is refused, with one line in the log, when the game's debug movement or the game's developer console is not loaded in the scene, and it is put back to the state the game reports. The game's `noclip` console command is kept in step with the toggle, so the command turns no clip off after the menu turned it on, and the other way round. Changing the scene switches no clip off: the game's debug movement belongs to the level scene, so it goes away with it, and the menu puts the game's console state back, then says so in one line in the log, without flagging the save as dev-modified again. The toggle is put back to the state the game reports, so it is never left showing a value from the level that unloaded. A scene the game loads on top of the current one, which is how it streams the neighbours of an open level, is not a change of scene and leaves no clip alone. This scene change path was
- **Stats and skills.** One value field for the stats and one for the skills; each button names the value that stat or skill has right now. `Survival.Set` keeps a stat inside its own range (hunger -100 to 100, radiation and sauna heat have no top, the rest 0 to 100), so a value outside it lands on the nearest end. Skills have no setter in the framework: the card adds the difference with `Skills.Add`, which lowers a skill for a negative amount. The game clamps the amount to the top and to 0, so a level outside the range lands on the nearest end, and it reports a refusal only when the skill already sits at `Skills.MaxLevel`. A value that is not a plain number is refused and logged.
- **Not in the menu.** Going back in time (see above). **Test snapshots** are not included: a snapshot would copy the player's save, and no copy-and-restore path that leaves the real saves alone was checked, so the menu does not offer one.
- **From and To on a rewind.** The Current objective line on a quest card, and the `From` and `To` on `Quests.Rewound`, name the objective that was `Active` before the call and the one that is `Active` after it. When a quest has two objectives active at once, both report the first one in the quest's objective list, so a rewind that moves between parallel objectives reads as standing still. See [Rewinding a quest](quest-authoring.md#rewinding-a-quest).
- **A section that throws in a button.** A button callback from your section that throws closes the Developer menu window, the same as any framework window. The framework logs the error under its own log.
- **Scene and save.** The menu's actions change the live save. Use a scratch save while you test.
- **A dev-modified save.** Every action of the menu's own cards that succeeds flags the loaded save, so a bug report from that save can say it was changed. The flag is kept in the framework's own file in the save folder, `mods\obenseuer.moddingframework.devmodified.json`, and is written with the next normal game save, so quitting without saving drops it. It never clears itself. `DevMenu.SaveIsDevModified` reads it, and one warning goes to the log the first time the flag is set or read in a session. While `[Debug] DeveloperMenu` is off, the flag is never bound to the save, so `DevMenu.SaveIsDevModified` gives back `false` even for a save whose `mods\obenseuer.moddingframework.devmodified.json` says it was changed. Browsing the menu, the checks on the Tools tab, and the buttons in a mod's own section do not flag the save, because they do not run through the menu's own action call.
- **Locks the game.** The window holds the mouse cursor and stops player movement while it is open, the same as the Mods menu. The watch overlay does not hold the game.
- **How many you can add.** At most 200 sections and 200 watches in total, across all mods. Adding one when the list is full returns `false` and logs the reason; replacing a section or watch you added before still works at the cap.
