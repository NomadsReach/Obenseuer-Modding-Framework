---
title: Text callbacks
parent: Dialogue authoring
grand_parent: Story
nav_order: 5
---

# Text callbacks

`ConversationLine.TextCallback` sets a line's text from a function. The game calls it when the line shows, so the text can change from one visit to the next. It works for a conversation from `DialogueAuthoring.Add` and for a branch.

- **The callback returns the text.** If it returns `null`, the line shows `Text` instead. If it throws an error, the line shows nothing and the error is logged once.
- **A returned text over 1000 characters, or with a control character, is replaced by `Text`.** The framework logs it once.
- **When `TextCallback` is set, `Text` may be `null`.** Without a callback, `Text` is required.
- **The callback can run several times for one showing of a line, so it must have no side effects.** While the game works out which lines come next, it reads each character reply it considers, and it reads a player choice once for the menu and again for the subtitle.
- **The callback runs while the conversation is open, from the game's normal code, so keep it short.** Do not start a conversation or write to the save from it.
- **The returned text is read as dialogue markup, like written text.** `[var=Name]`, emphasis tags such as `[em1]`, and `|` as a line break (on by default in the game's subtitle settings) all work.
  - Technical: "dialogue markup" is the Dialogue System's own text markup.
- **Removing a conversation drops its callbacks.** `Remove` drops those of that conversation, and `RemoveBranch` drops those of its branch. `DialogueAuthoring.Clear` drops those of each conversation it removes, and `Dialogue.RemoveAll` drops those of the branches it removes. `Dialogue.RemoveAll` does not remove conversations made with `DialogueAuthoring.Add`, so use `Clear` for those.
