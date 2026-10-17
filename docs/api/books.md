---
title: Books
parent: Phones, mail and media
grand_parent: API reference
nav_order: 1
---

# Books

Status: experimental.

## What it is

This API lets your mod see the books the player has collected and whether each one has been read. It also tells you when the player reads a book for the first time. It covers books only. A single note is not a book and never counts as read here. To open a page in the notes window, use `Hud.ShowNote` (see the [Hud page](hud.md)).

## When to use it

- Show a message or give a reward the first time the player reads a certain book.
- Check whether the player has read a book before a quest step goes on.
- List the books the player is holding, with their titles and page counts.

## Quick example

This mod writes a log line when the player reads a book. Once a save is loaded, it also logs every book the player holds.

```csharp
using ObenseuerCore;

[ModInfo("example.bookwatch", "Book Watch", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class BookWatch : FrameworkMod
{
    protected override void OnLoad()
    {
        Books.Read += args => Log.Info("The player read " + args.Title);
        Events.WhenReady(() =>
        {
            foreach (BookEntryInfo book in Books.All())
                Log.Info(book.Title + " (" + book.PageCount + " pages), read: " + Books.IsRead(book.Id));
        });
    }
}
```

`Read` is an event: the framework calls your code each time it happens. See [Using events](../guides/getting-started/using-events.md) for more on events and `WhenReady`.

## Common tasks

Count the books the player has read.

```csharp
int read = 0;
foreach (BookEntryInfo book in Books.All())
    if (Books.IsRead(book.Id)) read++;
Log.Info(read + " books read.");
```

Check one book by its id.

```csharp
if (Books.IsRead("some-book-id")) Log.Info("The player has read it.");
```

## Reference

This is the full list of what the API gives you. Text in brackets at the end of a note is extra detail for experienced modders, and you can skip it on a first read.

A book in your list is a `BookEntryInfo`. The list only has books the player is holding right now, because the game has no list of every book that exists.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Books</code> <span>the books the player holds</span></p>

Use `All` to list the books the player holds, `IsRead` to check one by its id, and listen to `Read` to hear when a book is read.

<div class="api-pair" markdown="1">

```csharp
public static class Books
{
    public static bool IsAvailable { get; }
    public static List<BookEntryInfo> All();
    public static bool IsRead(string id);
    public static event Action<BookReadArgs> Read;
}
```

| Method | Returns | What it does |
|---|---|---|
| `All()` | `List<BookEntryInfo>` | The books the player holds, in the order the game keeps them. |
| `IsRead(id)` | `bool` | `true` when the player has read the book. `false` when the player does not hold it. |

| Property | Type | What it means |
|---|---|---|
| `IsAvailable` | `bool` | `false` when the read event could not be set up. |

| Event | Gives you | When it fires |
|---|---|---|
| `Read` | `BookReadArgs` | The player has just read a book. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>BookEntryInfo</code> <span>one book the player holds</span></p>

`All` gives you one of these for each book the player holds. Its `Id` is what `IsRead` takes.

<div class="api-pair" markdown="1">

```csharp
public sealed class BookEntryInfo
{
    public string Id { get; }
    public string Title { get; }
    public int PageCount { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | The book's id. Use it with `IsRead`. |
| `Title` | `string` | The book's title. |
| `PageCount` | `int` | How many pages the book has in total, not only the ones the player holds. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>BookReadArgs</code> <span>what you get when a book is read</span></p>

`Read` hands you this when a book goes from unread to read. `Id` and `Title` say which book it was.

<div class="api-pair" markdown="1">

```csharp
public sealed class BookReadArgs
{
    public string Id { get; }
    public string Title { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | The book's id. |
| `Title` | `string` | The book's title. |

</div>
</div>

### Things to know

- `All()` only lists books the player holds. The game has no list of every book, so a book the player has not found does not appear. <span class="tech">(the list comes from the game's note collection (`NoteController.notes`); an entry with no book in it is skipped)</span>
- A book's `Id` is the first of these that the game has set: its id, its asset path, or its title. <span class="tech">(this is `BookInfo.Id` in the game)</span>
- `IsRead(id)` says whether the player has read the book. It returns `false` for a book the player does not hold.
- When a new page arrives in a book, the game marks the whole book unread again. <span class="tech">(it reads the `read` flag of the player's `CollectedNoteInfo` for that book)</span>
- `Read` fires when a book goes from unread to read. That happens when the player selects the book in the notes window.
- `Read` fires again after a new page arrives and the player reads the book again. It does not fire for a book that is already read. <span class="tech">(it is raised after `NoteUIPanel.Selected` when the `read` flag changed from false to true)</span>
- A note is not a book. Single notes, including the notes the player writes in the notebooks in their inventory, are not books and never raise `Read`.

### Limits

- A `null` id passed to `IsRead` also returns `false`, and one line about it goes to the log.
- The framework does not hold back `Read` while a save is loading. A handler that should ignore events raised then can check `SaveData.Busy`, which is `true` while a save is loading or being written.
- You can't add pages to a book or mark a book as read. This API only reads. Single notes, which are not books, are on the [Notes page](notes.md).

## Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\obenseuer.moddingframework.books.log`.

Everything not listed in this reference is internal and may change without notice. Items marked experimental may change too.

- The framework changes one of the game's methods when it starts, so it can tell your mod when a book is read.
- Its feature group, the name you will see in the log, is `obenseuer.moddingframework.books`.

### When you can call it

- Call everything from your mod's normal code (`OnLoad`, event handlers, `Update`), not from a background thread you started. The game only works safely on its main thread (see the [glossary](../glossary.md#main-thread)).
- It is safe to call any of it at any time, even on the main menu. With nothing to read, `All()` gives an empty list and `IsRead` gives `false`. Neither depends on `IsAvailable`.
- `Books.IsAvailable` tells you whether `Read` works. It is `false` when the framework could not set up the read event, or when the feature group was turned off.
- If the game fails while the framework reads, you get the plain default (an empty list or `false`) instead of an error.

### If something fails to set up

- The framework changes some of the game's methods when it starts. If a game update breaks one of them, the framework removes all the changes of the [feature group](../glossary.md#feature-group) that depends on it, turns the group off, and writes error lines and a warning to the group's log file in `BepInEx\logs`. The rest of the framework keeps working.
- If the change to the notes window fails, the feature group turns off and `Read` stops. One warning goes to the log. `All()` and `IsRead` keep working. <span class="tech">(the method is `NoteUIPanel.Selected`. The log line reads "book events not installed, Read is disabled")</span>
