---
title: Common tasks
parent: Dialogue authoring
grand_parent: Story
nav_order: 1
---

# Common tasks

Every example on this page is a whole mod. Replace the mod id, the NPC name and the keys with your own. Each example checks the call that can refuse and logs the reason, so a warning in the log points at the call that did not work.

Start a conversation you added. The NPC is found by its name, and the key starts the conversation with that NPC. The conversation is added the first time a save loads, or a new game starts, in a game session, because `DialogueAuthoring.Add` refuses a title that already exists. To cover a later save load in the same session, run the same method from `Events.Loaded` as well, which is safe because `Dialogue.Exists` is checked first:

```csharp
using ObenseuerCore;
using UnityEngine;

[ModInfo("example.talkstarter", "Talk Starter", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class TalkStarter : FrameworkMod
{
    // The name of an NPC in the scene. Use the name of a character in your save.
    private const string NpcName = "Replace with an NPC name";
    private ModKeybind talkKey;

    protected override void OnLoad()
    {
        talkKey = Mod.Key("Talk", "Start the hello conversation", KeyCode.F6);
        Events.WhenReady(Add);
    }

    private void Add()
    {
        if (Dialogue.Exists(DialogueAuthoring.TitleOf(Mod.Id, "hello"))) return;
        ConversationDefinition talk = new ConversationDefinition { Key = "hello" };
        talk.Lines.Add(new ConversationLine { Key = "hi", Text = "Hello there." });
        ConversationRefusal refusal;
        if (DialogueAuthoring.Add(Mod.Id, talk, out refusal) == null) Log.Warn("The conversation was refused: " + refusal);
    }

    private void Update()
    {
        if (UiState.IsMenuOpen || UiState.IsPaused || UiState.IsInputBlocked) return;
        if (talkKey == null || !talkKey.Down()) return;
        NpcInfo npc = Npcs.Find(NpcName);
        if (npc == null || !NpcActions.StartDialogue(npc.Id, DialogueAuthoring.TitleOf(Mod.Id, "hello")))
            Log.Warn("The conversation did not start. Check NpcName and that the NPC is in the scene.");
    }
}
```

Remove a conversation you added, and everything else your mod added to the dialogue system. F10 removes the `remove_demo` conversation, and F11 removes every conversation and branch the mod added:

```csharp
using ObenseuerCore;
using UnityEngine;

[ModInfo("example.talkremover", "Talk Remover", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class TalkRemover : FrameworkMod
{
    private ModKeybind removeKey;
    private ModKeybind clearKey;

    protected override void OnLoad()
    {
        removeKey = Mod.Key("Remove", "Remove the demo conversation", KeyCode.F10);
        clearKey = Mod.Key("Clear", "Remove all of this mod's conversations", KeyCode.F11);
        Events.WhenReady(Add);
    }

    private void Add()
    {
        if (Dialogue.Exists(DialogueAuthoring.TitleOf(Mod.Id, "remove_demo"))) return;
        ConversationDefinition talk = new ConversationDefinition { Key = "remove_demo" };
        talk.Lines.Add(new ConversationLine { Key = "hi", Text = "This line can be removed." });
        ConversationRefusal refusal;
        if (DialogueAuthoring.Add(Mod.Id, talk, out refusal) == null) Log.Warn("The conversation was refused: " + refusal);
    }

    private void Update()
    {
        if (UiState.IsMenuOpen || UiState.IsPaused || UiState.IsInputBlocked) return;
        if (removeKey != null && removeKey.Down() && !DialogueAuthoring.Remove(Mod.Id, "remove_demo")) Log.Warn("The conversation was not removed, it may be running now");
        if (clearKey != null && clearKey.Down()) Log.Info("Removed " + DialogueAuthoring.Clear(Mod.Id) + " conversations and branches");
    }
}
```

Add a new answer to a conversation. This example adds the answer to a conversation the mod made, so it runs without a game title. For a game conversation, pass its title and the text of one of its character lines in the same way, and the title and the text must match what the game has:

```csharp
using ObenseuerCore;

[ModInfo("example.ropeanswer", "Rope Answer", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class RopeAnswer : FrameworkMod
{
    private const string Key = "rope_demo";

    protected override void OnLoad()
    {
        Events.WhenReady(Add);
    }

    private void Add()
    {
        string title = DialogueAuthoring.TitleOf(Mod.Id, Key);
        if (!Dialogue.Exists(title))
        {
            ConversationDefinition talk = new ConversationDefinition { Key = Key };
            talk.Lines.Add(new ConversationLine { Key = "ask", Text = "Need anything today?" });
            ConversationRefusal refusal;
            if (DialogueAuthoring.Add(Mod.Id, talk, out refusal) == null)
            {
                Log.Warn("The conversation was refused: " + refusal);
                return;
            }
        }
        string key = Dialogue.AddResponse(Mod.Id, title, "Need anything today?", "Do you have any rope?", "Second shelf, by the door.", null, () => Dialogue.SetVariable("rope_asked", true));
        if (key == null) Log.Warn("The answer was refused, so the title or the text may not match");
    }
}
```

Show narration on the black screen. F8 shows the line, and the screen fades back when it ends:

```csharp
using ObenseuerCore;
using UnityEngine;

[ModInfo("example.narrationdemo", "Narration Demo", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class NarrationDemo : FrameworkMod
{
    private ModKeybind narrateKey;

    protected override void OnLoad()
    {
        narrateKey = Mod.Key("Narrate", "Show the narration", KeyCode.F8);
    }

    private void Update()
    {
        if (UiState.IsMenuOpen || UiState.IsPaused || UiState.IsInputBlocked) return;
        if (narrateKey == null || !narrateKey.Down()) return;
        if (!Dialogue.Narrate(Mod.Id, "Three days later...")) Log.Warn("Narration could not start now.");
    }
}
```
