---
title: Instruments API
parent: API reference
nav_order: 100
---

# Instruments API

Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Patch owner: `obenseuer.moddingframework.instruments`.
Files: `src/Api/Instruments.cs`, `src/Api/InstrumentsPatches.cs`, `src/Api/GuitarSong.cs`.

The only instrument in the game is the guitar (`WieldableItem.WieldableGuitar`). It is a hand item: the player holds it and uses it with the Primary Use key. This area reads the guitars, plays one strum or a timed song of strums, and reports when a strum is played and when a witness comments on it.

## If a hook cannot be installed

The framework installs four hooks under one owner: `WieldableGuitar.onPrimaryUse` (prefix and postfix), `NPCRandomTalk.OnGuitarPlayed` (postfix), `WieldableGuitar.Update` (prefix and postfix) and `CharacterBarkStarter.TryToComment` (postfix). A failed hook removes the owner's patches and logs one warning. `PlayNote`, `PlaySong` and the reads keep working, and the events stop.

## Threading and safety

- Main thread only.
- `Instruments.IsAvailable` is false when the hooks were not installed or the owner was disabled.
- Every read is safe at any time. With no save loaded, or on the main menu, `All` returns an empty list and `Get` returns `null`.
- `PlayNote`, `PlaySong` and `HoldNote` are refused while a save loads or saves (`SaveData.Busy`). A song strum that falls in that time is skipped.
- Strums and held notes are refused while the player's input is blocked: the game is paused (`GameController.GameIsPaused`), a dialogue is active (`DialogueController.DialogueActive`), a game menu is open (`GameUIController.IsMenuVisible`), or item use is disabled (`PlayerHandItems.itemUseDisabled`). These are four of the checks the player's Primary Use path makes. A song keeps its timing while blocked: a strum that falls due is skipped, not held back, so a 0 second entry does not sound later while paused.
- The events are raised synchronously from the game's own call. A handler that throws is logged and the other handlers still run.
- A song is advanced once per frame by the framework's `Update`, with `Time.deltaTime`. Loading a scene ends every song and every held note.

## API

```csharp
public static class Instruments
{
    public static List<GuitarInfo> All();
    public static GuitarInfo Get(string id);
    public static bool PlayNote(string modId, string guitarId);
    public static bool PlaySong(string modId, string guitarId, IList<float> delays);
    public static bool HoldNote(string modId, string guitarId, int index);
    public static bool StopHold(string modId, string guitarId);
    public static int HoldCount(string guitarId);
    public static bool StopSong(string modId, string guitarId);
    public static bool Clear(string modId);

    public static bool IsAvailable { get; }
    public static event Action<GuitarPlayedArgs> GuitarPlayed;
    public static event Action<GuitarCommentArgs> GuitarCommented;
}

public sealed class GuitarInfo
{
    public string Id { get; }               // save GUID, or the scene path
    public int ItemId { get; }              // the guitar's item id, 0 when none
    public bool Equipped { get; }           // held in a hand (WieldableGuitar.equipped)
    public bool HoldPlaying { get; }        // the game's own hold sound is playing (Primary Use is held); a HoldNote does not set it
    public bool CanPlayNote { get; }        // equipped, not on cooldown, and has a strum sound: the test PlayNote and GuitarPlayed use
}

public sealed class GuitarPlayedArgs
{
    public string GuitarId { get; }         // the guitar that played
}

public sealed class GuitarCommentArgs
{
    public string WitnessId { get; }        // the id of the component the game asked to comment: the witness's NPCRandomTalk, or its CharacterBarkStarter when it has no NPCRandomTalk
    public int NpcId { get; }               // NPCController.ID of the witness, 0 when it has no NPCController
    public bool Positive { get; }           // true: the game asked for the positive comment
}
```

