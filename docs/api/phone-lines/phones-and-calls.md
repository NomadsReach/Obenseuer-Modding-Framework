---
title: Phones and calls
parent: Phone lines
grand_parent: Phones, mail and media
nav_order: 3
---

# Phones and calls

A phone line is one phone in the scene. `PhoneLines` lists them, rings them, starts calls, and answers or turns down rings.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>PhoneLines</code> <span>rings, calls and events</span></p>
<div class="api-pair" markdown="1">

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
```

| Method | Returns | What it does |
|---|---|---|
| `All()` | `List<PhoneLineInfo>` | Every phone that is switched on in the scene you are in. |
| `Ring(string phoneId, string number, int hangupSeconds, Action<bool> answered)` | `bool` | Makes a phone ring, as if the number were calling. Returns `true` when the phone rings. |
| `StartCall(string phoneId, string number)` | `bool` | Starts a call from a phone, as if the player had dialled the number. Returns `true` when the call started. |
| `IsBusy(string phoneId)` | `bool` | `true` when the phone cannot take a new ring. |
| `IsRinging(string phoneId)` | `bool` | `true` when the phone is ringing and nobody has answered. |
| `RingingNumber(string phoneId)` | `string` | The digits of the number that is ringing. `null` when it is not ringing. |
| `Answer(string phoneId)` | `bool` | Picks up a ringing phone. Returns `true` when the ring is gone. |
| `Decline(string phoneId)` | `bool` | Turns a ring down without answering. Returns `true` when it worked. |

| Property | Type | What it means |
|---|---|---|
| `IsAvailable` | `bool` | `false` when the phone events could not be set up. |

| Event | Gives you | When it fires |
|---|---|---|
| `CallStarted` | `PhoneCallArgs`: the phone's id and the number. | A call has just started on a phone. |
| `CallEnded` | `PhoneCallArgs`: the phone's id and the number. | A call has just ended on a phone. |
| `Ringing` | `PhoneCallArgs`: the phone's id and the number. | A phone has just started to ring. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>PhoneLineInfo</code> <span>a snapshot of one phone</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class PhoneLineInfo
{
    public string Id { get; }
    public PhoneNetwork Network { get; }
    public bool Busy { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | The phone's id: its save id, or its place in the scene if it has none. |
| `Network` | `PhoneNetwork` | Which phone network the phone is on. The same values as in [Phones](../phones.md). |
| `Busy` | `bool` | The same answer as `IsBusy`. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>PhoneCallArgs</code> <span>what the three events tell you</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class PhoneCallArgs
{
    public string Id { get; }
    public string Number { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | The id of the phone. |
| `Number` | `string` | The digits of the number that was dialled or is ringing. `""` if there is none. |

</div>
</div>

## Things to know

- `All()` only lists the phones that are switched on in the scene you are in. A phone that is switched off is skipped. <span class="tech">(It uses `FindObjectsByType`, so an inactive phone is skipped. The id is the object's save GUID (see the [glossary](../../glossary.md#guid)), or its scene path when it has none.)</span>
- `Ring` makes the phone ring, as if the number were calling.
- Your `answered` code runs once the ring ends. It runs with `true` if the player picks up (with the key in the game or with `Answer`). It runs with `false` when the ring times out or `Decline` turns it down. `hangupSeconds` counts in-game seconds, not real seconds. <span class="tech">(It is counted on `TimeOfDayAzure.Timer`.)</span>
- `StartCall` starts a call from the phone, as if the player had dialled the number. It starts the phone's conversation for that number.
- `IsBusy` is `true` when the game menu is open, a call is going on, or the phone is ringing. These are the three things the game checks before it refuses a ring. It is `false` for an unknown id.
- The menu check applies only to `IsBusy`. `StartCall` checks whether a conversation is running, as the game's talk does, but not the menu, because the game does not check the menu there either.
- `IsRinging` is `true` when the phone has a call coming in that nobody has answered or turned down. It is `false` for an unknown id.
- `RingingNumber` gives the digits of the caller, written the way `Phones.All()` writes them. It gives `null` when the phone is not ringing or the id is unknown.
- `Answer` picks up a ringing phone. It returns `true` when the ring is gone.
- `Decline` turns a ring down without answering. The ring stops, and your `answered(false)` code runs, the same as on a timeout. Otherwise it returns `true`. <span class="tech">(It calls `Phone.ExpireReceivedCall`.)</span>
- Calls and rings run the game's own signals, and quests can react to them. Ringing a phone that a quest uses can change that quest. <span class="tech">(`Phone.onCallStarted`, `onCallEnded`, `onReceivingCall` and `onReceivedCallAnsweted` fire.)</span>

## Limits

- `Ring` returns `false` when the phone is not found, the number is not in the phone book, a save is loading or saving, or the game refuses. <span class="tech">(It calls the game's `Phone.ReceiveCall`. It learns about a refusal from the game's own not-answered callback, which the game calls at once when it refuses.)</span>
- The game refuses when the game menu is open, a call is going on, or the phone is already ringing. When it refuses, your `answered` code is not called.
- `Ring` throws an `ArgumentNullException` if `answered` is `null`, the same as `Locks.StartLockpick` does.
- Your `answered` code is lost when the game is saved and loaded. A ring that was still going when the player saved still exists after the load, and `Answer` and `Decline` still work on it, but your code never runs for it. <span class="tech">(The ring state (`incomingCall`, `hangupTimer`) is an `fsProperty` on `Phone`, but the callback fields are delegates, and the game's save system (FullSerializer) skips delegate fields.)</span>
- `StartCall` returns `false` when the phone is not found, the number is not in the phone book, a save is loading or saving, a call is going on or ringing, a conversation is already running, or the phone is missing the parts it needs to talk. <span class="tech">(It calls the game's `Phone.StartCall`. The phone must have its conversation trigger (`conversationTrigger`) and talk component (`interactableTalk`), or the game would end the call at once.)</span>
- The game's talk can still turn a call down after `StartCall`. That happens when the player is in the air, a contest is running, the player is waiting, the talk is switched off, or the talk's object is not active. Then the framework undoes the call: the background static stops, `CallStarted` and `CallEnded` do not fire, and `StartCall` returns `false`.
- The player's own calls are not undone like that. A call the player dials, or answers from a ring, goes through the same game method without the undo. If the game turns one of them down in its talk check, the background static keeps playing and the call stays marked as active until the next call.
- `Answer` returns `false`, and leaves the phone alone, when the phone is not found, a save is loading or saving, the phone is not ringing, the phone has no hang-up timer, the phone has no talk parts, a call is going on, the game menu is open, or a conversation is running. <span class="tech">(It calls the game's `Phone.Interact`, which runs the game's answer path when a ring is pending. `Phone.Use` and `ExpireReceivedCall` both use the hang-up timer (`hangupTimer`) without a null check, so a missing timer makes `Answer` refuse.)</span>
- `Answer` does the game's answer steps before the talk can turn the call down. The ring's enter signal, the "answered" signal, the ringtone stop, the pickup sound and your `answered(true)` code have all run by then. If the talk then refuses the call, those steps have already happened and the ring is gone. `Answer` still returns `true`, because the ring is gone either way. <span class="tech">(The signals are `Phone.onReceivedCallAnsweted` and the enter relay.)</span>
- `Decline` returns `false` when the phone is not found, a save is loading or saving, the phone is not ringing, or its hang-up timer is missing.
- Every call that changes a phone returns `false` while a save is loading or saving. <span class="tech">(It checks `SaveData.Busy`.)</span>
