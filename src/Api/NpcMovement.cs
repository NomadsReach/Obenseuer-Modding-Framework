using System;
using System.Collections.Generic;
using NPC;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ObenseuerCore
{
    public static class NpcMovement
    {
        internal const string Owner = "obenseuer.moddingframework.npcmovement";
        private const float ArriveDistance = 0.5f;
        private static readonly List<Claim> claims = new List<Claim>();
        private static readonly List<Claim> pending = new List<Claim>();

        public static bool SetActivityEnabled(string modId, int npcId, bool enabled)
        {
            ModIds.Require(modId, "modId");
            try
            {
                return enabled ? ReleaseActivity(modId, npcId) : HoldActivity(modId, npcId);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "NpcMovement.SetActivityEnabled", e);
                return false;
            }
        }

        public static bool MoveTo(string modId, int npcId, Vector3 position)
        {
            ModIds.Require(modId, "modId");
            try
            {
                NPCController controller = LiveController(npcId);
                if (controller == null || controller.richAI == null) return false;
                Ensure(modId, npcId, controller).Disabled = true;
                controller.DisableActivity();
                controller.SetTargetPosition(position, ArriveDistance, null);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "NpcMovement.MoveTo", e);
                return false;
            }
        }

        public static bool Patrol(string modId, int npcId, string nodeGroupId)
        {
            ModIds.Require(modId, "modId");
            NodeGroup group = FindGroup(nodeGroupId);
            if (group == null || group.nodes == null || group.nodes.Count == 0) return false;
            return StartControlled<Act_Patrol>(modId, npcId, "NpcMovement.Patrol", controller => controller.StartPatrol(group));
        }

        public static bool Stand(string modId, int npcId, string nodeGroupId)
        {
            ModIds.Require(modId, "modId");
            NodeGroup group = FindGroup(nodeGroupId);
            if (group == null || group.nodeReference == null || group.nodeReference.node == null) return false;
            return StartControlled<Act_Stand>(modId, npcId, "NpcMovement.Stand", controller => controller.StartStand(group));
        }

        public static bool ForceStopActivity(string modId, int npcId)
        {
            ModIds.Require(modId, "modId");
            try
            {
                NPCController controller = LiveController(npcId);
                if (controller == null || controller.Data.state.CurrentActivity == null) return false;
                controller.ForceStopActivity();
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "NpcMovement.ForceStopActivity", e);
                return false;
            }
        }

        public static bool Clear(string modId, int npcId)
        {
            ModIds.Require(modId, "modId");
            try
            {
                Claim claim = Find(modId, npcId);
                if (claim == null) return false;
                claims.Remove(claim);
                StopStarted(claim);
                if (claim.Disabled) Release(claim);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "NpcMovement.Clear", e);
                return false;
            }
        }

        internal static void Init()
        {
            Events.SceneLoadedWithMode += OnSceneLoaded;
        }

        internal static void Tick()
        {
            for (int i = claims.Count - 1; i >= 0; i--)
            {
                Claim claim = claims[i];
                if (claim.Controller != null) continue;
                claims.RemoveAt(i);
                StopStarted(claim);
            }
            for (int i = pending.Count - 1; i >= 0; i--)
            {
                Claim claim = pending[i];
                if (claim.Controller == null || IsHeld(claim.NpcId))
                {
                    pending.RemoveAt(i);
                    continue;
                }
                if (!Ready(claim.Controller)) continue;
                pending.RemoveAt(i);
                claim.Controller.EnableActivity();
            }
        }

        internal static NPCController LiveController(int npcId)
        {
            try
            {
                if (SaveGate.Busy) return null;
                NPCInfo npc = NPCManager.instance == null ? null : NPCManager.instance.FindNpcById(npcId);
                if (npc == null || npc.npcController == null || !npc.gameObject.activeInHierarchy || npc.IsDead()) return null;
                NPCController controller = npc.npcController;
                return controller.Data == null || controller.Data.state == null ? null : controller;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "NpcMovement.LiveController", e);
                return null;
            }
        }

        private static bool HoldActivity(string modId, int npcId)
        {
            NPCController controller = LiveController(npcId);
            if (controller == null) return false;
            Ensure(modId, npcId, controller).Disabled = true;
            controller.DisableActivity();
            return true;
        }

        private static bool ReleaseActivity(string modId, int npcId)
        {
            Claim claim = Find(modId, npcId);
            if (claim == null || !claim.Disabled) return false;
            claim.Disabled = false;
            if (claim.Started == null) claims.Remove(claim);
            Release(claim);
            return true;
        }

        private static bool StartControlled<T>(string modId, int npcId, string member, Action<NPCController> start) where T : Activity
        {
            try
            {
                NPCController controller = LiveController(npcId);
                if (controller == null || IsHeld(npcId)) return false;
                start(controller);
                Activity current = controller.Data.state.CurrentActivity;
                if (!(current is T)) return false;
                Ensure(modId, npcId, controller).Started = current;
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, member, e);
                return false;
            }
        }

        private static void OnSceneLoaded(string scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single) return;
            try
            {
                List<Claim> released = new List<Claim>(claims);
                foreach (Claim claim in released)
                {
                    claims.Remove(claim);
                    StopStarted(claim);
                    if (claim.Disabled) Release(claim);
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "NpcMovement.SceneLoaded", e);
            }
        }

        private static void Release(Claim claim)
        {
            if (claim.Controller == null || IsHeld(claim.NpcId)) return;
            if (!Ready(claim.Controller))
            {
                if (!pending.Contains(claim)) pending.Add(claim);
                return;
            }
            claim.Controller.EnableActivity();
        }

        private static bool Ready(NPCController controller)
        {
            return controller != null && !SaveGate.Busy && controller.gameObject.activeInHierarchy && controller.Data != null && controller.Data.state != null;
        }

        private static void StopStarted(Claim claim)
        {
            if (claim.Started == null) return;
            NPCController controller = claim.Controller;
            NPCData data = controller != null ? controller.Data : FindData(claim.NpcId);
            if (data == null || data.state == null || data.state.CurrentActivity != claim.Started) return;
            if (controller != null)
            {
                controller.ForceStopActivity();
                return;
            }
            claim.Started.ForceStopActivity();
            data.state.CurrentActivity = null;
        }

        private static NPCData FindData(int npcId)
        {
            return NPCDirector.instance == null ? null : NPCDirector.instance.GetNPCDataByID(npcId);
        }

        private static NodeGroup FindGroup(string nodeGroupId)
        {
            return string.IsNullOrEmpty(nodeGroupId) ? null : SceneIds.Find<NodeGroup>(nodeGroupId);
        }

        private static Claim Find(string modId, int npcId)
        {
            return claims.Find(c => c.ModId == modId && c.NpcId == npcId);
        }

        private static Claim Ensure(string modId, int npcId, NPCController controller)
        {
            Claim claim = Find(modId, npcId);
            if (claim == null)
            {
                claim = new Claim { ModId = modId, NpcId = npcId };
                claims.Add(claim);
            }
            claim.Controller = controller;
            return claim;
        }

        private static bool IsHeld(int npcId)
        {
            return claims.Exists(c => c.NpcId == npcId && c.Disabled);
        }

        private sealed class Claim
        {
            internal string ModId;
            internal int NpcId;
            internal NPCController Controller;
            internal bool Disabled;
            internal Activity Started;
        }
    }
}
