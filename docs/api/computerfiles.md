# Obenseuer Modding Framework computer contents API (files and emails)

Experimental. Not checked in game.
Game: Obenseuer 0.5.05 (build 25839246), Unity Mono.
Namespace: `ObenseuerCore`. Log owner: `obenseuer.moddingframework.computerfiles`. Logs go to `BepInEx\logs\obenseuer.moddingframework.computerfiles.log`.

Everything not listed here is internal. Main thread only.

Named `ComputerContents` because `ComputerFiles` is a game class. The file keeps the area name. Desk app registration is `ComputerApps` (`src\ComputerApps.cs`), not this facade.

Scope: the recycle bin files and the email inbox of a computer in the loaded scene.

## Rules

- **Read-only, no writes.** The computer keeps its files and emails in inspector-set lists (`InteractableComputer.emails` and `recycleBinFiles`). They are not saved (`[fsIgnore]`), and the game only copies them to the screen at `Start`. The game has no method to add or remove a file or an email at runtime, so the facade cannot write them.
- **Computer id.** `computerId` is what `ComputerContents.All()` or `ComputerContents.IdOf` returns. It is the computer's world GUID when the game gave one, otherwise the scene id (`Scene:Root[0]/Child[2]/Computer[1]`, each node with its sibling index, the `SceneIds.Of` scheme in `src/Api/SceneIds.cs`). Most computers have no GUID, so the path form is the usual one. It is stable while the scene is unchanged and is not meant for computers spawned at runtime.
- **Bad arguments.** A `null` id throws `ArgumentException`. An unknown id, or a computer that is not loaded, returns an empty list.

## API

```csharp
public static class ComputerContents
{
    public static List<ComputerInfo> All();                           // loaded computers
    public static string IdOf(GameObject computer);                  // null when the object has no InteractableComputer
    public static List<ComputerDocInfo> Files(string computerId);    // recycle bin files
    public static List<ComputerDocInfo> Emails(string computerId);   // inbox emails
}

public sealed class ComputerInfo
{
    public string Id { get; }
    public string Title { get; }                                     // the computer's title, "" when unset
    public string Scene { get; }
}

public sealed class ComputerDocInfo
{
    public string Title { get; }
    public string Sender { get; }
    public string Body { get; }
}
```

- `Files`: `Title` is the file name, `Sender` is `""`, and `Body` is the text of a text file. A file that is not a text file has `Body` `""`.
- `Emails`: `Title` is the subject, `Sender` is the sender's name, and `Body` is the email text.
- Lists are copies, in the order the game holds them. Each call reads the game's current lists.
- The facade has no events and no patches, so there is no `Init` and no `IsAvailable`.
