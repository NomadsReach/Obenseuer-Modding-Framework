---
title: Signs API
parent: API reference
nav_order: 74
---

# Signs API

Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Patch owner: `obenseuer.moddingframework.signs`.
Files: `src/Api/Signs.cs`, `src/Api/SignsPatches.cs`.

This area wraps the editable sign (`EditableSign`): the text the player or a mod writes on it. Saved text is part of the save.

## If a hook cannot be installed

The framework installs two hooks, `EditableSign.StartEditing` and `StopEditing`, which track player edits for `TextChanged`. If either fails, the owner is disabled and one warning is logged. The reads and `SetText` keep working. `TextChanged` stops for player edits, but still fires for `SetText`, because `SetText` raises it directly.

## Threading and safety

- Main thread only.
- `Signs.IsAvailable` is false when the hooks were not installed or the owner was disabled.
- Every member is safe at any time. On the main menu or with no save loaded, reads return an empty list or `null`, and `SetText` returns `false`.
- Nothing throws except `ArgumentException` when `text` is `null`. A null or empty `id` finds nothing.
- `SetText` returns `false` while a save is loading or being written, and while the sign is being edited.
- Objects on inactive GameObjects are not returned (`FindObjectsByType` default filter).
- `TextChanged` is raised synchronously after the text has changed. A handler that throws is logged and the other handlers still run.

## API

```csharp
public static class Signs
{
    public static List<SignInfo> All();
    public static SignInfo Get(string id);
    public static bool SetText(string id, string text);

    public static bool IsAvailable { get; }
    public static event Action<SignTextChangedArgs> TextChanged;
}

public sealed class SignInfo
{
    public string Id { get; }        // the save GUID, else the scene path
    public string Text { get; }      // EditableSign.text, the saved text
    public int MaxLines { get; }     // EditableSign.maxLines, 1 for a single-line sign
    public int MaxLength { get; }    // TMP characterLimit of the sign's input, 0 = no limit
}

public sealed class SignTextChangedArgs    // payload of TextChanged
{
    public string Id { get; }        // as SignInfo.Id
    public string Text { get; }      // the new text
}
```

- `All` lists every `EditableSign` in the active scene. `Get(id)` returns one `SignInfo`, or `null` when no sign has that id.
- `SetText(id, text)` sets the sign's `text` and refreshes its display with the game's `UpdateText`, as the game does after a player edit. It returns `true` when the write happened. It returns `false` when:
  - no sign has that id;
  - a save is loading or being written;
  - the sign is being edited right now (the game overwrites the text every frame while editing);
  - `text` is longer than `MaxLength` (when it is above 0);
  - `text` has more lines than `MaxLines` allows.
  It never truncates. A mod that wants a shorter text must shorten it before the call.
- `TextChanged` fires when the text changes in two ways: after a successful `SetText` that changed the text, and when the player finishes an edit (the game's `StopEditing`) and the text is different from the text at the start of that edit. It does not fire on a load, because the load restores the text without calling either path.

## Not wrapped

- The edit flow itself (`Interact`, `StartEditing`, the keyboard handling). A mod cannot start an edit. The player's edit is observed only through `TextChanged`.
- The `editing` static, which the game uses to track the one sign being edited. It is private and not needed for the reads.
- Font, colour and position of the text (`textComponent`).
