using System;
using System.Collections.Generic;
using PCDS = PixelCrushers.DialogueSystem;

namespace ObenseuerCore
{
    internal static class DialogueResponses
    {
        private static readonly List<Registered> responses = new List<Registered>();
        private static bool hooked;

        internal static string Add(string modId, string conversationTitle, string anchorText, int anchorId, string playerText, string npcReply, string luaCondition, Action onPicked)
        {
            if (string.IsNullOrEmpty(conversationTitle)) throw new ArgumentException("conversationTitle must not be empty", "conversationTitle");
            if (string.IsNullOrEmpty(playerText)) throw new ArgumentException("playerText must not be empty", "playerText");
            if (!Dialogue.IsAvailable) return Refused("the dialogue system is not installed");
            if (SaveGate.Busy) return Refused("a save or load is running");
            if (!DialogueAuthoring.ValidText(playerText, DialogueAuthoring.MaxText, true) || (npcReply != null && !DialogueAuthoring.ValidText(npcReply, DialogueAuthoring.MaxText, true)) || !DialogueAuthoring.ValidText(luaCondition, DialogueAuthoring.MaxLua, false)) return Refused("bad text or condition");
            PCDS.DialogueDatabase master = DialogueAuthoring.Master();
            if (master == null) return Refused("the dialogue database is not ready");
            PCDS.Conversation conversation = master.GetConversation(conversationTitle);
            if (conversation == null) return Refused("conversation " + conversationTitle + " is not in the database");
            PCDS.DialogueEntry anchor;
            string anchorRefusal = AnchorRefusal(master, conversation, anchorText, anchorId, out anchor);
            if (anchorRefusal != null) return Refused(anchorRefusal);
            string key = KeyFor(modId, conversationTitle, anchor.id, playerText, npcReply, luaCondition);
            Registered response = responses.Find(r => r.Key == key);
            if (response == null)
            {
                response = new Registered { ModId = modId, Key = key, Title = conversationTitle, AnchorId = anchor.id, PlayerText = playerText, NpcReply = npcReply, Condition = luaCondition, Priority = PlayerPriority(master, anchor) };
                response.PlayerId = DialogueAuthoring.FreeIdFrom(DialogueAuthoring.StableId(key), id => conversation.GetDialogueEntry(id) != null);
                response.NpcId = npcReply == null ? -1 : DialogueAuthoring.FreeIdFrom(response.PlayerId + 1, id => conversation.GetDialogueEntry(id) != null);
            }
            response.OnPicked = onPicked;
            if (!Apply(master, response)) return Refused("the response could not be written into the conversation");
            if (!responses.Contains(response)) responses.Add(response);
            Hook();
            return key;
        }

        internal static bool Remove(string modId, string key)
        {
            Registered response = responses.Find(r => r.ModId == modId && r.Key == key);
            if (response == null || SaveGate.Busy) return false;
            responses.Remove(response);
            try
            {
                PCDS.DialogueDatabase master = DialogueAuthoring.Master();
                if (master != null) Detach(master, response);
            }
            catch (Exception e)
            {
                ApiLog.Once(Dialogue.Owner, "RemoveResponse", e);
            }
            return true;
        }

        internal static void RemoveAll(string modId)
        {
            foreach (Registered response in responses.FindAll(r => r.ModId == modId)) Remove(modId, response.Key);
        }

        internal static void Picked(string key)
        {
            Registered response = responses.Find(r => r.Key == key);
            if (response == null || response.OnPicked == null) return;
            try
            {
                response.OnPicked();
            }
            catch (Exception e)
            {
                ApiLog.Once(Dialogue.Owner, "response onPicked", e);
            }
        }

        internal static void ApplyAll()
        {
            PCDS.DialogueDatabase master = DialogueAuthoring.Master();
            if (master == null) return;
            foreach (Registered response in new List<Registered>(responses))
            {
                try
                {
                    Apply(master, response);
                }
                catch (Exception e)
                {
                    ApiLog.Once(Dialogue.Owner, "re-apply response", e);
                }
            }
        }

        internal static PCDS.DialogueEntry FindAnchor(List<PCDS.DialogueEntry> entries, string text)
        {
            if (string.IsNullOrEmpty(text) || text.Trim().Length == 0) return null;
            List<PCDS.DialogueEntry> matches = entries.FindAll(e => e.DialogueText == text);
            if (matches.Count == 0)
            {
                string wanted = text.Trim();
                matches = entries.FindAll(e => string.Equals((e.DialogueText ?? "").Trim(), wanted, StringComparison.OrdinalIgnoreCase));
            }
            return matches.Count == 1 ? matches[0] : null;
        }

        internal static string AnchorRefusal(PCDS.DialogueDatabase master, PCDS.Conversation conversation, string anchorText, int anchorId, out PCDS.DialogueEntry anchor)
        {
            anchor = anchorText != null ? FindAnchor(conversation.dialogueEntries, anchorText) : conversation.GetDialogueEntry(anchorId);
            if (anchor == null) return "anchor is missing or matches more than one line";
            if (master.GetCharacterType(anchor.ActorID) != PCDS.CharacterType.NPC) return "anchor is not an NPC line";
            if (LeadsOnlyToNpc(master, anchor)) return "anchor only leads to NPC lines";
            if (PlayerActorId(master) < 0) return "the database has no player actor";
            return null;
        }

