using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using NPC;
using PixelCrushers.DialogueSystem;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ObenseuerCore
{
    [Flags]
    public enum NpcControlFlags
    {
        None = 0,
        Sight = 1,
        Talk = 2,
        Barks = 4,
        Fade = 8,
        Movement = 16,
        All = Sight | Talk | Barks | Fade | Movement
    }

    public static class NpcControl
    {
        internal const string Owner = "obenseuer.moddingframework.npccontrol";
        private const float LargeDistance = 1000000f;
        private static readonly FieldInfo culledField = AccessTools.Field(typeof(FadeGameObject), "culled");
        private static readonly List<Claim> claims = new List<Claim>();

        public static bool IsAvailable
        {
            get
            {
                return !CorePatcher.IsDisabled(Owner);
            }
        }

        public static bool Take(string modId, int npcId, NpcControlFlags flags = NpcControlFlags.All)
        {
            ModIds.Require(modId, "modId");
            RequireFlags(flags);
            if (!IsAvailable || SaveGate.Busy) return false;
            try
            {
                Prune();
                Claim claim = Find(npcId);
                if (claim != null && claim.ModId != modId) return false;
                NPCInfo npc = claim == null ? null : LiveClaimed(claim);
                if (npc == null && claim != null)
                {
                    Drop(claim);
                    claim = null;
                }
                if (npc == null) npc = LiveNpc(npcId, flags);
                if (npc == null) return false;
                NpcControlFlags before = claim == null ? NpcControlFlags.None : claim.Flags;
                Claim target = claim ?? new Claim { ModId = modId, NpcId = npcId, Info = npc };
                List<Edit> fresh = Build(npc, flags & ~before, target);
                if (claim == null) claims.Add(target);
                Update(target, flags, fresh);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "NpcControl.Take", e);
                return false;
            }
        }

        public static bool Release(string modId, int npcId)
        {
            ModIds.Require(modId, "modId");
            try
            {
                Prune();
                Claim claim = Find(npcId);
                if (claim == null || claim.ModId != modId) return false;
                Drop(claim);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "NpcControl.Release", e);
                return false;
            }
        }

        public static int ReleaseAll(string modId)
        {
            ModIds.Require(modId, "modId");
            try
            {
                Prune();
                List<Claim> mine = claims.FindAll(c => c.ModId == modId);
                foreach (Claim claim in mine) Drop(claim);
                return mine.Count;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "NpcControl.ReleaseAll", e);
                return 0;
            }
        }

        public static bool IsTaken(int npcId)
        {
            return TakenBy(npcId) != null;
        }

        public static string TakenBy(int npcId)
        {
            try
            {
                Prune();
                Claim claim = Find(npcId);
                return claim == null ? null : claim.ModId;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "NpcControl.TakenBy", e);
                return null;
            }
        }

        internal static bool HoldsMovement(int npcId)
        {
            Claim claim = Find(npcId);
            return claim != null && claim.Info != null && (claim.Flags & NpcControlFlags.Movement) != 0;
        }

        internal static bool HoldsBarks(int npcId)
        {
            return Holds(Find(npcId), NpcControlFlags.Barks);
        }

        internal static void Init()
        {
            Events.SceneLoadedWithMode += OnSceneLoaded;
            if (PatchGame()) return;
            CorePatcher.DisableOwner(Owner, "a hook on FadeGameObject, NPC_FOV, InteractableTalk, BarkStarter or DialogueSystemTrigger was not installed");
        }

        internal static void Tick()
        {
            if (claims.Count == 0) return;
            Prune();
            for (int i = claims.Count - 1; i >= 0; i--)
            {
                Claim claim = claims[i];
                if ((claim.Flags & NpcControlFlags.Movement) == 0 || !claim.Applied || Dead(claim)) continue;
                AssertMovement(claim);
                HoldIfNeeded(claim);
            }
        }

        private static bool PatchGame()
        {
            return CorePatcher.PatchCore(typeof(FadeGameObject), "Cull", new[] { typeof(bool) }, CorePatcher.Handler(typeof(NpcControl), "CullPrefix"), null, null, true, Owner)
                && CorePatcher.PatchCore(typeof(global::NPC.NPC_FOV), "CheckIfTargetIsDetected", new[] { typeof(Vector3), typeof(bool), typeof(bool) }, CorePatcher.Handler(typeof(NpcControl), "SightPrefix"), null, null, true, Owner)
                && CorePatcher.PatchCore(typeof(InteractableTalk), "CanTalk", Type.EmptyTypes, CorePatcher.Handler(typeof(NpcControl), "TalkPrefix"), null, null, true, Owner)
                && CorePatcher.PatchCore(typeof(BarkStarter), "TryBark", new[] { typeof(Transform), typeof(Transform) }, CorePatcher.Handler(typeof(NpcControl), "BarkPrefix"), null, null, true, Owner)
                && CorePatcher.PatchCore(typeof(DialogueSystemTrigger), "Fire", new[] { typeof(Transform) }, CorePatcher.Handler(typeof(NpcControl), "FirePrefix"), null, null, true, Owner);
        }

        private static void RequireFlags(NpcControlFlags flags)
        {
            if ((flags & ~NpcControlFlags.All) != 0) throw new ArgumentException("flags holds a value that is not an NpcControlFlags member", "flags");
        }

        private static NPCInfo LiveNpc(int npcId, NpcControlFlags flags)
        {
            NPCInfo npc = NPCManager.instance == null ? null : NPCManager.instance.FindNpcById(npcId);
            if (npc == null || npc.npcController == null || npc.IsDead()) return null;
            if (npc.gameObject.activeInHierarchy) return npc;
            return HiddenByFade(npc, npcId, (flags & NpcControlFlags.Fade) != 0) ? npc : null;
        }

        private static NPCInfo LiveClaimed(Claim claim)
        {
            if (claim.Info == null || claim.Info.IsDead()) return null;
            NPCManager manager = NPCManager.instance;
            if (manager == null) return claim.Info;
            return manager.FindNpcById(claim.NpcId) == claim.Info ? claim.Info : null;
        }

        private static bool HiddenByFade(NPCInfo npc, int npcId, bool fadeTaken)
        {
            FadeGameObject fade = npc.GetComponent<FadeGameObject>();
            if (fade == null || fade.hideAlways) return false;
            if (!fade.isHidden && !(fadeTaken && Culled(fade))) return false;
            Transform parent = npc.transform.parent;
            if (parent != null && !parent.gameObject.activeInHierarchy) return false;
            NPCData data = NPCDirector.instance == null ? null : NPCDirector.instance.GetNPCDataByID(npcId);
            return data == null || (data.state != null && data.state.currentScene == npc.gameObject.scene.name);
        }

        private static List<Edit> Build(NPCInfo npc, NpcControlFlags added, Claim owner)
        {
            List<Edit> list = new List<Edit>();
            if ((added & NpcControlFlags.Fade) != 0) BuildFade(npc, list);
            if ((added & NpcControlFlags.Movement) != 0) BuildMovement(npc, list, owner);
            return list;
        }

        private static void BuildFade(NPCInfo npc, List<Edit> list)
        {
            FadeGameObject fade = npc.GetComponent<FadeGameObject>();
            if (fade == null || !CulledFieldKnown()) return;
            list.Add(Pair<float>(NpcControlFlags.Fade, fade, () => fade.hideDistance, v => fade.hideDistance = v, LargeDistance));
            if (Culled(fade)) list.Add(Uncull(fade));
        }

        private static Edit Uncull(FadeGameObject fade)
        {
            return new Edit
            {
                Flag = NpcControlFlags.Fade,
                Target = fade,
                Apply = () =>
                {
                    culledField.SetValue(fade, false);
                    fade.Show();
                },
                Restore = () => { }
            };
        }

        private static bool CulledFieldKnown()
        {
            if (culledField != null && culledField.FieldType == typeof(bool)) return true;
            ApiLog.Once(Owner, "NpcControl.Fade", "FadeGameObject.culled was not found with the expected type, so the Fade flag does nothing");
            return false;
        }

        private static bool Culled(FadeGameObject fade)
        {
            return CulledFieldKnown() && (bool)culledField.GetValue(fade);
        }

        private static bool CullPrefix(FadeGameObject __instance)
        {
            try
            {
                return !HoldsCulling(__instance);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "NpcControl.Cull", e);
                return true;
            }
        }

        private static bool HoldsCulling(FadeGameObject fade)
        {
            if (claims.Count == 0 || !CulledFieldKnown()) return false;
            Claim claim = claims.Find(c => c.Info != null && c.Info.gameObject == fade.gameObject);
            return claim != null && (claim.Flags & NpcControlFlags.Fade) != 0 && !Dead(claim);
        }

        private static bool SightPrefix(global::NPC.NPC_FOV __instance)
        {
            return !Skip(__instance, NpcControlFlags.Sight, "NpcControl.Sight");
        }

        private static bool TalkPrefix(InteractableTalk __instance)
        {
            return !Skip(__instance, NpcControlFlags.Talk, "NpcControl.Talk");
        }

        private static bool BarkPrefix(BarkStarter __instance)
        {
            return !Skip(__instance, NpcControlFlags.Barks, "NpcControl.Barks");
        }

        private static bool FirePrefix(DialogueSystemTrigger __instance)
        {
            return string.IsNullOrEmpty(__instance.barkConversation) || !Skip(__instance, NpcControlFlags.Barks, "NpcControl.Barks");
        }

        private static bool Skip(Component part, NpcControlFlags flag, string what)
        {
            try
            {
                if (claims.Count == 0) return false;
                NPCInfo npc = part.GetComponentInParent<NPCInfo>();
                return npc != null && Holds(claims.Find(c => c.Info == npc), flag);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, what, e);
                return false;
            }
        }

        private static bool Holds(Claim claim, NpcControlFlags flag)
        {
            return claim != null && (claim.Flags & flag) != 0 && !Dead(claim);
        }

        private static void BuildMovement(NPCInfo npc, List<Edit> list, Claim owner)
        {
            NPCController controller = npc.npcController;
            if (controller == null || controller.richAI == null) return;
            bool original = controller.richAI.canMove;
            list.Add(new Edit
            {
                Flag = NpcControlFlags.Movement,
                Target = controller,
                Apply = () => controller.richAI.canMove = false,
                Restore = () =>
                {
                    if (!controller.richAI.canMove && !npc.Sleeping() && controller.GetComponentInParent<InteractableChair>() == null) controller.richAI.canMove = original || owner.GameCanMove;
                }
            });
        }

        private static Edit Pair<T>(NpcControlFlags flag, UnityEngine.Object target, Func<T> read, Action<T> write, T value)
        {
            T original = read();
            return new Edit
            {
                Flag = flag,
                Target = target,
                Apply = () => write(value),
                Restore = () =>
                {
                    if (EqualityComparer<T>.Default.Equals(read(), value)) write(original);
                }
            };
        }

        private static void Update(Claim claim, NpcControlFlags flags, List<Edit> fresh)
        {
            NpcControlFlags removed = claim.Flags & ~flags;
            if (removed != NpcControlFlags.None)
            {
                List<Edit> dropped = claim.Edits.FindAll(e => (e.Flag & removed) != 0);
                if (claim.Applied) RestoreEdits(claim, dropped);
                claim.Edits.RemoveAll(e => (e.Flag & removed) != 0);
                if ((removed & NpcControlFlags.Movement) != 0)
                {
                    claim.GameCanMove = false;
                    ReleaseHold(claim);
                }
            }
            claim.Flags = flags;
            claim.Edits.AddRange(fresh);
            if (claim.Applied)
            {
                ApplyEdits(fresh);
            }
            else
            {
                ApplyEdits(claim.Edits);
                claim.Applied = true;
            }
            if ((flags & NpcControlFlags.Movement) != 0) HoldIfNeeded(claim);
        }

        private static void AssertMovement(Claim claim)
        {
            try
            {
                NPCController controller = claim.Info.npcController;
                if (controller == null || controller.richAI == null) return;
                if (controller.richAI.canMove) claim.GameCanMove = true;
                controller.richAI.canMove = false;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "NpcControl.AssertMovement", e);
            }
        }

        private static void HoldIfNeeded(Claim claim)
        {
            if (claim.Held) return;
            NPCController controller = NpcMovement.LiveController(claim.NpcId);
            if (controller == null || controller.activityDisabled) return;
            claim.Held = NpcMovement.SetActivityEnabled(Owner, claim.NpcId, false);
        }

        private static void ReleaseHold(Claim claim)
        {
            if (!claim.Held) return;
            claim.Held = false;
            NpcMovement.SetActivityEnabled(Owner, claim.NpcId, true);
        }

        private static void Drop(Claim claim)
        {
            claims.Remove(claim);
            if (claim.Applied) RestoreEdits(claim, claim.Edits);
            ReleaseHold(claim);
        }

        private static void Prune()
        {
            for (int i = claims.Count - 1; i >= 0; i--)
            {
                Claim claim = claims[i];
                if (claim.Info == null || Dead(claim)) Drop(claim);
            }
        }

        private static void ApplyEdits(List<Edit> edits)
        {
            foreach (Edit edit in edits) Run(edit.Target, edit.Apply, "NpcControl.Apply");
        }

        private static void RestoreEdits(Claim claim, List<Edit> edits)
        {
            bool dead = Dead(claim);
            foreach (Edit edit in edits)
            {
                if (dead && edit.Flag != NpcControlFlags.Fade) continue;
                Run(edit.Target, edit.Restore, "NpcControl.Restore");
            }
        }

        private static bool Dead(Claim claim)
        {
            if (claim.Info == null) return true;
            try
            {
                return claim.Info.IsDead();
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "NpcControl.Dead", e);
                return true;
            }
        }

        private static void Run(UnityEngine.Object target, Action act, string what)
        {
            if (target == null) return;
            try
            {
                act();
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, what, e);
            }
        }

        private static void OnSceneLoaded(string scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single) return;
            try
            {
                foreach (Claim claim in new List<Claim>(claims)) Drop(claim);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "NpcControl.SceneLoaded", e);
            }
        }

        private static Claim Find(int npcId)
        {
            return claims.Find(c => c.NpcId == npcId);
        }

        private sealed class Claim
        {
            internal string ModId;
            internal int NpcId;
            internal NPCInfo Info;
            internal NpcControlFlags Flags;
            internal bool Applied;
            internal bool Held;
            internal bool GameCanMove;
            internal List<Edit> Edits = new List<Edit>();
        }

        private sealed class Edit
        {
            internal NpcControlFlags Flag;
            internal UnityEngine.Object Target;
            internal Action Apply;
            internal Action Restore;
        }
    }
}
