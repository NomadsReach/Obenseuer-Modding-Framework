using System;
using System.Collections.Generic;
using PCDS = PixelCrushers.DialogueSystem;

namespace ObenseuerCore
{
    internal static class DialogueBranches
    {
        private static readonly List<Registered> branches = new List<Registered>();
        private static bool hooked;

        internal static string Add(string modId, string conversationTitle, string anchorText, int anchorId, ConversationDefinition branch, int insertAt, int priority, Action onPicked)
        {
            if (string.IsNullOrEmpty(conversationTitle)) throw new ArgumentException("conversationTitle must not be empty", "conversationTitle");
            if (branch == null) throw new ArgumentNullException("branch");
            if (insertAt < -1) throw new ArgumentException("insertAt must be -1 or a link index", "insertAt");
            if (!DialogueResponses.ValidPriority(priority)) throw new ArgumentException("priority must be -1 or 0 to 4", "priority");
            if (!Dialogue.IsAvailable) return Refused("the dialogue system is not installed");
            if (SaveGate.Busy) return Refused("a save or load is running");
            if (branch.Key == Dialogue.NarrationKey) return Refused("the branch key narration is reserved for Dialogue.Narrate");
            ConversationRefusal check = DialogueAuthoring.Check(modId, branch);
            if (check != ConversationRefusal.None) return Refused("the branch is not valid: " + check);
            if (anchorText == null && anchorId < 0) return Refused("anchor is missing or matches more than one line");
            if (anchorText != null && anchorText.Trim().Length == 0) return Refused("anchorText must not be empty or only spaces");
            PCDS.DialogueDatabase master = DialogueAuthoring.Master();
            if (master == null) return Refused("the dialogue database is not ready");
            PCDS.Conversation conversation = master.GetConversation(conversationTitle);
            PCDS.DialogueEntry anchor = null;
            if (conversation != null)
            {
                string anchorRefusal = DialogueResponses.AnchorRefusal(master, conversation, anchorText, anchorId, out anchor);
                if (anchorRefusal != null && !AnchorAbsent(conversation, anchorText, anchorId)) return Refused(anchorRefusal);
            }
            string key = BranchKey(modId, conversationTitle, anchorText, anchorId, branch.Key);
            if (branches.Exists(r => r.Key == key)) return Refused("this branch is already added");
            Registered record = new Registered { ModId = modId, Key = key, Title = conversationTitle, AnchorId = anchor == null ? anchorId : anchor.id, AnchorText = anchorText, BranchKey = branch.Key, Definition = branch, InsertAt = insertAt, PriorityRequest = priority, OnPicked = onPicked, PickScript = onPicked == null ? null : PickScriptFor(key) };
            if (anchor == null)
            {
                record.Retry.Arm();
                branches.Add(record);
                Hook();
                ApiLog.Once(Dialogue.Owner, "AddBranch waiting", "a branch conversation or its anchor is not loaded yet, the branch is linked when they appear");
                return key;
            }
            if (branch.Speaker != null && !IsActor(master, branch.Speaker, anchor.ActorID)) return Refused("the branch speaker must be the NPC of the anchor");
            ConversationRefusal added;
            record.BranchTitle = DialogueAuthoring.AddWith(modId, branch, anchor.ActorID, out added);
            if (record.BranchTitle == null) return Refused("the branch conversation was refused: " + added);
            bool linked = false;
            try
            {
                linked = Apply(master, record);
            }
            catch (Exception e)
            {
                ApiLog.Once(Dialogue.Owner, "AddBranch", e);
            }
            if (linked)
            {
                branches.Add(record);
                Hook();
                return key;
            }
            Unwind(master, record);
            return Refused("the branch could not be linked to the anchor");
        }

        internal static bool Remove(string modId, string key)
        {
            Registered record = branches.Find(r => r.ModId == modId && r.Key == key);
            if (record == null || SaveGate.Busy) return false;
            PCDS.DialogueDatabase master = DialogueAuthoring.Master();
            if (master == null) return false;
            if (Dialogue.IsActive) return false;
            try
            {
                PCDS.Conversation branch = record.BranchTitle == null ? null : master.GetConversation(record.BranchTitle);
                List<int> entries = EntryIds(branch);
                if (record.BranchTitle != null && !DialogueAuthoring.RemoveCore(modId, record.BranchKey)) return false;
                int branchId = branch == null ? -1 : branch.id;
                branches.Remove(record);
                Detach(master, record);
                foreach (int entry in entries) DialogueResponses.ForgetSimStatus(branchId, entry);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Dialogue.Owner, "RemoveBranch", e);
                return false;
            }
        }

        internal static int RemoveAll(string modId)
        {
            int removed = 0;
            foreach (Registered record in branches.FindAll(r => r.ModId == modId))
            {
                if (Remove(modId, record.Key)) removed++;
            }
            return removed;
        }

