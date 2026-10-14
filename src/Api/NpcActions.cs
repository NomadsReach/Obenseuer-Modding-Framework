using System;
using NPC;
using UnityEngine;
using PCDS = PixelCrushers.DialogueSystem;

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

        public static bool FollowPlayer(int npcId, bool run, bool untilStopped)
        {
            NPCInfo npc = Live(npcId);
            if (npc == null) return false;
            try
            {
                NPCController controller = npc.npcController;
                if (controller.Data == null || controller.Data.character == null) return false;
                Act_Follow act = controller.FollowPlayer(run, !untilStopped, false);
                return controller.Data.state != null && controller.Data.state.CurrentActivity == act;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "NpcActions.FollowPlayer", e);
                return false;
            }
        }

        public static bool StopFollowing(int npcId)
        {
            NPCInfo npc = Live(npcId);
            if (npc == null) return false;
            try
            {
                NPCController controller = npc.npcController;
                if (controller.Data == null || controller.Data.state == null) return false;
                if (!(controller.Data.state.CurrentActivity is Act_Follow)) return false;
                controller.ForceStopActivity();
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "NpcActions.StopFollowing", e);
                return false;
            }
        }

        public static bool Say(int npcId, string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            NPCInfo npc = Live(npcId);
            if (npc == null) return false;
            try
            {
                if (!PCDS.DialogueManager.hasInstance || PCDS.DialogueManager.isConversationActive || Subtitles.instance == null) return false;
                MonoBehaviour bark = PCDS.DialogueActor.GetBarkUI(npc.transform) as MonoBehaviour;
                if (bark == null || !bark.enabled) return false;
                SubtitleBarkUI subtitle = bark as SubtitleBarkUI;
                if (subtitle != null) subtitle.Start();
                PCDS.DialogueManager.BarkString(text, npc.transform);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "NpcActions.Say", e);
                return false;
            }
        }

        public static bool Comment(int npcId, string conversationTitle)
        {
            if (string.IsNullOrEmpty(conversationTitle)) return false;
            NPCInfo npc = Live(npcId);
            CharacterBarkStarter starter = npc == null ? null : npc.characterBarkStarter;
            if (starter == null || !starter.enabled || starter.cacheBarkLines) return false;
            try
            {
                PCDS.DialogueDatabase database = PCDS.DialogueManager.masterDatabase;
                if (database == null || database.GetConversation(conversationTitle) == null) return false;
                if (PCDS.DialogueManager.isConversationActive && !starter.allowDuringConversations) return false;
                npc.TryComment(conversationTitle);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "NpcActions.Comment", e);
                return false;
            }
        }

        public static bool StartDialogue(int npcId, string conversationTitle)
        {
            if (SaveSlots.WorldBusy() || !Dialogue.Exists(conversationTitle) || Dialogue.IsActive) return false;
            NPCInfo npc = Live(npcId);
            InteractableTalk talk = npc == null ? null : npc.GetComponent<InteractableTalk>();
            if (talk == null) return false;
            try
            {
                return talk.CheckStartDialogue(conversationTitle) && Dialogue.IsActive;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "NpcActions.StartDialogue", e);
                return false;
            }
        }

        public static bool LookAtPlayer(int npcId)
        {
            NPCInfo npc = Live(npcId);
            if (npc == null || npc.characterHeadController == null) return false;
            try
            {
                if (ThirdPersonCameraCollision.instance == null) return false;
                npc.characterHeadController.LookAtPlayerDialogue();
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "NpcActions.LookAtPlayer", e);
                return false;
            }
        }

        public static bool LookAtPlayer(int npcId, int seconds)
        {
            NPCInfo npc = Live(npcId);
            if (npc == null || npc.characterHeadController == null) return false;
            try
            {
                if (seconds <= 0 || ThirdPersonCameraCollision.instance == null || !npc.characterHeadController.isActiveAndEnabled) return false;
                npc.characterHeadController.LookAtPlayerTime(seconds);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "NpcActions.LookAtPlayer", e);
                return false;
            }
        }

        public static bool LookAt(int npcId, Transform target)
        {
            NPCInfo npc = Live(npcId);
            if (npc == null || npc.characterHeadController == null || target == null) return false;
            try
            {
                npc.characterHeadController.LookAtNPCTarget(target);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "NpcActions.LookAt", e);
                return false;
            }
        }

        public static bool StopLooking(int npcId)
        {
            NPCInfo npc = Live(npcId);
            if (npc == null || npc.characterHeadController == null) return false;
            try
            {
                npc.characterHeadController.StopLookAtTarget();
                npc.characterHeadController.EnableLookAtPlayer();
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "NpcActions.StopLooking", e);
                return false;
            }
        }

        public static bool ChangeLevel(int npcId, string scene)
        {
            if (string.IsNullOrEmpty(scene) || SaveGate.Busy) return false;
            try
            {
                NPCData data = NPCDirector.instance == null ? null : NPCDirector.instance.GetNPCDataByID(npcId);
                if (data == null || data.state == null || data.state.currentScene == scene || data.state.activePath != null || data.Controller != null) return false;
                if (NPCManager.instance == null || data.state.currentScene == NPCManager.instance.ActiveScene) return false;
                if (data.state.CurrentActivity != null && data.state.CurrentActivity.GetFollowTarget() != null) return false;
                string timetableScene = data.scheduler == null || data.scheduler.currentScheduledAction == null || data.scheduler.currentScheduledAction.activity == null ? null : data.scheduler.currentScheduledAction.activity.GetTargetScene();
                if (!data.state.schedulerDisabled && !string.IsNullOrEmpty(timetableScene) && timetableScene != scene) return false;
                if (!SaveController.CheckIfLevelExists(scene)) return false;
                NPCSceneUtilities.ChangeNPCLevel(data, scene);
                if (data.state.currentScene != scene) return false;
                data.state.currentPosition = Vector3.zero;
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "NpcActions.ChangeLevel", e);
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
