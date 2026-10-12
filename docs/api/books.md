# Obenseuer Modding Framework books API (notes and book pages)

Experimental. Not checked in game.
Game: Obenseuer 0.5.05 (build 25839246), Unity Mono.
Namespace: `ObenseuerCore`. Log owner: `obenseuer.moddingframework.books`. Logs go to `BepInEx\logs\obenseuer.moddingframework.books.log`.

Everything not listed here is internal. Main thread only.

Scope: books the player holds, and whether each one has been read. Single notes are not books and never raise `Read`. To open a book's page in the notes window, use `Hud.ShowNote` (see `docs\api\hud.md`). This facade does not change it.

## Rules

- **Read-only.** No writes. The facade does not add pages or mark books read.
- **Collection only.** The game has no list of every book asset. `All()` lists the books the player holds, from the game's note collection.
- **Bad arguments.** A `null` id throws `ArgumentException`. Everything else returns the fallback.

## API

```csharp
public static class Books
{
    public static bool IsAvailable { get; }
    public static List<BookEntryInfo> All();          // books the player holds, in collection order
    public static bool IsRead(string id);             // false when the player does not hold the book
    public static event Action<BookReadArgs> Read;
}

public sealed class BookEntryInfo
{
    public string Id { get; }                         // book id; the title when the game has no id
    public string Title { get; }
    public int PageCount { get; }                     // pages in the book itself, not only the ones held
}

public sealed class BookReadArgs
{
    public string Id { get; }
    public string Title { get; }
}
```

- `IsRead` is the game's `read` flag on the player's copy. A new page sets it back to unread.
- `Read` fires when a book goes from unread to read. That happens when the player selects the book in the notes window. It fires again after a new page arrives and the book is read again.
- `IsAvailable` is `installed && !CorePatcher.IsDisabled(owner)`. `All` and `IsRead` do not depend on it.
- Notes in the player's inventory (notebooks the player writes) are not books and do not raise `Read`.