        internal static bool Owns(string title)
        {
            return title != null && branches.Exists(r => r.BranchTitle == title);
        }

        internal static void Picked(string key)
        {
            Registered record = branches.Find(r => r.Key == key);
            if (record == null || record.OnPicked == null) return;
            try
            {
                record.OnPicked();
            }
            catch (Exception e)
            {
                ApiLog.Once(Dialogue.Owner, "branch onPicked", e);
            }
        }

        internal static void ApplyAll()
        {
            DialogueAuthoring.Restore();
            PCDS.DialogueDatabase master = DialogueAuthoring.Master();
            if (master == null) return;
            foreach (Registered record in new List<Registered>(branches))
            {
                if (record.Failed) continue;
                try
                {
                    if (Apply(master, record)) record.Retry.Clear();
                    else if (!record.Failed)
                    {
                        record.Retry.Arm();
                        ApiLog.Once(Dialogue.Owner, "re-apply branch", "a branch conversation or its anchor is missing");
                    }
                }
                catch (Exception e)
                {
                    record.Retry.Arm();
                    ApiLog.Once(Dialogue.Owner, "re-apply branch", e);
                }
            }
        }

        internal static void Tick()
        {
            bool waiting = false;
            foreach (Registered record in branches)
            {
                if (record.Failed || !record.Retry.Waiting) continue;
                waiting = true;
                break;
            }
            if (!waiting) return;
            DialogueAuthoring.Restore();
            PCDS.DialogueDatabase master = DialogueAuthoring.Master();
            if (master == null) return;
            foreach (Registered record in new List<Registered>(branches))
            {
                if (record.Failed || !record.Retry.Waiting) continue;
                bool applied = false;
                try
                {
                    applied = Apply(master, record);
                }
                catch (Exception e)
                {
                    ApiLog.Once(Dialogue.Owner, "retry branch " + record.Key, e);
                }
                if (applied)
                {
                    record.Retry.Clear();
                    continue;
                }
                if (record.Failed) continue;
                record.Retry.Try(false);
                if (!record.Retry.Waiting) ApiLog.Once(Dialogue.Owner, "retry branch " + record.Key, "the conversation or the anchor never appeared");
            }
        }

        private static bool Apply(PCDS.DialogueDatabase master, Registered record)
        {
            PCDS.Conversation conversation = master.GetConversation(record.Title);
            if (conversation == null) return false;
            PCDS.DialogueEntry anchor = Resolve(master, conversation, record);
            if (anchor == null) return false;
            if (record.BranchTitle == null && record.Definition.Speaker != null && !IsActor(master, record.Definition.Speaker, anchor.ActorID))
            {
                Fail(record, "the branch speaker must be the NPC of the anchor");
                return false;
            }
            if (record.BranchTitle == null && !Create(master, record, anchor)) return false;
            PCDS.Conversation branch = master.GetConversation(record.BranchTitle);
            PCDS.DialogueEntry start = branch == null ? null : branch.GetDialogueEntry(0);
            if (start == null) return false;
            if (record.BranchId >= 0 && branch.id != record.BranchId) return false;
            record.BranchId = branch.id;
            PCDS.ConditionPriority playerPriority = DialogueResponses.PlayerPriority(master, anchor);
            bool npcFirst = NpcFirst(record.Definition);
            record.Priority = DialogueResponses.LinkPriority(record.PriorityRequest, playerPriority, npcFirst);
            if (record.PriorityRequest < 0 && npcFirst && playerPriority == PCDS.ConditionPriority.Low)
            {
                ApiLog.Once(Dialogue.Owner, "npc-first branch " + record.Key, "the anchor's player choices are at the lowest priority, so this branch shares their priority and can be shown instead of the menu; give its first line a Condition");
            }
            int at = DialogueResponses.InsertIndex(record.InsertAt, anchor.outgoingLinks.Count);
            foreach (PCDS.Link link in start.outgoingLinks)
            {
                int target = link.destinationDialogueID;
                if (record.PickScript != null) AddPickScript(branch.GetDialogueEntry(target), record.PickScript);
                if (anchor.outgoingLinks.Exists(l => l.destinationConversationID == branch.id && l.destinationDialogueID == target)) continue;
                at = DialogueResponses.InsertIndex(at, anchor.outgoingLinks.Count);
                anchor.outgoingLinks.Insert(at, new PCDS.Link(conversation.id, anchor.id, branch.id, target) { priority = record.Priority });
                at++;
            }
            return true;
        }

