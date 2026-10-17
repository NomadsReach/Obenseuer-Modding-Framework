---
title: Speaking
parent: NPC actions
grand_parent: People
nav_order: 5
---

# Speaking

This page uses `Transform` from Unity. The [glossary](../../glossary.md#types-that-come-from-unity) says what each one is.
{: .unity-types}

These calls make an NPC show your text, say one of the game's lines, or start one of the game's conversations.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>NpcActions</code> <span>making an NPC speak</span></p>

Call `Say` to show your own line above the NPC, `Comment` to make it say one of the game's lines, or `StartDialogue` to start one of the game's conversations.

<div class="api-pair" markdown="1">

```csharp
public static class NpcActions
{
    public static bool Say(int npcId, string text);
    public static bool Comment(int npcId, string conversationTitle);
    public static bool StartDialogue(int npcId, string conversationTitle);
}
```

| Method | Returns | What it does |
|---|---|---|
| `Say(npcId, text)` | `bool` | Shows a line of text above the NPC. |
| `Comment(npcId, conversationTitle)` | `bool` | Makes the NPC say one of the game's lines. |
| `StartDialogue(npcId, conversationTitle)` | `bool` | Starts one of the game's conversations. |

</div>
</div>

## Things to know

- `Say` shows your own text above the NPC's head, as a subtitle with the NPC's name. It is not a conversation. The text goes through the game's language system and its text markup, so special markup in your text is read as markup. <span class="tech">(It uses the Dialogue System bark path (`DialogueManager.BarkString`). The text goes through localisation and `FormattedText`.)</span>
- `Say` gives `false` when it cannot ask for the line. That is an empty text, a conversation that is running, a game with no dialogue system, no subtitle screen, or an NPC whose speech display is missing or switched off.
- If you use an NPC the same frame it spawns, wait one frame before `Say`. The NPC's speech display is started by the call, and a brand-new NPC may not be ready. <span class="tech">(`Say` starts the NPC's `SubtitleBarkUI` first, because the game's bark needs it.)</span>
- `Comment` makes the NPC say one of the game's existing lines, by its title. The title must be in the game's dialogue database, or it gives `false`. <span class="tech">(It calls `NPCInfo.TryComment`, which asks the NPC's `CharacterBarkStarter` to bark that conversation.)</span>
- `Comment` replaces the line the NPC normally says, until the game restarts. The NPC's own comment does not come back this session. An NPC that is set to speak only once may remove its speech component after the line. <span class="tech">(`CharacterBarkStarter.TryToComment` writes the `conversation` field. `Hit` does the same.)</span>
- `StartDialogue` starts a conversation the game already has, with a live NPC, as if the player had pressed the use key on it. The NPC's triggers and conditions run, and the player's stats are set inactive, as in the game. It starts conversations by title only. It does not create new ones, and the title must exist or you get `false`. <span class="tech">(It goes through the NPC's talk path (`InteractableTalk.CheckStartDialogue`). The title is checked with `Dialogue.Exists`. `Dialogue.Start` does the same for a conversant you already hold as a `Transform`.)</span>
- `StartDialogue` gives `true` only when a conversation is really running afterwards. It gives `false` when the title does not exist, a conversation is already running, the NPC is not in the scene, or the NPC cannot talk. <span class="tech">(It returns `true` when `Dialogue.IsActive` is true after the call. The game's own return value is not used, because the game returns `true` even when no conversation started. The game's `DelayCheck` closes a talk that has no conversation after about a second.)</span>

## Limits

- `Say` gives `true` when the line was asked for, which does not promise that it was shown. The line is spoken later, and the game can still drop it, for example when a more important line is already playing for that speaker, or when the game's bark settings block lines during a conversation.
- `Comment` gives `false` in these cases. The NPC's speech component is switched off (the game does this when an NPC dies), the NPC keeps its lines ready in advance, another mod is holding the NPC's barks through [NPC control](../npc-control.md), or a conversation is running and the NPC is not set to speak during one. <span class="tech">(The checks are `starter.enabled`, `starter.cacheBarkLines` (the game would say the cached conversation instead of your title), `NpcControl.HoldsBarks(npcId)`, and `starter.allowDuringConversations`.)</span>
- `Comment` also gives `true` when the line was asked for, not when it was shown. The NPC's own condition can turn it down, or the NPC can be in the middle of another line, and then the line is dropped without a message.
- The game can refuse the talk by its own rules. It refuses for a sleeping NPC, an NPC with talking turned off, a contest, a player who is not on the ground, or a waiting screen. The game may show its warning text when it does. <span class="tech">(The flags are `talkDisabled`, a contest in progress, the player not grounded, and the waiting screen.)</span>
- `StartDialogue` does not check a few things, so check them yourself. It starts the talk even over an open menu or panel. It does not look at quest state, so starting a quest conversation out of order is up to you. It does not check whether the NPC is friendly or how far away it is.
