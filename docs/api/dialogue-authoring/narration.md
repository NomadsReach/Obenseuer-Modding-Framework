---
title: Narration
parent: Dialogue authoring
grand_parent: Story
nav_order: 6
---

# Narration

`Dialogue.Narrate(modId, text)` shows `text` in the game's dialogue box on the game's black screen. This part is experimental.

```csharp
public static class Dialogue
{
    public static bool Narrate(string modId, string text);   // shows narration on a black screen; true when a conversation is running after the call
}
```

It is built on this API with one conversation for each mod. The key is `narration`, so the title is your mod id, a dot, and `narration`. Each call replaces that conversation's text, then starts it through the game's black screen.

- **`Narrate` returns `true` when a conversation is running after the call.** That can be a different conversation, when a handler from `Dialogue.Intercept` redirects the start.
- **`Narrate` returns `false` when it did not start.** That happens when the text is `null`, empty, only spaces, over 1000 characters, or has a control character other than a line break (`\n` or `\r`). It also happens when the dialogue system is not available or not ready, a save is loading or being written, a conversation is already running, the black screen is not there, or the player is waiting or asleep. It also returns `false` when `DialogueAuthoring.Add` refuses the conversation, for example when the game has no player character.
- **Do not call it from a wake-up callback while the sleep text is still waiting to show.** Starting a conversation wakes the player and cuts that text off, so the call is refused.
  - Technical: the check is that `WaitingController.eventDialogue` is empty.
- **The screen starts to fade to black at once, before the text shows.**
- **When the conversation ends, the framework fades the screen back.** If the conversation does not start, the framework fades the screen back at once and `Narrate` returns `false`. If the start throws an error after the conversation has begun, `Narrate` returns `true` and the fade back happens when the conversation ends.
  - Technical: it uses the end-of-talk callback on the black screen's talk component (`BlackCanvas.instance.interactableTalk.onEndTalkCallback`). The framework keeps any callback already set there and runs it after the fade.
- **An invalid `modId` throws an `ArgumentException`.**
- **The key `narration` belongs to `Narrate`.** Each call first removes your own conversation with that key. `Add` and `AddBranch` refuse the key `narration` (`ReservedKey`), and the refusal is logged once.
