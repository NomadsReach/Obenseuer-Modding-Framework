using System;
using PCDS = PixelCrushers.DialogueSystem;

namespace ObenseuerCore
{
    internal static class DialogueAuthoringSelfTest
    {
        private const string Mod = "obenseuer.moddingframework";

        internal static string Run()
        {
            try
            {
                ConversationDefinition definition = new ConversationDefinition();
                definition.Key = "c";
                definition.Lines.Add(Line("hello", false, "yes", "bye"));
                definition.Lines.Add(Line("yes", true, "bye"));
                definition.Lines.Add(Line("bye", false));
                if (DialogueAuthoring.Check(Mod, definition) != ConversationRefusal.None) return "a valid conversation was refused";
                PCDS.Conversation built = DialogueAuthoring.Build("t", 1000005, 1, 2, definition);
                if (built.dialogueEntries.Count != 4 || built.id != 1000005 || built.ActorID != 1 || built.ConversantID != 2) return "conversation header wrong";
                PCDS.DialogueEntry start = built.GetFirstDialogueEntry();
                if (start == null || start.id != 0 || start.outgoingLinks[0].destinationDialogueID != 1) return "START entry wrong";
                PCDS.DialogueEntry hello = built.GetDialogueEntry("hello");
                PCDS.DialogueEntry yes = built.GetDialogueEntry("yes");
                if (hello.ActorID != 2 || hello.ConversantID != 1 || yes.ActorID != 1 || yes.ConversantID != 2) return "speakers wrong";
                if (hello.outgoingLinks.Count != 2 || hello.outgoingLinks[0].destinationDialogueID != yes.id || hello.outgoingLinks[0].destinationConversationID != 1000005) return "links wrong";
                if (built.GetDialogueEntry("bye").outgoingLinks.Count != 0 || hello.DialogueText != "text hello") return "entry text or end wrong";
                definition.Lines[0].Next = new[] { "missing" };
                if (DialogueAuthoring.Check(Mod, definition) != ConversationRefusal.BadInput) return "a link to a missing line was accepted";
                if (DialogueAuthoring.Check("bad id", definition) != ConversationRefusal.BadInput) return "a bad mod id was accepted";
                int id = DialogueAuthoring.StableId("a.b");
                if (id != DialogueAuthoring.StableId("a.b") || id < 1000000 || id >= 2000000) return "stable id wrong";
                if (DialogueAuthoring.TitleOf(Mod, "c") != Mod + ".c" || DialogueAuthoring.TitleOf(Mod, "c.d") != null) return "title wrong";
            }
            catch (Exception e)
            {
                return "exception: " + e.Message;
            }
            return "dialogue authoring self-check ok";
        }

        private static ConversationLine Line(string key, bool player, params string[] next)
        {
            return new ConversationLine { Key = key, Player = player, Text = "text " + key, Next = next };
        }
    }
}
