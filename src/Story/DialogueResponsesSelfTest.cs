using System;
using System.Collections.Generic;
using PCDS = PixelCrushers.DialogueSystem;

namespace ObenseuerCore
{
    internal static class DialogueResponsesSelfTest
    {
        internal static string Run()
        {
            try
            {
                List<PCDS.DialogueEntry> entries = new List<PCDS.DialogueEntry>();
                entries.Add(Entry(1, "Any work?"));
                entries.Add(Entry(2, "Any work?"));
                entries.Add(Entry(3, "Bye"));
                entries.Add(Entry(4, " bye"));
                entries.Add(Entry(5, "Hi there"));
                if (DialogueResponses.FindAnchor(entries, "Any work?") != null) return "an ambiguous exact anchor matched";
                if (DialogueResponses.FindAnchor(entries, " any WORK? ") != null) return "an ambiguous loose anchor matched";
                if (DialogueResponses.FindAnchor(entries, "BYE") != null) return "an ambiguous loose anchor matched";
                if (DialogueResponses.FindAnchor(entries, "Hi") != null || DialogueResponses.FindAnchor(entries, " ") != null) return "a missing anchor matched";
                if (DialogueResponses.FindAnchor(entries, "Bye").id != 3) return "exact anchor match wrong";
                if (DialogueResponses.FindAnchor(entries, "  HI THERE ").id != 5) return "trimmed anchor match wrong";
                if (DialogueResponses.AnchorMatchCount(entries, "Any work?") != 2) return "anchor match count wrong";
                if (DialogueResponses.AnchorMatchCount(entries, "BYE") != 2) return "loose anchor match count wrong";
                if (DialogueResponses.AnchorMatchCount(entries, "Hi there") != 1) return "an exact anchor match count wrong";
                if (DialogueResponses.AnchorMatchCount(entries, "Hi") != 0 || DialogueResponses.AnchorMatchCount(entries, " ") != 0) return "a missing anchor match count wrong";
                if (DialogueAuthoring.FreeIdFrom(1000005, id => id == 1000005 || id == 1000006) != 1000007) return "id allocation skipped a used id wrong";
                if (DialogueAuthoring.FreeIdFrom(1999999, id => id == 1999999) != 1000000) return "id allocation did not wrap";
                string key = DialogueResponses.KeyFor("a.b", "conv", 7, "Yes", "Sure", null);
                if (key != DialogueResponses.KeyFor("a.b", "conv", 7, "Yes", "Sure", null) || key == DialogueResponses.KeyFor("a.b", "conv", 7, "No", "Sure", null) || !key.StartsWith("a.b.", StringComparison.Ordinal)) return "response key wrong";
                if (DialogueResponses.InsertIndex(-1, 3) != 3) return "the default insert position was not the end";
                if (DialogueResponses.InsertIndex(0, 3) != 0) return "the first insert position was not kept";
                if (DialogueResponses.InsertIndex(2, 3) != 2) return "an inner insert position was not kept";
                if (DialogueResponses.InsertIndex(3, 3) != 3) return "an insert at the end was not kept";
                if (DialogueResponses.InsertIndex(9, 3) != 3) return "an insert past the end was not clamped";
                if (DialogueResponses.InsertIndex(0, 0) != 0) return "an insert into an empty link list was wrong";
                if (!DialogueResponses.ValidPriority(-1) || !DialogueResponses.ValidPriority(0) || !DialogueResponses.ValidPriority(4)) return "a valid priority was refused";
                if (DialogueResponses.ValidPriority(-2) || DialogueResponses.ValidPriority(5)) return "an invalid priority was accepted";
                if (DialogueResponses.RequestedPriority(-1, PCDS.ConditionPriority.High) != PCDS.ConditionPriority.High) return "the anchor priority was not the default";
                if (DialogueResponses.RequestedPriority(0, PCDS.ConditionPriority.High) != PCDS.ConditionPriority.Low) return "a requested priority was not used";
            }
            catch (Exception e)
            {
                return "exception: " + e.Message;
            }
            return "dialogue responses self-check ok";
        }

        private static PCDS.DialogueEntry Entry(int id, string text)
        {
            PCDS.DialogueEntry entry = PCDS.Template.FromDefault().CreateDialogueEntry(id, 1, "entry " + id);
            entry.DialogueText = text;
            return entry;
        }
    }
}
