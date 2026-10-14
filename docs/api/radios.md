---
title: Radios API
parent: API reference
nav_order: 58
---

# Radios API

Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. No patches, so no owner and no events.
Files: `src/Api/Radios.cs`.

Everything not listed here is internal. There is no global radio. Each `Radio` is one prop with its own sound, so the API is per radio, addressed by its `GUID`. See `docs/api/audio.md` (Music tracks, under "Not wrapped"): the game has no music or radio player class, and nothing global can play or stop a station.

## Threading and safety

- Main thread only.
- Every member is safe at any time. With no save loaded, or on the main menu, `All` returns an empty list, `Enabled` and `State` return `false` and `0`, and `SetEnabled` returns `false`.
- `SetEnabled` returns `false`, and logs once, for a null or empty id. It also returns `false` while a save loads or saves (`SaveData.Busy`).

## API

```csharp
public static class Radios
{
    public static List<RadioInfo> All();
    public static RadioInfo Get(string id);
    public static bool Enabled(string id);
    public static int State(string id);
    public static bool SetEnabled(string id, bool enabled);
}

public sealed class RadioInfo
{
    public string Id { get; }              // Radio.GUID, or "<scene>:<root>[i]/.../<radio>[j]" when the radio has no GUID
    public string Title { get; }           // Interactable.title, "Radio" when the game left it empty
    public Vector3 Position { get; }       // world position
    public bool Enabled { get; }           // Radio.RadioEnabled
    public int State { get; }              // Radio.CurrentState: 0 is off, 1 and up are the channels
}
```

- `All` lists every active `Radio` in the scene. The id is the radio's `GUID` when it has one. A radio with an empty `GUID` (the game clears the GUIDs of prefabs it spawns through `logic_prefab_spawn`, and such a radio is not saved) gets a fallback id, `<scene name>:<root>[i]/.../<radio>[j]`, where each part is a hierarchy node name and its sibling index. The fallback is stable while the hierarchy keeps its order, but a spawned radio can change it between sessions, so keep it for the current session only.
- `Get(id)` returns one `RadioInfo`, or `null` when no active radio has that id.
- `Enabled` reads `RadioEnabled`. This is the radio's power state, not its channel.
- `State` reads `CurrentState`. `0` is off, `1` and up are the channels. A radio can keep a channel while disabled, as the game does.
- `SetEnabled` calls the game's own toggle, `Radio.EnableRadio(bool)`. It stops or starts the sound and swaps the mesh material, and it leaves `CurrentState` unchanged. It returns `true` when the radio was found and the game's toggle ran. The game ignores a call that does not change the state. Side effects are the game's own: enabling a radio re-applies its channel (`SetState`), which plays the channel or turn-off sound and fires the radio's turn-on or turn-off outputs, so enabling a radio that is on channel 0 plays the turn-off sound and fires the turn-off outputs. No game code calls `EnableRadio`, so any caller is a serialized scene event. Whether a scene calls it is still open. The player's use key calls `Radio.Use`, which changes the channel only, and is not wrapped.

## Not wrapped

- Channel changes (`Radio.Use`, `Radio.SetState`). The player changes channels with the use key, and `SetState` plays the channel sound. These are not exposed; `State` reads the current channel only.
- Turning a radio off with its sound effect (`Radio.TurnOff`). Only the enable toggle is exposed.
- Soundscape and music. See `docs/api/audio.md`.