        private static PCDS.DialogueEntry Resolve(PCDS.DialogueDatabase master, PCDS.Conversation conversation, Registered record)
        {
            if (record.BranchTitle != null)
            {
                return record.AnchorId < 0 ? null : conversation.GetDialogueEntry(record.AnchorId);
            }
            PCDS.DialogueEntry found;
            int matches;
            string refusal = DialogueResponses.AnchorRefusal(master, conversation, record.AnchorText, record.AnchorId, out found, out matches);
            if (refusal == null)
            {
                record.AnchorId = found.id;
                return found;
            }
            if (found != null || matches > 1) Fail(record, refusal);
            return null;
        }

        private static bool Create(PCDS.DialogueDatabase master, Registered record, PCDS.DialogueEntry anchor)
        {
            ConversationRefusal refusal;
            string title = DialogueAuthoring.AddWith(record.ModId, record.Definition, anchor.ActorID, out refusal);
            if (title != null)
            {
                record.BranchTitle = title;
                return true;
            }
            if (refusal == ConversationRefusal.AlreadyExists)
            {
                Fail(record, "the branch conversation was refused: " + refusal);
                return false;
            }
            ApiLog.Once(Dialogue.Owner, "retry branch " + record.Key, "the branch conversation is not ready: " + refusal);
            return false;
        }

        private static void Fail(Registered record, string reason)
        {
            record.Failed = true;
            record.Retry.Clear();
            StoryLog.Refuse(Dialogue.Owner, "AddBranch", reason);
        }

        private static void Unwind(PCDS.DialogueDatabase master, Registered record)
        {
            try
            {
                Detach(master, record);
            }
            catch (Exception e)
            {
                ApiLog.Once(Dialogue.Owner, "AddBranch unwind", e);
            }
            DialogueAuthoring.RemoveCore(record.ModId, record.BranchKey);
        }

        private static void Detach(PCDS.DialogueDatabase master, Registered record)
        {
            PCDS.Conversation conversation = master.GetConversation(record.Title);
            PCDS.DialogueEntry anchor = conversation == null ? null : conversation.GetDialogueEntry(record.AnchorId);
            if (anchor != null) anchor.outgoingLinks.RemoveAll(l => l.destinationConversationID == record.BranchId);
        }

        private static void AddPickScript(PCDS.DialogueEntry entry, string script)
        {
            if (entry == null) return;
            string current = entry.userScript ?? "";
            if (current.IndexOf(script, StringComparison.Ordinal) >= 0) return;
            entry.userScript = current.Length == 0 ? script : current + "\n" + script;
        }

        private static string BranchKey(string modId, string conversationTitle, string anchorText, int anchorId, string branchKey)
        {
            string anchor = anchorText != null ? "text\n" + anchorText : "id\n" + anchorId.ToString();
            return DialogueAuthoring.HashedId(modId, conversationTitle + "\n" + anchor + "\n" + branchKey);
        }

        private static string PickScriptFor(string key)
        {
            return "Core_BranchPicked(\"" + key + "\")";
        }

        private static bool NpcFirst(ConversationDefinition definition)
        {
            if (definition == null || definition.Lines == null || definition.Lines.Count == 0 || definition.Lines[0] == null) return false;
            return !definition.Lines[0].Player;
        }

        private static bool AnchorAbsent(PCDS.Conversation conversation, string anchorText, int anchorId)
        {
            if (anchorText != null)
            {
                if (anchorText.Trim().Length == 0) return false;
                return DialogueResponses.AnchorMatchCount(conversation.dialogueEntries, anchorText) == 0;
            }
            return conversation.GetDialogueEntry(anchorId) == null;
        }

        private static List<int> EntryIds(PCDS.Conversation branch)
        {
            List<int> ids = new List<int>();
            if (branch == null) return ids;
            foreach (PCDS.DialogueEntry entry in branch.dialogueEntries) ids.Add(entry.id);
            return ids;
        }

        private static bool IsActor(PCDS.DialogueDatabase master, string name, int actorId)
        {
            PCDS.Actor actor = master.GetActor(name);
            return actor != null && actor.id == actorId;
        }

        private static string Refused(string reason)
        {
            StoryLog.Refuse(Dialogue.Owner, "AddBranch", reason);
            return null;
        }

        private static void Hook()
        {
            if (hooked) return;
            hooked = true;
            Events.SceneLoaded += scene => ApplyAll();
            Events.Loaded += ApplyAll;
        }

        private sealed class Registered
        {
            internal string ModId;
            internal string Key;
            internal string Title;
            internal int AnchorId;
            internal string AnchorText;
            internal string BranchKey;
            internal ConversationDefinition Definition;
            internal string BranchTitle;
            internal int BranchId = -1;
            internal int InsertAt;
            internal int PriorityRequest;
            internal PCDS.ConditionPriority Priority;
            internal Action OnPicked;
            internal string PickScript;
            internal bool Failed;
            internal readonly RetryGate Retry = new RetryGate();
        }
    }
}
