using System;
using NPC;
using UnityEngine;

namespace ObenseuerCore
{
    public static class NpcActions
    {
        internal const string Owner = "obenseuer.moddingframework.npcactions";

        public static bool Teleport(int npcId, Vector3 position)
        {
            if (SaveSlots.WorldBusy()) return false;
            NPCInfo npc = Live(npcId);
            if (npc == null) return false;
            try
            {
                if (npc.npcController.richAI == null) return false;
                npc.npcController.richAI.Teleport(position);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "NpcActions.Teleport", e);
                return false;
            }
        }

        public static bool Kill(int npcId)
        {
            NPCInfo npc = Live(npcId);
            if (npc == null) return false;
            try
            {
                NPCController controller = npc.npcController;
                if (controller.Data != null && controller.Data.state != null) controller.ForceStopActivity();
                if (controller.richAI != null) controller.richAI.canMove = false;
                npc.Kill();
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "NpcActions.Kill", e);
                return false;
            }
        }

        public static bool Hit(int npcId)
        {
            NPCInfo npc = Live(npcId);
            if (npc == null) return false;
            try
            {
                CharacterBarkStarter bark = npc.GetComponent<CharacterBarkStarter>();
                if (bark == null || npc.character == null || Crime.instance == null) return false;
                if (npc.onHit != null) npc.onHit.triggerOutputs();
                if (!npc.dontCommentOnHit)
                {
                    string text = string.IsNullOrEmpty(npc.onHitDialogue) ? "Generic/Hit" : npc.onHitDialogue;
                    if (npc.startDialogueOnHit)
                    {
                        InteractableTalk talk = npc.GetComponent<InteractableTalk>();
                        if (talk != null) talk.StartDialogue(text);
                    }
                    else
                    {
                        bark.TryToCommentLookAtPlayer5s(text);
                    }
                }
                if (!npc.dontGiveCrimeOnHit)
                {
                    Crime.instance.CommitCrime(npc.character.NPCReference(), 100, Crime.CrimeType.Violence);
                    Witness.IsWitnessed();
                }
                if (npc.setAnimatorValue != null) npc.setAnimatorValue.Trigger("hit");
                NPCController controller = npc.npcController;
                if (controller.Data != null && controller.Data.state != null)
                {
                    Activity activity = controller.Data.state.CurrentActivity;
                    Act_Police police = activity as Act_Police;
                    if (police != null) police.StartChase();
                    Act_Robber robber = activity as Act_Robber;
                    if (robber != null) robber.AbortRobbing();
                }
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "NpcActions.Hit", e);
                return false;
            }
        }

        private static NPCInfo Live(int npcId)
        {
            try
            {
                if (SaveGate.Busy) return null;
                NPCInfo npc = NPCManager.instance == null ? null : NPCManager.instance.FindNpcById(npcId);
                if (npc == null || npc.npcController == null || !npc.gameObject.activeInHierarchy || npc.IsDead()) return null;
                return npc;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "NpcActions.Live", e);
                return null;
            }
        }
    }
}
