---
title: Phones and calls
parent: Phone lines
grand_parent: Phones, mail and media
nav_order: 3
---

# Phones and calls

```csharp
public static class PhoneLines
{
    public static List<PhoneLineInfo> All();                                                         // every phone in the scene you are in
    public static bool Ring(string phoneId, string number, int hangupSeconds, Action<bool> answered);  // makes a phone ring; true when it rings
    public static bool StartCall(string phoneId, string number);                                     // starts a call from a phone; true when it started
    public static bool IsBusy(string phoneId);                                                       // true when the phone cannot take a new ring
    public static bool IsRinging(string phoneId);                                                    // true when the phone is ringing and nobody has answered
    public static string RingingNumber(string phoneId);                                              // the digits of the number that is ringing; null when it is not ringing
    public static bool Answer(string phoneId);                                                       // picks up a ringing phone; true when the ring is gone
    public static bool Decline(string phoneId);                                                      // turns a ring down without answering; true when it worked

    public static event Action<PhoneCallArgs> CallStarted;   // a call has just started on a phone
    public static event Action<PhoneCallArgs> CallEnded;     // a call has just ended on a phone
    public static event Action<PhoneCallArgs> Ringing;       // a phone has just started to ring

    public static bool IsAvailable { get; }                  // false when the phone events could not be set up
}

public sealed class PhoneLineInfo    // a snapshot of one phone
{
    public string Id { get; }            // the phone's id: its save id, or its place in the scene if it has none
    public PhoneNetwork Network { get; } // which phone network the phone is on (the same values as in Phones)
    public bool Busy { get; }            // the same answer as IsBusy
}

public sealed class PhoneCallArgs    // what the three events tell you
{
    public string Id { get; }      // the id of the phone
    public string Number { get; }  // the digits of the number that was dialled or is ringing; "" if there is none
}
```

- **`All()` lists the phones that are switched on in the scene you are in.** A phone that is switched off in the scene is not listed.
  - Technical: it uses `FindObjectsByType`, so an inactive phone is skipped. The id is the object's save GUID (see the [glossary](../../glossary.md#guid)), or its scene path when it has none.
- **`Ring` makes the phone ring, as if the number were calling.** It returns `true` when the phone rings. It returns `false` when the phone is not found, the number is not in the phone book, a save is loading or saving, or the game refuses. The game refuses when the game menu is open, a call is going on, or the phone is already ringing. When it refuses, your `answered` code is not called.
  - Technical: it calls the game's `Phone.ReceiveCall`. It learns about a refusal from the game's own not-answered callback, which the game calls at once when it refuses.
- **Your `answered` code runs once the ring ends.** It runs with `true` if the player picks up (with the key in the game or with `Answer`). It runs with `false` when the ring times out or `Decline` turns it down. `hangupSeconds` counts in-game seconds, not real seconds.
  - Technical: it is counted on `TimeOfDayAzure.Timer`. `Ring` throws an `ArgumentNullException` if `answered` is `null`, the same as `Locks.StartLockpick` does.
- **Your `answered` code is lost when the game is saved and loaded.** A ring that was still going when the player saved still exists after the load, and `Answer` and `Decline` still work on it, but your code never runs for it.
  - Technical: the ring state (`incomingCall`, `hangupTimer`) is an `fsProperty` on `Phone`, but the callback fields are delegates, and the game's save system (FullSerializer) skips delegate fields.
- **`StartCall` starts a call from the phone, as if the player had dialled the number.** It starts the phone's conversation for that number. It returns `true` when the call started. It returns `false` when the phone is not found, the number is not in the phone book, a save is loading or saving, a call is going on or ringing, a conversation is already running, or the phone is missing the parts it needs to talk.
  - Technical: it calls the game's `Phone.StartCall`. The phone must have its conversation trigger (`conversationTrigger`) and talk component (`interactableTalk`), or the game would end the call at once.
- **The game's talk can still turn a call down after `StartCall`.** That happens when the player is in the air, a contest is running, the player is waiting, the talk is switched off, or the talk's object is not active. Then the framework undoes the call: the background static stops, `CallStarted` and `CallEnded` do not fire, and `StartCall` returns `false`.
- **The player's own calls are not undone like that.** A call the player dials, or answers from a ring, goes through the same game method without the undo. If the game turns one of them down in its talk check, the background static keeps playing and the call stays marked as active until the next call.
- **`IsBusy` is `true` when the game menu is open, a call is going on, or the phone is ringing.** These are the three things the game checks before it refuses a ring. It is `false` for an unknown id. The menu check applies only to `IsBusy`: `StartCall` checks whether a conversation is running, as the game's talk does, but not the menu, because the game does not check the menu there either.
- **`IsRinging` is `true` when the phone has a call coming in that nobody has answered or turned down.** It is `false` for an unknown id. `RingingNumber` gives the digits of the caller, written the way `Phones.All()` writes them. It gives `null` when the phone is not ringing or the id is unknown.
- **`Answer` picks up a ringing phone.** It returns `true` when the ring is gone. It returns `false`, and leaves the phone alone, when the phone is not found, a save is loading or saving, the phone is not ringing, the phone has no hang-up timer, the phone has no talk parts, a call is going on, the game menu is open, or a conversation is running.
  - Technical: it calls the game's `Phone.Interact`, which runs the game's answer path when a ring is pending. `Phone.Use` and `ExpireReceivedCall` both use the hang-up timer (`hangupTimer`) without a null check, so a missing timer makes `Answer` refuse.
- **`Answer` does the game's answer steps before the talk can turn the call down.** The ring's enter signal, the "answered" signal, the ringtone stop, the pickup sound and your `answered(true)` code have all run by then. If the talk then refuses the call, those steps have already happened and the ring is gone. `Answer` still returns `true`, because the ring is gone either way.
  - Technical: the signals are `Phone.onReceivedCallAnsweted` and the enter relay.
- **`Decline` turns a ring down without answering.** The ring stops, and your `answered(false)` code runs, the same as on a timeout. It returns `false` when the phone is not found, a save is loading or saving, the phone is not ringing, or its hang-up timer is missing. Otherwise it returns `true`.
  - Technical: it calls `Phone.ExpireReceivedCall`.
- **Calls and rings run the game's own signals, and quests can react to them.** Ringing a phone that a quest uses can change that quest.
  - Technical: `Phone.onCallStarted`, `onCallEnded`, `onReceivingCall` and `onReceivedCallAnsweted` fire.
- **Every call that changes a phone returns `false` while a save is loading or saving.**
  - Technical: it checks `SaveData.Busy`.
