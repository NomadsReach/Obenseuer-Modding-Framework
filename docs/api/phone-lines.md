---
title: Phone lines API
parent: API reference
nav_order: 53
---

# Phone lines API

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Patch owner: `obenseuer.moddingframework.phones`.
Files: `src/Api/PhoneLines.cs`, `src/Api/PhoneLinesPatches.cs`.

This area wraps the phone objects (`Phone`): ringing a phone, answering or declining a ring, starting a call from it, whether it is busy or ringing, and the call start, call end and ring events. The contact list is `Phones` in [NPCs, animals, relationships and phones](npcs.md#phones). This area does not duplicate it. It does not cover the dial pad (`PhoneUI`) or hanging up an active call.

## If a hook cannot be installed

The framework needs five private fields on `Phone`: `callActive`, `incomingCall`, `conversationTrigger`, `interactableTalk` and `hangupTimer`. It installs four hooks: the prefix on `StartCall(PhoneNumber)`, the postfixes on `OnCallStarted()` and `OnCallEnded()`, and the prefix and postfix on `ReceiveCall(PhoneNumber, int, Action, Action)`. If a field or a hook is missing, the owner is disabled and `PhoneLines.IsAvailable` is `false`.

## Threading and safety

- Main thread only.
- `PhoneLines.IsAvailable` is `false` when the hooks or fields were not found, or the owner was disabled.
- Every member is safe at any time. With no save loaded, or on the main menu, `All` returns an empty list, `IsBusy` returns `false`, and the writers return `false`.
- Nothing throws, except `Ring` with a null `answered` callback, which throws `ArgumentNullException` as `Locks.StartLockpick` does. A null or empty id finds nothing.
- The writers return `false` while `SaveData.Busy` is true.

## Phone numbers

A number is a string of digits, with `*` for key 10 and `#` for key 11. This is the form `Phones.All()` returns. The string is turned into a `PhoneNumber` by searching `PhoneNumberDatabase.database` with the game's own `PhoneNumber.ValidateNumber`. An entry marked `notIndexed` therefore cannot be reached, as in game. An empty string, any other character, or a number that matches no entry makes the call refused.

## API

```csharp
public static class PhoneLines
{
    public static List<PhoneLineInfo> All();
    public static bool Ring(string phoneId, string number, int hangupSeconds, Action<bool> answered);
    public static bool StartCall(string phoneId, string number);
    public static bool IsBusy(string phoneId);
    public static bool IsRinging(string phoneId);
    public static string RingingNumber(string phoneId);
    public static bool Answer(string phoneId);
    public static bool Decline(string phoneId);

    public static event Action<PhoneCallArgs> CallStarted;
    public static event Action<PhoneCallArgs> CallEnded;
    public static event Action<PhoneCallArgs> Ringing;

    public static bool IsAvailable { get; }
}

public sealed class PhoneLineInfo
{
    public string Id { get; }            // the save GUID, or the scene path
    public PhoneNetwork Network { get; } // Phone.network, the same values as Phones
    public bool Busy { get; }            // the same test as IsBusy
}

public sealed class PhoneCallArgs
{
    public string Id { get; }      // save GUID, or the scene path
    public string Number { get; }  // digits of the PhoneNumber that was dialled or is ringing, "" if none
}
```

- `All()` lists every `Phone` in the active scene. It uses `FindObjectsByType`, so an inactive phone is skipped.
- `Ring(phoneId, number, hangupSeconds, answered)` calls the game's `Phone.ReceiveCall`. The phone rings and returns `true`. `answered(true)` runs if the player picks up, with the game's key or with `Answer`. `answered(false)` runs when `hangupSeconds` runs out or when `Decline` clears the ring. `hangupSeconds` counts in-game seconds on the game's `TimeOfDayAzure.Timer`, not real seconds. It returns `false` when the phone is not found, the number is unknown, a save is busy, or the game refuses the ring. The game refuses when the game menu is open, a call is active, or a call is already ringing. In that case `answered` is not called.
- `StartCall(phoneId, number)` calls the game's `Phone.StartCall`, which starts the phone's dialogue for that number. It returns `false` when the phone is not found, the number is unknown, a save is busy, a call is active or ringing, a dialogue is already running, or the phone has no conversation trigger or talk component. A missing trigger or talk component would end the call at once in the game. It returns `true` when the game's talk started. The game's talk can refuse the call privately: the player is airborne, a contest is running, a wait is in progress, the talk is disabled, or the talk's object is not active. Then the call is undone: the background static stops, `CallStarted` and `CallEnded` do not fire, and `StartCall` returns `false`. The dialogue runs in the game's dialogue system. `CallStarted` fires when the talk starts.
- `IsBusy(phoneId)` returns `true` when the game menu is open, a call is active, or a call is ringing. These are the three conditions `Phone.ReceiveCall` checks before it refuses a ring. It returns `false` for an unknown id.
- `IsRinging(phoneId)` returns `true` when the phone has an incoming call that is not yet answered or declined. It returns `false` for an unknown id.
- `RingingNumber(phoneId)` returns the digits of the ringing number, as `Phones.All()` writes them. It returns `null` when the phone is not ringing or the id is unknown.
- `Answer(phoneId)` picks up a ring. It calls the game's `Phone.Interact`, which runs the game's answer path when a ring is pending. It returns `false` without changing the phone when the phone is not found, a save is busy, the phone is not ringing, its timer is missing (`Use` and `ExpireReceivedCall` both call the timer without a null check), the phone has no conversation trigger or talk component, a call is active, the game menu is open or a dialogue is running. Otherwise it returns `true` when the ring is cleared. That is also `true` when the talk then refuses the call (see the `StartCall` bullet), because the ring is gone either way. `CallStarted` fires when the talk starts.
- `Decline(phoneId)` clears a ring without answering. It calls `Phone.ExpireReceivedCall`, so the ring stops and `answered(false)` runs, as on a timeout. It returns `false` when the phone is not found, a save is busy, the phone is not ringing, or its timer is missing. It returns `true` otherwise.
- `Ringing` fires from `Phone.ReceiveCall` when the game accepts a ring, after the ring is set. It fires for `Ring` calls too, and for rings the game starts itself. A refused ring raises nothing, and nothing is raised while a save is busy. `Number` is the digits of the ringing number.
- `CallStarted` fires from `Phone.OnCallStarted` for a call that `StartCall` set up. That includes calls the player makes on the dial pad, which reach `StartCall` through `PhoneUI.StartCallDelay`, and calls answered from a ring. A refused `StartCall` raises nothing.
- `CallEnded` fires from `Phone.OnCallEnded` for the same calls. `Number` is the digits of the `PhoneNumber` the game passed to `StartCall`. For a player call the game may have picked a message entry (not reachable, not answering, outside network), and `Number` is then that entry's number.

## Ceilings and risks

- Starting a call or ringing a phone runs the game's relays. `Phone.onCallStarted`, `onCallEnded`, `onReceivingCall` and `onReceivedCallAnsweted` fire, and quests can react to them. Ringing a phone that a quest uses can change the quest.
- `StartCall` checks whether a dialogue is running, as the game's talk does, but not the menu, because the game does not check the menu there either. Only `IsBusy` looks at the menu.
- `Ring` learns about a refusal from the game's own not-answered callback, which the game calls at once when it refuses. A later timeout calls `answered(false)`.
- The `answered` callback is lost on a save and load. The ring state (`incomingCall`, `hangupTimer`) is an `fsProperty` on `Phone`, but the callback fields are delegates, and FullSerializer skips delegate fields. So `answered` never fires for a ring that was still pending across a load.
- The number of the current call is kept until the game calls `OnCallEnded`. A call the game ends without that path keeps the number until the next `StartCall`.
- A call from `PhoneLines.StartCall` that the game's talk refuses is undone (see the `StartCall` bullet above). The player's own calls, an answered ring and the dial pad, reach the same `Phone.StartCall` without that undo. If the game refuses one of them in its talk check, the background static keeps playing and the active call stays set until the next call.
- `Answer` runs the game's answer path before any talk check: the enter relay, `Phone.onReceivedCallAnsweted`, the ringtone stop, the pickup sound and the `answered(true)` callback. If the talk then refuses the call, those have already run and the ring is cleared.
- `Ringing` is raised inside the game's `ReceiveCall`, so a handler runs while the game is still ringing the phone. Its handlers run one at a time, so one that throws does not stop the others.
- A ring restored from a save raises no `Ringing`. The only caller of `ReceiveCall` is `PhonecallRelay`, so a load does not call it. `Answer` and `Decline` still work on a restored ring, but its `answered` callback is gone (see the save bullet above).

## Save loads

The framework does not gate `CallStarted` and `CallEnded` on a save load. A handler that should ignore events raised while a save loads can check `SaveData.Busy`, which is true while a save is loading or being written. `Ringing` is not raised while a save is busy.