- `All` lists every guitar in the active scene, held or not.
- `Get(id)` returns one guitar, or `null` when no guitar has that id.
- `PlayNote(modId, guitarId)` plays one strum on the guitar, the game's own Primary Use sound. It returns `true` when the strum was played. It returns `false` when the guitar has no such id, when `CanPlayNote` is false, while a save loads or saves, or while the player's input is blocked. `modId` must be a valid mod id. Two mods can call it without conflict.
- `PlaySong(modId, guitarId, delays)` plays a song of strums on one guitar. `delays` has one entry per strum, in seconds. The first entry is the wait before the first strum, and each later entry is the wait after the strum before it. `{ 0f, 0.5f, 0.5f, 1f }` plays four strums at about 0, 0.5, 1 and 2 seconds, the first on the next frame. Each strum is the game's own `onPrimaryUse`, so `GuitarPlayed` fires for each one. It returns `true` when the song is set. It returns `false` when `delays` is null, empty, or holds a NaN, infinite or negative value; when the guitar has no such id or `CanPlayNote` is false when the song starts; or while a save loads or saves. A strum that falls due while the player's input is blocked is skipped, and the song goes on.
- A song strum is skipped when the guitar is no longer equipped or is on its one second cooldown, or the player's input is blocked, at that moment. The song goes on with the next entry.
- A guitar plays one song at a time. A newer `PlaySong` on the same guitar replaces the current song, from any mod. When the replaced song belongs to another mod, one warning is logged naming both mods.
- The guitar is found once, when `PlaySong` is called. A song plays at most four strums in one frame. The rest play on the following frames, so a song can run a few frames late but loses no strum. A song whose guitar is destroyed or inactive plays no more strums.
- `HoldNote(modId, guitarId, index)` plays entry `index` of the guitar's hold sounds, the game's `primaryHoldAudio` array, and keeps it playing until it ends or is stopped. `index` is `0` to `HoldCount(guitarId) - 1`. It returns `true` when the sound started. It returns `false` when `index` is out of range, when the guitar has no such id or `CanPlayNote` is false when it starts, when that entry is missing, while a save loads or saves, or while the player's input is blocked. `modId` must be a valid mod id.
- A guitar has one held note at a time. A newer `HoldNote` on the same guitar replaces the current one, from any mod, and stops its sound. When the replaced one belongs to another mod, one warning is logged naming both mods. A held note also refuses a sound that is already playing, and it lets go if the player starts a hold on the same sound.
- A held note ends when any of these happens: `StopHold` for its mod and guitar, `Clear` for its mod, a Single scene load, the guitar is unequipped or destroyed, the player's input becomes blocked, or its sound stops playing (a one-shot clip ends on its own). The check runs each frame only while a held note exists.
- `StopHold(modId, guitarId)` stops the held note only when `modId` started it. It returns `true` when a held note was stopped.
- `HoldCount(guitarId)` returns the number of hold sounds the guitar has, or `0` when the guitar has no such id or the field is missing. It does not check `CanPlayNote`.
- `StopSong(modId, guitarId)` stops the song only when `modId` set it. It returns `true` when a song was stopped.
- `Clear(modId)` stops every song and every held note that `modId` set. It returns `true` when any of them was stopped. `modId` must be a valid mod id in `PlaySong`, `StopSong`, `HoldNote`, `StopHold` and `Clear`, as in `PlayNote`.
- `GuitarPlayed` fires each time the game plays a strum: a Primary Use key press on an equipped guitar that is not on cooldown, and each strum that `PlayNote` or a song plays. It fires after the game has started the sound. A strum skipped by the input gates does not fire it.
- `GuitarCommented` fires when the game asks a witness to comment on a guitar that is being held. `Positive` is the game's Music skill check. The comment is the game's own bark, so it can still be skipped by the bark's own timing.

## Behaviour

- A guitar is on cooldown for one second after it is equipped, so a strum inside that second does nothing and does not raise `GuitarPlayed`.
- The witness comment happens only while the game's own hold sound is playing (a `HoldNote` does not start it), once the 10 second timer has run out, to a witness within 10 units that currently sees the player (`Witness.GetWitnesses`).
- A witness with an `NPCRandomTalk` gets the comment through `NPCRandomTalk.OnGuitarPlayed`. A witness with a `CharacterBarkStarter` but no `NPCRandomTalk` gets the game's own bare bark, and `GuitarCommented` fires for it too with the same `Positive` flag. That bare comment is reported only when it is started from the guitar's own update, so a bark with the same text from anywhere else is not reported.

## Not covered

- A key-driven sustain. The game's own hold is started and stopped by the key, and `HoldNote` does not change it. A held note from `HoldNote` is a plain play of one hold sound, so it makes no witness comment, and `HoldPlaying` does not show it. Whether the clip loops and its volume are set on the guitar's prefab, not in code, and the asset data was not read.
- Notes and pitches. A guitar has one strum sound and an array of hold sounds. A song is a timed series of strums. The game code has no note table, and the clip contents are asset data that was not read.
- Other instruments. The game code has no piano, drum or other instrument.

## Example

```csharp
Instruments.GuitarPlayed += args => Logger.LogInfo("strum on " + args.GuitarId);

foreach (GuitarInfo guitar in Instruments.All())
{
    if (guitar.CanPlayNote) Instruments.PlaySong("my.mod", guitar.Id, new float[] { 0f, 0.5f, 0.5f, 1f });
}
```
