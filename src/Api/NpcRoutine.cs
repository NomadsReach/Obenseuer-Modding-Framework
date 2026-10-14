using System;
using System.Collections.Generic;
using UnityEngine;

namespace ObenseuerCore
{
    [Flags]
    public enum NpcDays
    {
        None = 0,
        Monday = 1,
        Tuesday = 2,
        Wednesday = 4,
        Thursday = 8,
        Friday = 16,
        Saturday = 32,
        Sunday = 64,
        Weekdays = 31,
        Weekend = 96,
        Every = 127
    }

    public sealed class NpcRoutine
    {
        private const int MaxSteps = 64;
        private const int MinPoints = 2;
        private const int MaxPoints = 32;
        private const float MinRadius = 0.5f;
        private const float MaxRadius = 50f;
        private readonly List<NpcRoutineStep> steps = new List<NpcRoutineStep>();
        private readonly Dictionary<string, Vector3> doors = new Dictionary<string, Vector3>();

        public int Count
        {
            get
            {
                return steps.Count;
            }
        }

        public NpcRoutine Stand(NpcDays days, TimeSpan from, TimeSpan to, string scene, Vector3 point, float yaw)
        {
            RequireVector(point, "point");
            RequireNumber(yaw, "yaw");
            return Add(NpcStepKind.Stand, days, from, to, scene, new Vector3[] { point }, yaw, 0f);
        }

        public NpcRoutine Wander(NpcDays days, TimeSpan from, TimeSpan to, string scene, Vector3 center, float radius)
        {
            RequireVector(center, "center");
            if (!(radius >= MinRadius && radius <= MaxRadius)) throw new ArgumentException("radius must be 0.5 to 50", "radius");
            return Add(NpcStepKind.Wander, days, from, to, scene, new Vector3[] { center }, 0f, radius);
        }

        public NpcRoutine Patrol(NpcDays days, TimeSpan from, TimeSpan to, string scene, IList<Vector3> points)
        {
            return Add(NpcStepKind.Patrol, days, from, to, scene, CopyPoints(points), 0f, 0f);
        }

        public NpcRoutine Sit(NpcDays days, TimeSpan from, TimeSpan to, string scene, Vector3 seat)
        {
            RequireVector(seat, "seat");
            return Add(NpcStepKind.Sit, days, from, to, scene, new Vector3[] { seat }, 0f, 0f);
        }

        public NpcRoutine Sleep(NpcDays days, TimeSpan from, TimeSpan to, string scene, Vector3 bed)
        {
            RequireVector(bed, "bed");
            return Add(NpcStepKind.Sleep, days, from, to, scene, new Vector3[] { bed }, 0f, 0f);
        }

        public NpcRoutine Door(string scene, Vector3 point)
        {
            RequireScene(scene);
            RequireVector(point, "point");
            doors[scene] = point;
            return this;
        }

        internal NpcRoutineStep[] StepSnapshot()
        {
            return steps.ToArray();
        }

        internal Dictionary<string, Vector3> DoorSnapshot()
        {
            return new Dictionary<string, Vector3>(doors);
        }

        private NpcRoutine Add(NpcStepKind kind, NpcDays days, TimeSpan from, TimeSpan to, string scene, Vector3[] points, float yaw, float radius)
        {
            RequireScene(scene);
            if (from.Ticks < 0 || from.Ticks >= TimeSpan.TicksPerDay) throw new ArgumentException("from must be 00:00 to 23:59", "from");
            if (to.Ticks <= 0 || to.Ticks > TimeSpan.TicksPerDay) throw new ArgumentException("to must be 00:01 to 24:00", "to");
            int fromMinute = (int)(from.Ticks / TimeSpan.TicksPerMinute);
            int toMinute = (int)(to.Ticks / TimeSpan.TicksPerMinute);
            string problem = NpcRoutineTable.Check((int)days, fromMinute, toMinute);
            if (problem != null) throw new ArgumentException(problem, "days");
            if (steps.Count >= MaxSteps) throw new ArgumentException("a routine holds at most 64 steps");
            steps.Add(new NpcRoutineStep
            {
                Kind = kind,
                Scene = scene,
                Windows = NpcRoutineTable.Windows((int)days, fromMinute, toMinute),
                Points = points,
                Yaw = yaw,
                Radius = radius
            });
            return this;
        }

        private static void RequireScene(string scene)
        {
            if (string.IsNullOrEmpty(scene)) throw new ArgumentException("scene must be a non-empty string", "scene");
        }

        private static Vector3[] CopyPoints(IList<Vector3> points)
        {
            if (points == null) throw new ArgumentException("points must not be null", "points");
            if (points.Count < MinPoints || points.Count > MaxPoints) throw new ArgumentException("points must hold 2 to 32 points", "points");
            Vector3[] copy = new Vector3[points.Count];
            for (int i = 0; i < copy.Length; i++)
            {
                RequireVector(points[i], "points");
                copy[i] = points[i];
            }
            return copy;
        }

        private static void RequireVector(Vector3 value, string param)
        {
            if (!IsNumber(value.x) || !IsNumber(value.y) || !IsNumber(value.z)) throw new ArgumentException(param + " must be finite", param);
        }

        private static void RequireNumber(float value, string param)
        {
            if (!IsNumber(value)) throw new ArgumentException(param + " must be finite", param);
        }

        private static bool IsNumber(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }

    internal sealed class NpcRoutineStep
    {
        internal NpcStepKind Kind;
        internal string Scene;
        internal NpcWindow[] Windows;
        internal Vector3[] Points;
        internal float Yaw;
        internal float Radius;
    }
}
