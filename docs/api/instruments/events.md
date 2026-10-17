---
title: Events
parent: Instruments
grand_parent: Objects and devices
nav_order: 4
---

# Events

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Instruments</code> <span>the guitar events</span></p>

`GuitarPlayed` and `GuitarCommented` are the events on this class. The calls that play a guitar are on the [Playing notes and songs](playing-notes-and-songs.md) page.

<div class="api-pair" markdown="1">

```csharp
public static class Instruments
{
    public static event Action<GuitarPlayedArgs> GuitarPlayed;
    public static event Action<GuitarCommentArgs> GuitarCommented;
}
```

| Event | Gives you | When it fires |
|---|---|---|
| `GuitarPlayed` | `GuitarPlayedArgs` | Each time a strum is played. |
| `GuitarCommented` | `GuitarCommentArgs` | When the game asks a person nearby to comment on a guitar being played. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>GuitarPlayedArgs</code> <span>a strum that was played</span></p>

`GuitarPlayed` gives you this, once for each strum. `GuitarId` is the guitar that played.

<div class="api-pair" markdown="1">

```csharp
public sealed class GuitarPlayedArgs
{
    public string GuitarId { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `GuitarId` | `string` | The guitar that played. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>GuitarCommentArgs</code> <span>a comment on the playing</span></p>

`GuitarCommented` gives you this, for a person nearby who comments on the player's long note. `Positive` is `true` for a nice comment.

<div class="api-pair" markdown="1">

```csharp
public sealed class GuitarCommentArgs
{
    public string WitnessId { get; }
    public int NpcId { get; }
    public bool Positive { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `WitnessId` | `string` | The id of the witness, the person who commented. |
| `NpcId` | `int` | The NPC id of the witness. `0` when it has none. |
| `Positive` | `bool` | `true` when the comment is a nice one, `false` for a nasty one. |

</div>
</div>

## Things to know

- `GuitarPlayed` fires each time a strum is played. That is a Primary Use press on a held guitar that is not resting, and each strum from `PlayNote` or a song. It fires right after the game has started the sound. A strum that is skipped does not fire it.
- `GuitarCommented` fires when the game asks a person nearby to comment on a guitar being played. `Positive` comes from the game's check of the player's Music skill. The comment is the game's own, so it can still be skipped by the game's own timing. <span class="tech">(`WitnessId` is the id of the component the game asked to comment: the witness's `NPCRandomTalk`, or its `CharacterBarkStarter` when it has no `NPCRandomTalk`. `NpcId` is `NPCController.ID`, or `0` when the witness has no `NPCController`.)</span>
- The comment happens only while the player's own long note is playing. A note from `HoldNote` never brings a comment. The person must be close by and able to see the player, and the game waits a while between comments. <span class="tech">(The witness is within 10 units and in `Witness.GetWitnesses`, and the 10 second timer has run out. The hold is the game's own hold sound.)</span>
- A person with no talk component still gets a plain comment. The game gives them a short bark, and `GuitarCommented` fires for it too with the same `Positive` flag. It is reported only when it comes from the guitar, so the same words from anywhere else are not reported. <span class="tech">(A witness with an `NPCRandomTalk` gets the comment through `NPCRandomTalk.OnGuitarPlayed`. One with only a `CharacterBarkStarter` gets the game's bare bark through `CharacterBarkStarter.TryToComment`.)</span>
- An event handler that throws an error does not stop the others. The framework logs the error and still runs the other handlers. Handlers run right away, from inside the game's own call.
