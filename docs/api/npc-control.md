---
title: Obenseuer Modding Framework NPC control API (npccontrol)
parent: API reference
nav_order: 95.3
---

# Obenseuer Modding Framework NPC control API (npccontrol)

Status: experimental.
Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Log owner: `obenseuer.moddingframework.npccontrol`. Logs go to `BepInEx\logs\obenseuer.moddingframework.npccontrol.log`.

Ids are the NPC ids from the `Npcs` facade (`NpcInfo.Id`, `Npcs.All()`). For a mod NPC from [Mod NPCs](modnpcs.md), the id is `ModNpcs.CharacterIdOf(modId, npcId)`.

Taking an NPC lets a mod switch off part of its behaviour for a while, for example to run its own talk or a scene. Each flag switches one part off. Release puts the values back.

## Rules

- **Main thread only.** Call every member on Unity's main thread.
- **One owner.** A mod takes an NPC with its `modId`. Another mod's `Take` on the same NPC returns `false` until the owner releases it.
- **Claims end by themselves.** A claim ends when the NPC dies, from the framework's next call, so a later `Release` returns `false` and `IsTaken` is `false`. A `Take` by your mod on the same npc id after it moves to a new NPC instance drops the stale claim and takes the new instance, and that drop puts back the same values `Release` puts back, which for a dead NPC is the fade distance only.
- **Live NPCs only.** `Take` needs a loaded NPC that is alive. An NPC that is inactive only because its `FadeGameObject` hid it by distance can be taken. With the `Fade` flag, an NPC that is inactive only because the camera culled it can be taken too. An NPC that is inactive for any other reason is refused, and so is a culled NPC taken without `Fade`. An unknown, dead or off-scene NPC is refused.
- **Saves and loads.** `Take` returns `false` while a save loads or saves. `Release` and `ReleaseAll` always work. The game's save does not write any value the framework changes, so the framework does nothing special at save time.
- **Bad arguments.** A `modId` that fails `ModIds` validation and a `flags` value outside `NpcControlFlags.All` throw `ArgumentException`. Game failures are logged once and the call returns `false`. Nothing else throws.
- **Values are put back only when the game has not changed them.** The fade distance is put back only while it still holds the value the framework set. Movement is the one flag that sets a value to hold the NPC, so its release writes `canMove` back. It does not turn movement on while the NPC is seated or asleep, because the game turns movement on again when the NPC stands up or wakes. Sight, Talk and Barks change no value. The framework checks their flags whenever the game asks, so releasing them takes effect at once.
- **No events.** This facade raises no events of its own.

## API

```csharp
public static class NpcControl
{
    public static bool IsAvailable { get; }
    public static bool Take(string modId, int npcId, NpcControlFlags flags = NpcControlFlags.All);
    public static bool Release(string modId, int npcId);
    public static int ReleaseAll(string modId);
    public static bool IsTaken(int npcId);
    public static string TakenBy(int npcId);
}

[Flags]
public enum NpcControlFlags
{
    None = 0,
    Sight = 1,
    Talk = 2,
    Barks = 4,
    Fade = 8,
    Movement = 16,
    All = Sight | Talk | Barks | Fade | Movement
}
```

- **IsAvailable** is false when the owner `obenseuer.moddingframework.npccontrol` was disabled. Release and the reads keep working, so a mod can still give back an NPC it took earlier.
- **Take(modId, npcId, flags)** takes the NPC for `modId` and changes the parts named in `flags`. It returns `true` on success. It returns `false` for another mod's NPC, an unknown or dead NPC, an NPC that is not live, a save or load in progress, or a disabled owner.
- **Take again** with the same mod updates the flags. A flag that is no longer in the set is put back. A new flag is applied at once. It returns `true`.
- **Take with `None`** holds the NPC for your mod without changing any value. Other mods cannot take it while you hold it.
- **Release(modId, npcId)** gives the NPC back. It puts back each value your mod changed and returns `false` when your mod holds nothing on that NPC.
- **ReleaseAll(modId)** releases every NPC your mod holds and returns how many it released.
- **IsTaken(npcId)** is true while any mod holds the NPC.
- **TakenBy(npcId)** returns the `modId` that holds the NPC, or `null` when it is free.

## What each flag changes

