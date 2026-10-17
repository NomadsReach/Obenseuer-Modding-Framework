---
title: Notes
parent: Phones, mail and media
grand_parent: API reference
nav_order: 6
---

# Notes

Status: experimental.

## What it is

In the game, the player collects notes and emails: single sheets with a title and some text, which they can read again in their notes window. This API lets a mod list all the single notes the game has, list the ones the player holds, check whether the player holds one, give one to the player, and get told when the player gets one.

Book pages are not covered here. Use `Books` (see [Books](books.md)) to read books, and `Hud.ShowNote` (see [HUD](hud.md)) to open a note or a page on screen. Notebooks that the player writes in are not covered either.

## When to use it

- Give the player a note when a quest step is done.
- Check that the player has read or collected a note before something happens.
- Log or react when the player collects a note, however they got it.

## Quick example

This mod logs the title of every note the player collects. Once a save is loaded, it also logs how many single notes the game has and how many the player holds.

```csharp
using ObenseuerCore;

[ModInfo("example.notewatch", "Note Watch", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class NoteWatch : FrameworkMod
{
    protected override void OnLoad()
    {
        Notes.Collected += args => Log.Info("Collected note: " + args.Title);
        Events.WhenReady(() => Log.Info("The game has " + Notes.All().Count + " notes, the player holds " + Notes.Held().Count));
    }
}
```

`Collected` is an event: the framework calls your code each time it happens. `WhenReady` runs your code once a save is loaded (see [Using events](../guides/getting-started/using-events.md)).

## Common tasks

Give the player a note by its id. `Give` returns `true` only when the note was added just now.

```csharp
NoteEntryInfo first = Notes.All()[0];
if (Notes.Give(first.Id)) Log.Info("Gave the note " + first.Title);
```

Check whether the player already has a note.

```csharp
bool has = Notes.Has("some-note-id");
```

List the notes the player holds.

```csharp
foreach (NoteEntryInfo note in Notes.Held())
    Log.Info(note.Id + ": " + note.Title);
```

## Reference

This is the full list of what the API gives you, one section for each part. In every code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

### Notes

`Notes` reads and gives the game's single notes. Each note is described by a `NoteEntryInfo`.

```csharp
public static class Notes
{
    public static bool IsAvailable { get; }          // false when the collected event could not be set up
    public static List<NoteEntryInfo> All();         // every single note the game has
    public static List<NoteEntryInfo> Held();        // the single notes the player holds, in the order they were collected
    public static bool Has(string id);               // true when the player holds this note
    public static bool Give(string id);              // gives the player the note; true only when it was added just now
    public static event Action<NoteCollectedArgs> Collected;   // the player has just collected a note
}

public sealed class NoteEntryInfo    // one note
{
    public string Id { get; }        // the note's id; use it with Has and Give
    public string Title { get; }     // the note's title
}

public sealed class NoteCollectedArgs    // what you are told when a note is collected
{
    public string Id { get; }        // the note's id
    public string Title { get; }     // the note's title
}
```

- **`All()` lists every single note the game has, and leaves out book pages.** It is the same list the game's developer console uses.
  - Technical: it lists the `NoteInfo` assets in the game's `Resources/Notes` folder and skips any with `NoteInfo.book` set.
- **`Held()` lists the single notes the player has collected, in the order they were collected.** Book pages are left out.
  - Technical: it reads `NoteController.notes` and skips entries that have a book.
- **A note's id is the note's own id in the game files. If it has none, the id is where the note is stored, and if it has neither, the id is its title.**
  - Technical: `Id` is `NoteInfo.Id`: the `id` field, else `addressableAssetPath`, else `title`.
- **`Has(id)` and `Give(id)` find the note by its id, and do nothing if two notes share an id.** If two notes in the game have the same id, `Has` and `Give` can't tell them apart, so both return `false` for that id.
- **`Give(id)` adds the note the way the game's own mail, tasks and bundles do.** The notes window and the save both update as they would in the game. It does not show the "New note" pop-up, the same as the game's mail and tasks.
  - Technical: it calls the game's `NoteController.AddNote`.
- **`Give` returns `false` when it did not add the note.** That happens when the id is not a note the game has, when the player already holds the note, when a save is loading or being written, and when the game's note controller is not running (on the main menu, or before a save is loaded). An id of `null` also returns `false`, and writes one line to the log.
- **`Collected` fires once for each single note that goes from not held to held, however it was added.** It fires for `Give`, for mail, for tasks and for the console. It does not fire again for a note the player already holds, and it does not fire when a save loads.
- **`IsAvailable` is `false` when the collected event could not be set up.** The reads and `Give` keep working when it is `false`. See [If something fails to set up](#if-something-fails-to-set-up).

### What you can't do (yet)

- **You can't add a new note of your own.** Every note is a data file that ships with the game, and the save stores a note by the path of that file. A mod can't add a note without shipping such a file, so `Give` only works for notes the game already has.
  - Technical: a note is a `NoteInfo` asset, stored in the save by its addressable path.

## Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\obenseuer.moddingframework.notes.log`.

Everything not listed in this reference is internal and may change without notice. Items marked experimental may change too.

### When you can call it

- **Call everything from your mod's normal code, not from a background thread you started.** The game and Unity only work safely on one thread, the main thread (see the [glossary](../glossary.md#main-thread)).
- **It is safe to call any of it at any time, even on the main menu or while a save is loading.** If no save is loaded, or the game's note controller is missing, the reads give an empty list or `false`.
- **Your `Collected` handler runs inside the game's own call, one at a time.** If one of your handlers throws an error, the framework logs it and still runs the other handlers.
- **`Collected` also fires while a save is loading, if a note is added then.** If you want to ignore those, check `SaveData.Busy`, which is `true` while a save is loading or being written.

### What to check for and what comes back

- **Nothing here throws.** A `null` id gives `false` and one line in the log. Everything else gives its plain default (`false` or an empty list).
- **`IsAvailable` only tells you about the `Collected` event.** In code it is "set up, and the feature group is not turned off".

### If something fails to set up

The framework changes some of the game's methods when it starts, so it can tell your mod when a note is collected. If a game update breaks one of them, only that feature turns off, and the framework writes error lines and a warning to the log. The rest of the API keeps working.

- **The `Collected` event depends on the feature group `obenseuer.moddingframework.notes`.** This is the name you will see in the log. If the framework can't change the game's method that adds a note, it turns the group off, `Collected` stops, and the framework writes error lines and a warning to `BepInEx\logs\obenseuer.moddingframework.notes.log`. `All`, `Held`, `Has` and `Give` keep working.
  - Technical: the game method is `NoteController.AddNote(NoteInfo, bool)`.
