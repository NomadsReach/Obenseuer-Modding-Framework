using System;
using System.Collections.Generic;
using UnityEngine;
using PCDS = PixelCrushers.DialogueSystem;

namespace ObenseuerCore
{
    public enum ConversationRefusal
    {
        None,
        NotReady,
        SaveBusy,
        BadInput,
        AlreadyExists,
        UnknownSpeaker,
        NoPlayerActor,
        Failed
    }

    public sealed class ConversationLine
    {
        public string Key { get; set; }
        public bool Player { get; set; }
        public string Text { get; set; }
        public string Condition { get; set; }
        public string Script { get; set; }
        public string[] Next { get; set; }
    }

    public sealed class ConversationDefinition
    {
        public string Key { get; set; }
        public string Speaker { get; set; }
        public List<ConversationLine> Lines { get; set; }

        public ConversationDefinition()
        {
            Lines = new List<ConversationLine>();
        }
    }

    public static class DialogueAuthoring
    {
        internal const string Owner = "obenseuer.moddingframework.dialogueauthoring";
        internal const int MaxLines = 200;
        internal const int MaxText = 1000;
        internal const int MaxLua = 500;
        private const int FirstId = 1000000;
        private const int IdRange = 1000000;
        private static readonly Dictionary<string, Added> added = new Dictionary<string, Added>();
        private static bool hooked;

        public static string TitleOf(string modId, string key)
        {
            return ModIds.IsValid(modId) && ValidKey(key) ? modId + "." + key : null;
        }

        public static string Add(string modId, ConversationDefinition definition, out ConversationRefusal refusal)
        {
            refusal = Check(modId, definition);
            if (refusal != ConversationRefusal.None) return Refused(refusal, out refusal);
            if (SaveGate.Busy)
            {
                refusal = ConversationRefusal.SaveBusy;
                return null;
            }
            PCDS.DialogueDatabase master = Master();
            if (master == null)
            {
                refusal = ConversationRefusal.NotReady;
                return null;
            }
            string title = modId + "." + definition.Key;
            try
            {
                if (added.ContainsKey(title) || master.GetConversation(title) != null)
                {
                    refusal = ConversationRefusal.AlreadyExists;
                    return Refused(refusal, out refusal);
                }
                PCDS.Actor player = master.actors.Find(a => a.IsPlayer);
                if (player == null)
                {
                    refusal = ConversationRefusal.NoPlayerActor;
                    return Refused(refusal, out refusal);
                }
                int speakerId = SpeakerId(master, definition.Speaker);
                if (speakerId < 0 || speakerId == player.id)
                {
                    refusal = ConversationRefusal.UnknownSpeaker;
                    return Refused(refusal, out refusal);
                }
                PCDS.Conversation conversation = Build(title, FreeId(master, title), player.id, speakerId, definition);
                PCDS.DialogueDatabase database = PCDS.DatabaseUtility.CreateDialogueDatabaseInstance(false);
                database.name = "ObenseuerCore " + title;
                database.emphasisSettings = master.emphasisSettings;
                database.conversations.Add(conversation);
                PCDS.DialogueManager.AddDatabase(database);
                if (master.GetConversation(title) == null)
                {
                    UnityEngine.Object.Destroy(database);
                    refusal = ConversationRefusal.Failed;
                    return Refused(refusal, out refusal);
                }
                added[title] = new Added { ModId = modId, Database = database };
                Hook();
                return title;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Add", e);
                refusal = ConversationRefusal.Failed;
                return null;
            }
        }

        public static bool Remove(string modId, string key)
        {
            string title = TitleOf(modId, key);
            Added entry;
            if (title == null || SaveGate.Busy || !added.TryGetValue(title, out entry) || entry.ModId != modId) return false;
            if (Dialogue.IsActive && Dialogue.CurrentTitle == title) return false;
            try
            {
                PCDS.DialogueManager.RemoveDatabase(entry.Database);
                UnityEngine.Object.Destroy(entry.Database);
                added.Remove(title);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Remove", e);
                return false;
            }
        }

        public static int Clear(string modId)
        {
            if (!ModIds.IsValid(modId)) return 0;
            int removed = 0;
            foreach (string title in new List<string>(added.Keys))
            {
                if (added[title].ModId == modId && Remove(modId, title.Substring(modId.Length + 1))) removed++;
            }
            return removed;
        }

