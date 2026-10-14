---
title: Obenseuer Modding Framework computer desk API (ComputerDesk)
parent: API reference
nav_order: 116
---

# Obenseuer Modding Framework computer desk API (ComputerDesk)

Experimental.
Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Log owner: `obenseuer.moddingframework.computerdesk`. Logs go to `BepInEx\logs\obenseuer.moddingframework.computerdesk.log`.
Files: `src/Api/ComputerDesk.cs`. The lookup is in `src/Api/ComputerFiles.cs`.

Everything not listed here is internal. Main thread only.

Scope: the desktop of a computer in the active scene. Reading its files and emails is in `ComputerContents`. Desk apps are in `ComputerApps`.

## Rules

- **Wallpaper.** `SetWallpaper` changes the desktop picture of the computer's screen right away. It is not saved. The computer sets its own wallpaper in its `Start`, which runs after `Events.SceneLoaded`, so a call made in that handler is overwritten. Call `Events.WhenReady` from your `Events.SceneLoaded` handler, so the wallpaper is set again after every load.
- **Owner.** `SetWallpaper` takes the mod id.
- **Refusals.** It returns `false` for an empty computer id, a `null` sprite, an unknown or unloaded computer, or an invalid mod id.
- **Files and emails.** The framework does not add files or emails to a computer. The game's calls for that replace the list and build a row for every entry, so a second call shows the old rows twice.

## API

```csharp
public static class ComputerDesk
{
    public static bool SetWallpaper(string modId, string computerId, Sprite wallpaper);
}
```

The computer id is the one `ComputerContents.All()` or `ComputerContents.IdOf` returns.

## Example

```csharp
string id = ComputerContents.IdOf(computerObject);
if (id != null) ComputerDesk.SetWallpaper("MyMod", id, wallpaperSprite);
```

## Not wrapped

- **Emails and files.** `ComputerScreen.AddEmails` and `ComputerScreen.AddRecycleBinFiles` are one-time setup calls from the computer's `Start`. Calling them again duplicates the rows, so the framework does not wrap them.
- **Opening a file.** `ComputerScreen.OpenTextFile` opens a game panel and needs the game's note asset, which the framework cannot look up by id.
