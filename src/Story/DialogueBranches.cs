using System;
using System.Collections.Generic;
using PCDS = PixelCrushers.DialogueSystem;

namespace ObenseuerCore
{
    internal static class DialogueBranches
    {
        private static readonly List<Registered> branches = new List<Registered>();
        private static bool hooked;

        internal static string Add(string modId, string conversationTitle, string anchorText, int anchorId, ConversationDefinition branch, int insertAt, int priority)
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
            if (!branch.Lines[0].Player) return Refused("the first branch line must be a player line");
            PCDS.DialogueDatabase master = DialogueAuthoring.Master();
            if (master == null) return Refused("the dialogue database is not ready");
            PCDS.Conversation conversation = master.GetConversation(conversationTitle);
            if (conversation == null) return Refused("conversation " + conversationTitle + " is not in the database");
            PCDS.DialogueEntry anchor;
            string anchorRefusal = DialogueResponses.AnchorRefusal(master, conversation, anchorText, anchorId, out anchor);
            if (anchorRefusal != null) return Refused(anchorRefusal);
            if (branch.Speaker != null && !IsActor(master, branch.Speaker, anchor.ActorID)) return Refused("the branch speaker must be the NPC of the anchor");
            string key = DialogueAuthoring.HashedId(modId, conversationTitle + "\n" + anchor.id + "\n" + branch.Key);
            if (branches.Exists(r => r.Key == key)) return Refused("this branch is already added");
            Registered record = new Registered { ModId = modId, Key = key, Title = conversationTitle, AnchorId = anchor.id, BranchKey = branch.Key, InsertAt = insertAt, Priority = DialogueResponses.RequestedPriority(priority, DialogueResponses.PlayerPriority(master, anchor)) };
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
                PCDS.Conversation branch = master.GetConversation(record.BranchTitle);
                List<int> entries = EntryIds(branch);
                if (!DialogueAuthoring.RemoveCore(modId, record.BranchKey) && branch != null) return false;
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

        internal static void ApplyAll()
        {
            DialogueAuthoring.Restore();
            PCDS.DialogueDatabase master = DialogueAuthoring.Master();
            if (master == null) return;
            foreach (Registered record in new List<Registered>(branches))
            {
                try
                {
                    if (Apply(master, record)) record.Retry.Clear();
                    else
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
                if (!record.Retry.Waiting) continue;
                waiting = true;
                break;
            }
            if (!waiting) return;
            DialogueAuthoring.Restore();
            PCDS.DialogueDatabase master = DialogueAuthoring.Master();
            if (master == null) return;
            foreach (Registered record in new List<Registered>(branches))
            {
                if (!record.Retry.Waiting) continue;
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
                record.Retry.Try(false);
                if (!record.Retry.Waiting) ApiLog.Once(Dialogue.Owner, "retry branch " + record.Key, "the conversation or the anchor never appeared");
            }
        }

        private static bool Apply(PCDS.DialogueDatabase master, Registered record)
        {
            PCDS.Conversation conversation = master.GetConversation(record.Title);
            PCDS.Conversation branch = master.GetConversation(record.BranchTitle);
            PCDS.DialogueEntry anchor = conversation == null ? null : conversation.GetDialogueEntry(record.AnchorId);
            PCDS.DialogueEntry start = branch == null ? null : branch.GetDialogueEntry(0);
            if (anchor == null || start == null) return false;
            if (record.BranchId >= 0 && branch.id != record.BranchId) return false;
            record.BranchId = branch.id;
            int at = DialogueResponses.InsertIndex(record.InsertAt, anchor.outgoingLinks.Count);
            foreach (PCDS.Link link in start.outgoingLinks)
            {
                int target = link.destinationDialogueID;
                if (anchor.outgoingLinks.Exists(l => l.destinationConversationID == branch.id && l.destinationDialogueID == target)) continue;
                at = DialogueResponses.InsertIndex(at, anchor.outgoingLinks.Count);
                anchor.outgoingLinks.Insert(at, new PCDS.Link(conversation.id, anchor.id, branch.id, target) { priority = record.Priority });
                at++;
            }
            return true;
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
            internal string BranchKey;
            internal string BranchTitle;
            internal int BranchId = -1;
            internal int InsertAt;
            internal PCDS.ConditionPriority Priority;
            internal readonly RetryGate Retry = new RetryGate();
        }
    }
}