        internal static ConversationRefusal Check(string modId, ConversationDefinition definition)
        {
            if (!ModIds.IsValid(modId) || definition == null || !ValidKey(definition.Key)) return ConversationRefusal.BadInput;
            if (definition.Speaker != null && (definition.Speaker.Length == 0 || definition.Speaker.Length > QuestAuthoring.MaxTitle)) return ConversationRefusal.BadInput;
            List<ConversationLine> lines = definition.Lines;
            if (lines == null || lines.Count < 1 || lines.Count > MaxLines) return ConversationRefusal.BadInput;
            List<string> keys = new List<string>();
            foreach (ConversationLine line in lines)
            {
                if (line == null || !ValidKey(line.Key) || keys.Contains(line.Key) || !ValidText(line.Text, MaxText, true)) return ConversationRefusal.BadInput;
                if (!ValidText(line.Condition, MaxLua, false) || !ValidText(line.Script, MaxLua, false)) return ConversationRefusal.BadInput;
                keys.Add(line.Key);
            }
            foreach (ConversationLine line in lines)
            {
                if (line.Next == null) continue;
                foreach (string next in line.Next)
                {
                    if (!keys.Contains(next)) return ConversationRefusal.BadInput;
                }
            }
            return ConversationRefusal.None;
        }

        internal static PCDS.Conversation Build(string title, int id, int playerId, int speakerId, ConversationDefinition definition)
        {
            PCDS.Template template = PCDS.Template.FromDefault();
            PCDS.Conversation conversation = template.CreateConversation(id, title);
            conversation.ActorID = playerId;
            conversation.ConversantID = speakerId;
            PCDS.DialogueEntry start = template.CreateDialogueEntry(0, id, "START");
            start.ActorID = playerId;
            start.ConversantID = speakerId;
            conversation.dialogueEntries.Add(start);
            Dictionary<string, int> ids = new Dictionary<string, int>();
            for (int i = 0; i < definition.Lines.Count; i++) ids[definition.Lines[i].Key] = i + 1;
            start.outgoingLinks.Add(new PCDS.Link(id, 0, id, 1));
            foreach (ConversationLine line in definition.Lines)
            {
                int entryId = ids[line.Key];
                PCDS.DialogueEntry entry = template.CreateDialogueEntry(entryId, id, line.Key);
                entry.ActorID = line.Player ? playerId : speakerId;
                entry.ConversantID = line.Player ? speakerId : playerId;
                entry.DialogueText = line.Text;
                entry.conditionsString = line.Condition ?? "";
                entry.userScript = line.Script ?? "";
                if (line.Next != null)
                {
                    foreach (string next in line.Next) entry.outgoingLinks.Add(new PCDS.Link(id, entryId, id, ids[next]));
                }
                conversation.dialogueEntries.Add(entry);
            }
            return conversation;
        }

        internal static int StableId(string title)
        {
            uint hash = 2166136261u;
            foreach (char c in title) hash = unchecked((hash ^ c) * 16777619u);
            return FirstId + (int)(hash % IdRange);
        }

        private static int FreeId(PCDS.DialogueDatabase master, string title)
        {
            return FreeIdFrom(StableId(title), id => master.GetConversation(id) != null);
        }

        internal static int FreeIdFrom(int id, Func<int, bool> taken)
        {
            while (taken(id)) id = id + 1 >= FirstId + IdRange ? FirstId : id + 1;
            return id;
        }

        private static int SpeakerId(PCDS.DialogueDatabase master, string speaker)
        {
            if (speaker != null)
            {
                PCDS.Actor actor = master.GetActor(speaker);
                return actor == null || actor.IsPlayer ? -1 : actor.id;
            }
            int max = 0;
            foreach (PCDS.Actor actor in master.actors) max = Math.Max(max, actor.id);
            return max + 1;
        }

        internal static PCDS.DialogueDatabase Master()
        {
            return Dialogue.IsAvailable && PCDS.DialogueManager.hasInstance ? PCDS.DialogueManager.masterDatabase : null;
        }

        private static string Refused(ConversationRefusal reason, out ConversationRefusal refusal)
        {
            refusal = reason;
            ApiLog.Once(Owner, "Add " + reason, "refused: " + reason);
            return null;
        }

        private static void Hook()
        {
            if (hooked) return;
            hooked = true;
            Events.SceneLoaded += scene => Restore();
        }

        private static void Restore()
        {
            try
            {
                PCDS.DialogueDatabase master = Master();
                if (master == null) return;
                foreach (KeyValuePair<string, Added> pair in added)
                {
                    if (pair.Value.Database != null && master.GetConversation(pair.Key) == null) PCDS.DialogueManager.AddDatabase(pair.Value.Database);
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Restore", e);
            }
        }

        private static bool ValidKey(string key)
        {
            return key != null && key.Length <= QuestAuthoring.MaxKey && ModIds.IsValid(key) && key.IndexOf('.') < 0;
        }

        internal static bool ValidText(string text, int max, bool required)
        {
            if (text == null) return !required;
            if (text.Length > max || (required && text.Trim().Length == 0)) return false;
            foreach (char c in text)
            {
                if (char.IsControl(c) && c != '\n' && c != '\r') return false;
            }
            return true;
        }

        private sealed class Added
        {
            internal string ModId;
            internal PCDS.DialogueDatabase Database;
        }
    }
}
