---
title: Events
parent: Phone lines
grand_parent: Phones, mail and media
nav_order: 4
---

# Events

These events are on `PhoneLines`, which is described on [Phones and calls](phones-and-calls.md).

## Things to know

- `Ringing` fires when the game accepts a ring, right after the ring is set. It fires for rings from your `Ring` call and for rings the game starts itself. `Number` is the digits of the caller. It fires while the game is still ringing the phone, so keep your handler short. <span class="tech">(It is raised inside the game's `ReceiveCall`, after the ring is set.)</span>
- `CallStarted` fires when a call starts on a phone. That includes calls the player makes on the dial pad and calls answered from a ring. <span class="tech">(It is raised from `Phone.OnCallStarted`. Dial-pad calls reach `StartCall` through `PhoneUI.StartCallDelay`.)</span>
- `CallEnded` fires when the same call ends. `Number` is the digits of the number the game used to start the call. For a call the player dialled, the game may have picked a message entry instead (for example "not reachable", "not answering" or "outside the network"), and then `Number` is that entry's number. <span class="tech">(It is raised from `Phone.OnCallEnded`. `Number` is the digits of the `PhoneNumber` the game passed to `StartCall`.)</span>
- The framework keeps the number of the current call until the game ends it. If the game ends a call another way, the number stays until the next `StartCall`. <span class="tech">(It is cleared in `Phone.OnCallEnded`.)</span>
- `IsAvailable` is `false` when the phone events could not be set up. See "If something fails to set up" in [Details](details.md).

## Limits

- A refused ring raises nothing, and nothing is raised while a save is loading or saving.
- A ring that comes back from a save raises no `Ringing`. The game does not call its ring method when a save loads. <span class="tech">(The only caller of `ReceiveCall` is `PhonecallRelay`, so a load does not call it.)</span>
- A call that `StartCall` set up and the game then turned down raises nothing.
- `CallStarted` and `CallEnded` are not stopped while a save is loading. If your handler should ignore events raised while a save loads, check `SaveData.Busy` (true while a save is loading or being written).
