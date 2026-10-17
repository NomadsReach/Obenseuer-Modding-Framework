---
title: Looks of slider characters
parent: NPC appearance
grand_parent: People
nav_order: 4
---

# Looks of slider characters

A look is a set of body and face dials plus the names of the clothes and hair worn. It is the same `PlayerLook.Look` that the player's look uses. Its dials are numbers such as `height`, `headSize` and `eyeSize`, and its worn items are names such as `hat`, `hair` and `shirtT`.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>NpcAppearance</code> <span>look calls</span></p>
<div class="api-pair" markdown="1">

```csharp
public static partial class NpcAppearance
{
    public static PlayerLook.Look GetLook(int npcId);
    public static bool SetLook(string modId, int npcId, PlayerLook.Look look);
}
```

| Method | Returns | What it does |
|---|---|---|
| `GetLook(int npcId)` | `PlayerLook.Look` | The character's current look. It is `null` if the character is not built from sliders. |
| `SetLook(string modId, int npcId, PlayerLook.Look look)` | `bool` | Applies a look. Returns `true` when it was applied. |

</div>
</div>

## Things to know

- `GetLook` gives the current look of a slider character, and `null` for any other character.
- `SetLook` applies the dials of your look, and each dial is kept inside the range the player's own look screen allows. For a worn item, a name the character does not have in its list keeps the item it is wearing now. The look's `male` and `advanced` settings are ignored, because the character keeps its own. <span class="tech">(Wearable names are checked against the choices of the NPC's current gender.)</span>
- Only one mod can change a character's look at a time. A second mod that calls `SetLook` for the same character is refused until the first mod calls `Restore` or `Clear`.
- `Restore` and `Clear` put the look back to what it was before your mod's first `SetLook` on that character. For a mod NPC that your own mod spawned, they also forget the look saved for it, so its next spawn starts from the look of the character it was copied from. See [Look and shape](../modnpcs/look-and-shape.md#look-and-shape).