- **Sight** stops the NPC's field of view from detecting anything while the flag is held. The NPC does not see the player, does not notice other NPCs or targets, does not witness crimes, and does not turn its head toward the player. An NPC with no field of view component is not affected.
- **Talk** stops the player from starting a conversation with the NPC by the use key. The NPC shows the game's "Can't talk" prompt. Conversations the game forces still start, such as the NPC's own scripted reactions, the police and robber acts, card game prompts and the black screen dialogue.
- **Barks** stops the NPC's bark starter, including its character bark starter, and every `DialogueSystemTrigger` on the NPC that has a bark conversation set. So the game's own comments stop: hits, guitar, crime and random comments. While Barks is held, [NPC actions](npc-actions.md) `Comment` refuses the NPC. `Say` is not stopped, because it does not go through the bark starter. Barks that the game sends straight to the Dialogue System, without a bark starter, are not stopped. An example is the behaviour tree's bark task.
- **Fade** raises the `hideDistance` of the NPC's own `FadeGameObject` to one million metres. The game then does not hide the NPC by distance. A distance-hidden NPC is shown on the game's next fade update, not at once. Fade also stops camera culling from hiding the NPC. A culled NPC is shown when you take it, and while the flag is held the game's culling callback does nothing for it, so the NPC stays shown when the camera looks away. On release the framework puts back the distance and does not write the culled flag back, so the game's next culling change for the NPC decides. A culled NPC that is out of view can stay shown until then. If the game does not have a `culled` bool field, the flag does nothing and the framework logs it once.
- **Movement** sets `richAI.canMove` to `false` and holds the NPC's activity, the same as [NPC movement](npc-movement.md) `SetActivityEnabled`, under the owner `obenseuer.moddingframework.npccontrol`. The schedule does not start or change the NPC's activity while the flag is held. The framework sets `canMove` to `false` again on every frame while the flag is held. On release, `canMove` comes back as it was when you took the NPC. It comes back `true` if the game turned movement on during the take. It stays off while the NPC is seated in a chair or asleep, because the game turns movement on when the NPC stands up or wakes. A mod NPC that is in a routine order is not timed out while it is held. The hold is not placed while the game has the NPC's activity disabled (`activityDisabled`), so release never turns on an activity the game turned off. The hold is placed on a later frame once the game clears that flag. A hold is also not placed on an NPC whose controller is not live, such as one hidden by the fade. The framework retries each frame until the controller is live.

The framework does not hold the NPC's activity for any flag other than Movement. Release drops only the hold that the Movement flag placed. Another mod's hold on the same NPC is left alone.

## Not included

- **Animation, sound, appearance and the NPC's own state.** These are not changed.
- **Walks already under way.** A walk that the game started before the take finishes or stops by the game's own rules. Only the hold stops new activity.

## Release on a dead NPC

When the NPC is dead at release, the framework puts back the fade distance only. It leaves `canMove` as the game holds it. A dead NPC is not held: the Sight, Talk and Barks flags stop applying as soon as the NPC dies, so the game's own behaviour applies to it.

## Scene loads and saves

- **Single scene load.** Every claim is released and each value still alive is put back. Claim the NPC again from `Events.WhenReady` if you still need it. A `Take` called straight from an `Events.SceneLoaded` handler is refused, because the load gate is set for the whole of that event and `Take` returns `false` while it is set.
- **Save.** The save does not write any value the framework changes. Take is refused while the save runs, and Release still works.

## Example

```csharp
void StartScene(int npcId)
{
    NpcControlFlags flags = NpcControlFlags.Sight | NpcControlFlags.Talk | NpcControlFlags.Fade | NpcControlFlags.Movement;
    if (!NpcControl.Take("my.mod", npcId, flags)) return;
}

void Speak(int npcId)
{
    if (!NpcActions.Say(npcId, "Wait here.")) return;
}

void EndScene(int npcId)
{
    NpcControl.Release("my.mod", npcId);
}
```

`NpcActions.Say` refuses an NPC that is hidden by the fade, because it needs the NPC's own object to be active. Check the result, and call it again later if it returned `false`. `Say` does not read the bark starter, so Barks can stay out of the flags for a line of speech.

## If a hook cannot be installed

- `NpcControl.Init` subscribes to the scene event, and the framework calls `NpcControl.Tick` once per frame. It also patches five game methods: `FadeGameObject.Cull`, `NPC_FOV.CheckIfTargetIsDetected`, `InteractableTalk.CanTalk`, `BarkStarter.TryBark` and `DialogueSystemTrigger.Fire`. The five install together or not at all. If one cannot be installed, the owner `obenseuer.moddingframework.npccontrol` is disabled, `IsAvailable` is false and `Take` returns `false`. Release and the reads keep working.
- If `FadeGameObject.culled` is missing or is not a bool, the framework logs it once. The Fade flag then does nothing.
