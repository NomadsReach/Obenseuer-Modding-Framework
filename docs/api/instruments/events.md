---
title: Events
parent: Instruments
grand_parent: Objects and devices
nav_order: 4
---

# Events

```csharp
public static class Instruments
{
    public static event Action<GuitarPlayedArgs> GuitarPlayed;       // a strum was played
    public static event Action<GuitarCommentArgs> GuitarCommented;   // a person nearby commented on the playing
}

public sealed class GuitarPlayedArgs    // a strum that was played
{
    public string GuitarId { get; }    // the guitar that played
}

public sealed class GuitarCommentArgs    // a comment on the playing
{
    public string WitnessId { get; }   // the id of the witness: the person who commented (see Technical below)
    public int NpcId { get; }          // the NPC id of the witness; 0 when it has none
    public bool Positive { get; }      // true when the comment is a nice one, false for a nasty one
}
```

- **`GuitarPlayed` fires each time a strum is played.** That is a Primary Use press on a held guitar that is not resting, and each strum from `PlayNote` or a song. It fires right after the game has started the sound. A strum that is skipped does not fire it.
- **`GuitarCommented` fires when the game asks a person nearby to comment on a guitar being played.** `Positive` comes from the game's check of the player's Music skill. The comment is the game's own, so it can still be skipped by the game's own timing.
  - Technical: `WitnessId` is the id of the component the game asked to comment: the witness's `NPCRandomTalk`, or its `CharacterBarkStarter` when it has no `NPCRandomTalk`. `NpcId` is `NPCController.ID`, or `0` when the witness has no `NPCController`.
- **The comment happens only while the player's own long note is playing.** A note from `HoldNote` never brings a comment. The person must be close by and able to see the player, and the game waits a while between comments.
  - Technical: the witness is within 10 units and in `Witness.GetWitnesses`, and the 10 second timer has run out. The hold is the game's own hold sound.
- **A person with no talk component still gets a plain comment.** The game gives them a short bark, and `GuitarCommented` fires for it too with the same `Positive` flag. It is reported only when it comes from the guitar, so the same words from anywhere else are not reported.
  - Technical: a witness with an `NPCRandomTalk` gets the comment through `NPCRandomTalk.OnGuitarPlayed`. One with only a `CharacterBarkStarter` gets the game's bare bark through `CharacterBarkStarter.TryToComment`.
- **An event handler that throws an error does not stop the others.** The framework logs the error and still runs the other handlers. Handlers run right away, from inside the game's own call.
