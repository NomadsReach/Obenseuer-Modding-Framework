---
title: Obenseuer Modding Framework notes API (single notes and emails)
parent: API reference
nav_order: 48
---

# Obenseuer Modding Framework notes API (single notes and emails)

Experimental.
Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Log owner: `obenseuer.moddingframework.notes`. Logs go to `BepInEx\logs\obenseuer.moddingframework.notes.log`.

Everything not listed here is internal. Main thread only.

Scope: single notes and emails the player holds. Book pages are not covered: use `Books` (see [books.md](books.md)) to read books and `Hud.ShowNote` (see [hud.md](hud.md)) to open a note or page. Notebooks the player writes are not covered either.

## Rules

- **Safe at any time.** Reads return an empty list or `false` when no save is loaded or the note controller is missing. `Give` returns `false` while a save loads or saves (`SaveData.Busy`).
- **Bad arguments.** A `null` id returns `false` and is logged once. Everything else returns the fallback.
- **Game collect path.** `Give` calls the game's `NoteController.AddNote`, the same method mail, tasks and bundles use. The notes window and the save update through the game's own code.
- **Silent.** `Give` does not show the "New note" popup. The game's mail and task paths are silent too.
- **Mod notes are not supported.** A note is a `NoteInfo` asset, and the save stores it by addressable path. A mod cannot add a new note without shipping an asset, so `Give` only works for notes the game already has.

## API

```csharp
public static class Notes
{
    public static bool IsAvailable { get; }
    public static List<NoteEntryInfo> All();          // every standalone note the game has, in asset order
    public static List<NoteEntryInfo> Held();         // standalone notes the player holds, in collection order
    public static bool Has(string id);                // false when the player does not hold the note
    public static bool Give(string id);               // true only when the note was added now
    public static event Action<NoteCollectedArgs> Collected;
}

public sealed class NoteEntryInfo
{
    public string Id { get; }                         // note id; the addressable path or the title when the game has no id
    public string Title { get; }
}

public sealed class NoteCollectedArgs
{
    public string Id { get; }
    public string Title { get; }
}
```

- `All` lists the `NoteInfo` assets in the game's `Resources/Notes` folder, the same list the developer console uses. It skips book pages (`NoteInfo.book` set).
- `Held` lists the player's collected entries without a book, from `NoteController.notes`.
- `Has` and `Give` find the asset by `Id`. If two assets share the same id, neither can be given or checked, and both return `false`.
- `Give` returns `false` when the id is unknown, the player already holds the note, or the note controller is not running (main menu, no save loaded).
- `Collected` fires once for each standalone note that goes from not held to held, however it was added: by `Give`, by mail, by tasks or by the console. It does not fire when a save loads.
- `IsAvailable` is `installed && !CorePatcher.IsDisabled(owner)`. `All`, `Held`, `Has` and `Give` do not depend on it.

## Save loads

The framework does not gate this event on a save load. A handler that should ignore events raised while a save loads can check `SaveData.Busy`, which is true while a save is loading or being written.
