---
title: Computer desk
parent: Phones, mail and media
grand_parent: API reference
nav_order: 2
---

# Computer desk

Status: experimental.

## What it is

The computers in the game show a desktop with a picture behind the icons, the wallpaper. This API lets your mod change that picture on one computer. Reading a computer's files and emails is on the [Computer files](computerfiles.md) page, and adding apps to the desktop is a separate API, `ComputerApps`.

## When to use it

- Give a computer in your mod's area its own wallpaper.
- Change the wallpaper when something happens in the story, for example after a hack.

## Quick example

This mod paints every computer in the scene blue. It does this after each save loads, because the game sets its own wallpaper every time a scene loads.

```csharp
using ObenseuerCore;
using UnityEngine;

[ModInfo("example.bluedesktop", "Blue Desktop", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class BlueDesktop : FrameworkMod
{
    private Sprite wallpaper;

    protected override void OnLoad()
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, new Color(0.1f, 0.2f, 0.6f));
        texture.Apply();
        wallpaper = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));

        Events.SceneLoaded += scene => Events.WhenReady(Paint);
    }

    private void Paint()
    {
        foreach (ComputerInfo computer in ComputerContents.All())
            ComputerDesk.SetWallpaper(Mod.Id, computer.Id, wallpaper);
    }
}
```

`SceneLoaded` is an event: the framework calls your code each time the game loads a scene. `WhenReady` runs your code once a save is loaded. See [Using events](../guides/getting-started/using-events.md) for more on both.

## Common tasks

Change the wallpaper of one computer you already have, for example the one the player is using. `IdOf` gives you the computer's id from its game object, or null when the object is not a computer.

```csharp
string id = ComputerContents.IdOf(computerObject);
if (id != null) ComputerDesk.SetWallpaper(Mod.Id, id, wallpaperSprite);
```

## Reference

This is the full list of what the API gives you. Text in brackets at the end of a note is extra detail for experienced modders, and you can skip it on a first read.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>ComputerDesk</code> <span>changes a computer's wallpaper</span></p>
<div class="api-pair" markdown="1">

```csharp
public static class ComputerDesk
{
    public static bool SetWallpaper(string modId, string computerId, Sprite wallpaper);
}
```

| Method | Returns | What it does |
|---|---|---|
| `SetWallpaper(string modId, string computerId, Sprite wallpaper)` | `bool` | Changes the wallpaper of one computer right away. `modId` is your mod's id. `computerId` comes from `ComputerContents.All()` or `ComputerContents.IdOf`. Returns `true` when the wallpaper was changed, `false` when it was not. |

</div>
</div>

### Things to know

- The wallpaper changes on the screen straight away, and it is not saved. When the player loads a save or the scene loads again, the computer goes back to its own wallpaper. Set it again after each load. <span class="tech">(the computer sets its wallpaper from its own saved picture in `InteractableComputer.Start`, which runs after `Events.SceneLoaded` is raised; a call made inside a `SceneLoaded` handler would be overwritten; calling `Events.WhenReady` from that handler makes the wallpaper be set again after every load)</span>
- `SetWallpaper` needs your mod's id. Pass the id of your mod, such as `Mod.Id`.

### Limits

- An id that is empty, longer than 64 characters, or has characters other than letters, digits, `.`, `_` and `-` makes `SetWallpaper` return `false` and writes one line to the log.
- `SetWallpaper` returns `false` when it cannot do the change. That happens for an empty computer id, a `null` picture, a computer that is not in the scene you are in, or a bad mod id. It does not throw for any of these. <span class="tech">(the computer is found among the active `InteractableComputer` objects in the active scene, so a computer on a switched-off object or in another scene is not found)</span>
- You can't add files or emails to a computer. The game sets up a computer's files and emails once, when the computer starts. Doing it again would show every old entry twice, so the framework does not offer it. <span class="tech">(`ComputerScreen.AddEmails` and `ComputerScreen.AddRecycleBinFiles` assign the list and build a row for every entry, so a second call duplicates the rows; they are not wrapped)</span>
- You can't open a file on a computer from your mod. Opening a file shows a game panel that needs one of the game's own note assets, and the framework has no way to look one up by name. <span class="tech">(`ComputerScreen.OpenTextFile` takes a `NoteInfo` asset)</span>

## Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\obenseuer.moddingframework.computerdesk.log`.

Everything not listed in this reference is internal and may change without notice. Items marked experimental may change too.

### When you can call it

- Call `SetWallpaper` from your mod's normal code (`OnLoad`, event handlers, `Update`), not from a background thread you started. The game only works safely on one thread, the main thread (see the [glossary](../glossary.md#main-thread)).
- It only does something once a computer exists in the scene. On the main menu it returns `false`.

### If something fails to set up

- This API does not change any game method, so there is no feature group to turn off.
- If the game's private computer screen field is missing in a future game version, `SetWallpaper` returns `false`.
- If the game raises an error while the wallpaper is set, the framework writes one error line to the log, then returns `false`.
