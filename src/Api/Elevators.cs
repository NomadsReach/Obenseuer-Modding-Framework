using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace ObenseuerCore
{
    public sealed class ElevatorInfo
    {
        public string Id { get; private set; }

        public int FloorCount { get; private set; }

        public int CurrentFloor { get; private set; }

        public bool Moving { get; private set; }

        internal ElevatorInfo(string id, int floorCount, int currentFloor, bool moving)
        {
            Id = id;
            FloorCount = floorCount;
            CurrentFloor = currentFloor;
            Moving = moving;
        }
    }

    public sealed class ElevatorArrivedArgs
    {
        public string Id { get; private set; }

        public int Floor { get; private set; }

        internal ElevatorArrivedArgs(string id, int floor)
        {
            Id = id;
            Floor = floor;
        }
    }

    public static class Elevators
    {
        internal const string Owner = "obenseuer.moddingframework.elevators";
        internal static readonly FieldInfo MovingField = AccessTools.Field(typeof(Elevator), "moving");
        internal static readonly FieldInfo QueueField = AccessTools.Field(typeof(Elevator), "Queue");
        private static bool installed;

        public static event Action<ElevatorArrivedArgs> Arrived;

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        public static List<ElevatorInfo> All()
        {
            List<ElevatorInfo> list = new List<ElevatorInfo>();
            try
            {
                foreach (Elevator elevator in UnityEngine.Object.FindObjectsByType<Elevator>(UnityEngine.FindObjectsSortMode.None))
                {
                    if (!string.IsNullOrEmpty(elevator.GUID)) list.Add(ToInfo(elevator));
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Elevators.All", e);
            }
            return list;
        }

        public static ElevatorInfo Get(string id)
        {
            Elevator elevator = Find(id);
            return elevator == null ? null : ToInfo(elevator);
        }

        public static int Floor(string id)
        {
            Elevator elevator = Find(id);
            return elevator == null ? 0 : CurrentFloor(elevator);
        }

        public static bool Moving(string id)
        {
            Elevator elevator = Find(id);
            return elevator != null && IsMoving(elevator);
        }

        public static bool GoToFloor(string id, int floor)
        {
            Elevator elevator = Find(id);
            if (elevator == null || MovingField == null || QueueField == null) return false;
            if (floor < 0 || floor >= elevator.Floors.Length) throw new ArgumentException("floor " + floor + " is not a floor of this elevator", "floor");
            if (SaveGate.Busy) return false;
            return Request(elevator, floor);
        }

        internal static void Init()
        {
            installed = ElevatorsPatches.Install();
            if (installed) return;
            CorePatcher.DisableOwner(Owner);
            CoreLog.For(Owner).Warn("elevator hooks incomplete, elevator events are disabled");
        }

        internal static void RaiseArrived(Elevator elevator)
        {
            if (Arrived == null) return;
            ElevatorArrivedArgs args = new ElevatorArrivedArgs(elevator.GUID, CurrentFloor(elevator));
            Events.Raise(Arrived, "Elevators.Arrived", h => h(args));
        }

        internal static bool IsMoving(Elevator elevator)
        {
            return MovingField != null && (int)MovingField.GetValue(elevator) != 0;
        }

        private static Elevator Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            try
            {
                foreach (Elevator elevator in UnityEngine.Object.FindObjectsByType<Elevator>(UnityEngine.FindObjectsSortMode.None))
                {
                    if (elevator.GUID == id) return elevator;
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Elevators.Find", e);
            }
            return null;
        }

        private static bool Request(Elevator elevator, int floor)
        {
            try
            {
                if (IsMoving(elevator) || elevator.Position == floor) return false;
                elevator.Call(floor);
                return ((List<float>)QueueField.GetValue(elevator)).Contains(floor);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Elevators.GoToFloor", e);
                return false;
            }
        }

        private static int CurrentFloor(Elevator elevator)
        {
            return (int)Math.Round(elevator.Position);
        }

        private static ElevatorInfo ToInfo(Elevator elevator)
        {
            return new ElevatorInfo(elevator.GUID, elevator.Floors.Length, CurrentFloor(elevator), IsMoving(elevator));
        }
    }
}
