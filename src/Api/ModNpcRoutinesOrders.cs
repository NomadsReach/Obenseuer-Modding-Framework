using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using NPC;
using Pathfinding;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ObenseuerCore
{
    public enum NpcStepKind { Away, Stand, Wander, Patrol, Sit, Sleep }

    public static partial class ModNpcRoutines
    {
        internal const string Owner = "obenseuer.moddingframework.modnpcroutines";
        private const float ArriveDistance = 0.6f;
        private const float OriginNudge = 0.05f;
        private const float SeatRange = 1.5f;
        private const float BedRange = 2f;
        private const float WanderLeash = 1.5f;
        private const int WanderAttempts = 5;
        private const float MinRadius = 0.5f;
        private const float MaxRadius = 50f;
        private const int MinPatrolPoints = 2;
        private const int MaxPatrolPoints = 32;
        private static readonly List<NpcOrder> orders = new List<NpcOrder>();
        private static readonly System.Random random = new System.Random();

        public static bool IsAvailable
        {
            get
            {
                return ModNpcs.IsAvailable;
            }
        }

        public static bool WalkTo(string modId, string npcId, Vector3 point, float yaw, bool run, Action<bool> done = null)
        {
            ModIds.Require(modId, "modId");
            RequireVector(point, "point");
            RequireNumber(yaw, "yaw");
            return Begin(modId, npcId, NpcStepKind.Stand, new Vector3[] { point }, yaw, 0f, run, false, true, done, "ModNpcRoutines.WalkTo");
        }

        public static bool Wander(string modId, string npcId, Vector3 center, float radius)
        {
            ModIds.Require(modId, "modId");
            RequireVector(center, "center");
            if (!(radius >= MinRadius && radius <= MaxRadius)) throw new ArgumentException("radius must be 0.5 to 50", "radius");
            return Begin(modId, npcId, NpcStepKind.Wander, new Vector3[] { center }, 0f, radius, false, false, false, null, "ModNpcRoutines.Wander");
        }

        public static bool Patrol(string modId, string npcId, IList<Vector3> points, bool run)
        {
            ModIds.Require(modId, "modId");
            return Begin(modId, npcId, NpcStepKind.Patrol, CopyPoints(points), 0f, 0f, run, false, false, null, "ModNpcRoutines.Patrol");
        }

        public static bool Sit(string modId, string npcId, Vector3 seat)
        {
            ModIds.Require(modId, "modId");
            RequireVector(seat, "seat");
            return Begin(modId, npcId, NpcStepKind.Sit, new Vector3[] { seat }, 0f, 0f, false, false, false, null, "ModNpcRoutines.Sit");
        }

        public static bool Sleep(string modId, string npcId, Vector3 bed)
        {
            ModIds.Require(modId, "modId");
            RequireVector(bed, "bed");
            return Begin(modId, npcId, NpcStepKind.Sleep, new Vector3[] { bed }, 0f, 0f, false, false, false, null, "ModNpcRoutines.Sleep");
        }

        public static bool Stop(string modId, string npcId)
        {
            ModIds.Require(modId, "modId");
            NpcOrder order = Find(modId, npcId);
            if (SaveGate.Busy || order == null) return false;
            Finish(order, false);
            return true;
        }

        internal static void InitOrders()
        {
            Events.SceneLoadedWithMode += OnSceneLoaded;
        }

        internal static void TickOrders()
        {
            if (orders.Count == 0 || SaveGate.Busy) return;
            for (int i = orders.Count - 1; i >= 0; i--)
            {
                if (i >= orders.Count) continue;
                NpcOrder order = orders[i];
                try
                {
                    Advance(order);
                }
                catch (Exception e)
                {
                    ApiLog.Once(Owner, "ModNpcRoutines.TickOrders", e);
                    Finish(order, false);
                }
            }
        }

        internal static bool StartStep(string modId, string npcId, NpcStepKind kind, Vector3[] points, float yaw, float radius, Action<bool> done)
        {
            ModIds.Require(modId, "modId");
            if (HasOrder(modId, npcId)) return false;
            switch (kind)
            {
                case NpcStepKind.Stand:
                    return Begin(modId, npcId, kind, points, yaw, 0f, false, true, false, done, "ModNpcRoutines.StartStep");
                case NpcStepKind.Wander:
                    return Begin(modId, npcId, kind, points, 0f, radius, false, true, false, done, "ModNpcRoutines.StartStep");
                case NpcStepKind.Patrol:
                case NpcStepKind.Sit:
                case NpcStepKind.Sleep:
                    return Begin(modId, npcId, kind, points, 0f, 0f, false, true, false, done, "ModNpcRoutines.StartStep");
                default:
                    return false;
            }
        }

        internal static void EndStep(string modId, string npcId)
        {
            NpcOrder order = Find(modId, npcId);
            if (order != null && order.Routine) Finish(order, false);
        }

        internal static void EndAll(string modId, string npcId)
        {
            NpcOrder order = Find(modId, npcId);
            if (order != null) Finish(order, false);
        }

        internal static bool HasOrder(string modId, string npcId)
        {
            NpcOrder order = Find(modId, npcId);
            return order != null && !order.Routine;
        }

        internal static bool HasStep(string modId, string npcId)
        {
            NpcOrder order = Find(modId, npcId);
            return order != null && order.Routine;
        }

        internal static bool IsTalking(int characterId)
        {
            return IsTalking(NPCManager.instance == null ? null : NPCManager.instance.FindNpcById(characterId));
        }

        internal static bool IsTalking(NPCInfo npc)
        {
            if (npc == null || npc.interactableTalk == null || DialogueController.instance == null) return false;
            return DialogueController.instance.currentInteractableTalk == npc.interactableTalk;
        }

        private static bool Begin(string modId, string npcId, NpcStepKind kind, Vector3[] points, float yaw, float radius, bool run, bool routine, bool once, Action<bool> done, string member)
        {
            if (!IsAvailable || SaveGate.Busy || string.IsNullOrEmpty(npcId) || AstarPath.active == null) return false;
            try
            {
                int characterId = ModNpcs.CharacterIdOf(modId, npcId);
                NPCController controller = characterId == 0 ? null : NpcMovement.LiveController(characterId);
                if (controller == null || controller.richAI == null || controller.activityDisabled) return false;
                NPCInfo info = NPCManager.instance.FindNpcById(characterId);
                Activity activity = null;
                if (kind == NpcStepKind.Sit || kind == NpcStepKind.Sleep)
                {
                    activity = kind == NpcStepKind.Sit ? SitActivity(points[0]) : SleepActivity(points[0]);
                    if (activity == null)
                    {
                        if (!routine) return false;
                        ApiLog.Once(Owner, "routine seat", "a routine seat step found no chair or bed in range, so the NPC stands at the step's point");
                        kind = NpcStepKind.Stand;
                    }
                }
                NpcOrder previous = Find(modId, npcId);
                if (previous != null)
                {
                    Finish(previous, false);
                    if (Find(modId, npcId) != null) return false;
                }
                if (controller.Data.state.CurrentActivity != null) controller.ForceStopActivity();
                NpcOrder order = new NpcOrder
                {
                    ModId = modId, NpcId = npcId, CharacterId = characterId, Controller = controller, Info = info, Kind = kind, Points = points,
                    Yaw = yaw, Radius = radius, Run = run, Routine = routine, Once = once, Done = done, Activity = activity
                };
                if (activity != null) controller.ForceStartActivity(activity);
                else if (kind != NpcStepKind.Wander) StartWalk(order, controller, points[0]);
                orders.Add(order);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, member, e);
                return false;
            }
        }

        private static void Advance(NpcOrder order)
        {
            NPCInfo npc = order.Info;
            NPCController controller = order.Controller;
            if (npc == null || controller == null || npc.npcController != controller || !npc.gameObject.activeInHierarchy || npc.IsDead() || controller.Data == null || controller.Data.state == null)
            {
                Finish(order, false);
                return;
            }
            if (controller.activityDisabled || IsTalking(npc)) return;
            Activity current = controller.Data.state.CurrentActivity;
            if (order.Activity != null)
            {
                if (current != order.Activity || !order.Activity.running) Finish(order, false);
                return;
            }
            if (current != null && current.running) return;
            if (order.Walking)
            {
                if (order.Arrived)
                {
                    Arrive(order, controller);
                    return;
                }
                order.Timer -= Time.deltaTime;
                if (order.Timer <= 0f) TimeOut(order, controller);
                return;
            }
            if (order.Kind != NpcStepKind.Wander) return;
            order.Timer -= Time.deltaTime;
            if (order.Timer > 0f) return;
            Vector3 next;
            if (PickWanderPoint(order, out next)) StartWalk(order, controller, next);
            else order.Timer = NpcWalkMath.WaitSeconds(random);
        }

        private static void StartWalk(NpcOrder order, NPCController controller, Vector3 point)
        {
            if (NpcWalkMath.IsOrigin(point.x, point.y, point.z)) point.x += OriginNudge;
            order.Target = point;
            order.Walking = true;
            order.Arrived = false;
            SetSpeed(controller, order.Run);
            order.Timer = NpcWalkMath.Timeout(Vector3.Distance(controller.transform.position, point), controller.richAI.maxSpeed);
            controller.SetTargetPosition(point, ArriveDistance, () => order.Arrived = true);
        }

        private static void Arrive(NpcOrder order, NPCController controller)
        {
            order.Walking = false;
            if (order.Kind == NpcStepKind.Wander)
            {
                order.Timer = NpcWalkMath.WaitSeconds(random);
            }
            else if (order.Kind == NpcStepKind.Patrol)
            {
                order.Index = NpcWalkMath.NextPatrolIndex(order.Index, order.Points.Length);
                StartWalk(order, controller, order.Points[order.Index]);
            }
            else
            {
                controller.transform.rotation = Quaternion.Euler(0f, order.Yaw, 0f);
                if (order.Once) Finish(order, true);
                else Report(order, true);
            }
        }

        private static void TimeOut(NpcOrder order, NPCController controller)
        {
            ApiLog.Once(Owner, "walk timeout", "a walk did not arrive in time, the order moves on or ends");
            if (order.Routine) controller.richAI.Teleport(order.Target);
            if (order.Routine || order.Kind == NpcStepKind.Wander || order.Kind == NpcStepKind.Patrol) Arrive(order, controller);
            else Finish(order, false);
        }

        private static bool PickWanderPoint(NpcOrder order, out Vector3 point)
        {
            Vector3 center = order.Points[0];
            for (int attempt = 0; attempt < WanderAttempts; attempt++)
            {
                float dx;
                float dz;
                NpcWalkMath.DiscPoint(random, order.Radius, out dx, out dz);
                NNInfo near = AstarPath.active.GetNearest(new Vector3(center.x + dx, center.y, center.z + dz));
                if (near.node == null) continue;
                Vector3 offset = near.position - center;
                offset.y = 0f;
                if (offset.magnitude > order.Radius * WanderLeash) continue;
                point = near.position;
                return true;
            }
            point = center;
            return false;
        }

        private static void SetSpeed(NPCController controller, bool run)
        {
            if (controller.movementAnimation == null) return;
            controller.movementAnimation.SetMovementSpeed(run ? OpenSewerNPCMovementAnimation.MovementTypes.Run : OpenSewerNPCMovementAnimation.MovementTypes.SlowWalk);
        }

        private static void Finish(NpcOrder order, bool success)
        {
            orders.Remove(order);
            StopOrder(order);
            Report(order, success);
        }

        private static void StopOrder(NpcOrder order)
        {
            NPCController controller = order.Controller;
            if (controller == null || controller.Data == null || controller.Data.state == null) return;
            try
            {
                if (order.Activity != null && controller.Data.state.CurrentActivity == order.Activity) controller.ForceStopActivity();
                if (controller.richAI == null || order.Activity != null) return;
                SetSpeed(controller, false);
                controller.SetTargetPosition(controller.transform.position, ArriveDistance, null);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "ModNpcRoutines.StopOrder", e);
            }
        }

        private static void Report(NpcOrder order, bool success)
        {
            Action<bool> done = order.Done;
            order.Done = null;
            if (done == null) return;
            try
            {
                done(success);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "ModNpcRoutines done", e);
            }
        }

        private static Activity SitActivity(Vector3 point)
        {
            InteractableChair chair = NearestChair(point);
            if (chair == null) return null;
            Act_sit sit = new Act_sit();
            sit.activityName = "framework sit";
            sit.interactableChair = new InteractableChairReference();
            sit.interactableChair.chair = chair;
            return sit;
        }

        private static Activity SleepActivity(Vector3 point)
        {
            global::Sleep bed = NearestBed(point);
            if (bed == null) return null;
            Act_Sleep sleep = new Act_Sleep();
            sleep.activityName = "framework sleep";
            sleep.bed = new SleepReference(bed);
            return sleep;
        }

        private static readonly FieldInfo hideNpcField = AccessTools.Field(typeof(InteractableChair), "hideNpc");

        private static bool HidesNpc(InteractableChair chair)
        {
            try
            {
                return hideNpcField != null && (bool)hideNpcField.GetValue(chair);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static InteractableChair NearestChair(Vector3 point)
        {
            InteractableChair found = null;
            float best = SeatRange;
            foreach (InteractableChair chair in SceneIds.Active<InteractableChair>())
            {
                if (!chair.gameObject.activeInHierarchy || HidesNpc(chair)) continue;
                float distance = Vector3.Distance(chair.transform.position, point);
                if (distance <= best)
                {
                    found = chair;
                    best = distance;
                }
            }
            return found;
        }

        private static global::Sleep NearestBed(Vector3 point)
        {
            global::Sleep found = null;
            float best = BedRange;
            foreach (global::Sleep bed in SceneIds.Active<global::Sleep>())
            {
                if (bed.occupied || !bed.gameObject.activeInHierarchy) continue;
                float distance = Vector3.Distance(bed.transform.position, point);
                if (distance <= best)
                {
                    found = bed;
                    best = distance;
                }
            }
            return found;
        }

        private static NpcOrder Find(string modId, string npcId)
        {
            return orders.Find(o => o.ModId == modId && o.NpcId == npcId);
        }

        private static void OnSceneLoaded(string scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single) return;
            foreach (NpcOrder order in new List<NpcOrder>(orders)) Finish(order, false);
        }

        private static Vector3[] CopyPoints(IList<Vector3> points)
        {
            if (points == null) throw new ArgumentException("points must not be null", "points");
            if (points.Count < MinPatrolPoints || points.Count > MaxPatrolPoints) throw new ArgumentException("points must hold 2 to 32 points", "points");
            Vector3[] copy = new Vector3[points.Count];
            for (int i = 0; i < copy.Length; i++)
            {
                if (!Finite(points[i])) throw new ArgumentException("every patrol point must be finite", "points");
                copy[i] = points[i];
            }
            return copy;
        }

        private static void RequireVector(Vector3 value, string param)
        {
            if (!Finite(value)) throw new ArgumentException(param + " must be finite", param);
        }

        private static void RequireNumber(float value, string param)
        {
            if (!IsNumber(value)) throw new ArgumentException(param + " must be finite", param);
        }

        private static bool Finite(Vector3 v)
        {
            return IsNumber(v.x) && IsNumber(v.y) && IsNumber(v.z);
        }

        private static bool IsNumber(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }

    internal sealed class NpcOrder
    {
        internal string ModId;
        internal string NpcId;
        internal int CharacterId;
        internal NPCController Controller;
        internal NPCInfo Info;
        internal NpcStepKind Kind;
        internal Vector3[] Points;
        internal float Yaw;
        internal float Radius;
        internal bool Run;
        internal bool Routine;
        internal bool Once;
        internal Action<bool> Done;
        internal Activity Activity;
        internal Vector3 Target;
        internal int Index;
        internal bool Walking;
        internal bool Arrived;
        internal float Timer;
    }
}