        internal static string KeyFor(string modId, string conversationTitle, int anchorId, string playerText, string npcReply, string luaCondition)
        {
            uint hash = 2166136261u;
            string text = conversationTitle + "\n" + anchorId + "\n" + playerText + "\n" + (npcReply ?? "") + "\n" + (luaCondition ?? "");
            foreach (char c in text) hash = unchecked((hash ^ c) * 16777619u);
            return modId + "." + hash.ToString("x8");
        }

        private static bool Apply(PCDS.DialogueDatabase master, Registered response)
        {
            PCDS.Conversation conversation = master.GetConversation(response.Title);
            PCDS.DialogueEntry anchor = conversation == null ? null : conversation.GetDialogueEntry(response.AnchorId);
            if (anchor == null) return false;
            PCDS.DialogueEntry existing = conversation.GetDialogueEntry(response.PlayerId);
            if (existing != null)
            {
                existing.userScript = PickScript(response.Key);
                return true;
            }
            int playerActor = PlayerActorId(master);
            if (playerActor < 0) return false;
            PCDS.Template template = PCDS.Template.FromDefault();
            PCDS.DialogueEntry player = template.CreateDialogueEntry(response.PlayerId, conversation.id, response.Key);
            player.ActorID = playerActor;
            player.ConversantID = anchor.ActorID;
            player.DialogueText = response.PlayerText;
            player.conditionsString = response.Condition ?? "";
            player.userScript = PickScript(response.Key);
            conversation.dialogueEntries.Add(player);
            anchor.outgoingLinks.Add(new PCDS.Link(conversation.id, anchor.id, conversation.id, response.PlayerId) { priority = response.Priority });
            if (response.NpcId < 0) return true;
            PCDS.DialogueEntry npc = template.CreateDialogueEntry(response.NpcId, conversation.id, response.Key + "_reply");
            npc.ActorID = anchor.ActorID;
            npc.ConversantID = playerActor;
            npc.DialogueText = response.NpcReply;
            conversation.dialogueEntries.Add(npc);
            player.outgoingLinks.Add(new PCDS.Link(conversation.id, response.PlayerId, conversation.id, response.NpcId) { priority = response.Priority });
            return true;
        }

        private static void Detach(PCDS.DialogueDatabase master, Registered response)
        {
            PCDS.Conversation conversation = master.GetConversation(response.Title);
            if (conversation == null) return;
            PCDS.DialogueEntry anchor = conversation.GetDialogueEntry(response.AnchorId);
            if (anchor != null) anchor.outgoingLinks.RemoveAll(l => l.destinationConversationID == conversation.id && l.destinationDialogueID == response.PlayerId);
            conversation.dialogueEntries.RemoveAll(e => e.id == response.PlayerId || e.id == response.NpcId);
            ForgetSimStatus(conversation.id, response.PlayerId);
            if (response.NpcId >= 0) ForgetSimStatus(conversation.id, response.NpcId);
        }

        internal static void ForgetSimStatus(int conversationId, int entryId)
        {
            PCDS.Lua.Run(string.Format("if Conversation[{0}] ~= nil and Conversation[{0}].Dialog ~= nil then Conversation[{0}].Dialog[{1}] = nil end", conversationId, entryId));
        }

        internal static int PlayerActorId(PCDS.DialogueDatabase master)
        {
            PCDS.Actor player = master.actors.Find(a => a.IsPlayer);
            return player == null ? -1 : player.id;
        }

        internal static PCDS.ConditionPriority PlayerPriority(PCDS.DialogueDatabase master, PCDS.DialogueEntry anchor)
        {
            foreach (PCDS.Link link in anchor.outgoingLinks)
            {
                PCDS.DialogueEntry target = master.GetDialogueEntry(link);
                if (target != null && master.GetCharacterType(target.ActorID) == PCDS.CharacterType.PC) return link.priority;
            }
            return PCDS.ConditionPriority.Normal;
        }

        private static bool LeadsOnlyToNpc(PCDS.DialogueDatabase master, PCDS.DialogueEntry anchor)
        {
            bool npc = false;
            foreach (PCDS.Link link in anchor.outgoingLinks)
            {
                PCDS.DialogueEntry target = master.GetDialogueEntry(link);
                if (target == null) continue;
                PCDS.CharacterType type = master.GetCharacterType(target.ActorID);
                if (type == PCDS.CharacterType.PC) return false;
                if (type == PCDS.CharacterType.NPC) npc = true;
            }
            return npc;
        }

        private static string PickScript(string key)
        {
            return "Core_ResponsePicked(\"" + key + "\")";
        }

        private static string Refused(string reason)
        {
            StoryLog.Refuse(Dialogue.Owner, "AddResponse", reason);
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
            internal int PlayerId;
            internal int NpcId;
            internal string PlayerText;
            internal string NpcReply;
            internal string Condition;
            internal PCDS.ConditionPriority Priority;
            internal Action OnPicked;
        }
    }
}
