using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace ObenseuerCore
{
    public sealed class SeatInfo
    {
        public string Id { get; private set; }

        public bool IsOccupied { get; private set; }

        public bool OccupiedByPlayer { get; private set; }

        public Vector3 Position { get; private set; }

        public bool IsToilet { get; private set; }

        internal SeatInfo(string id, bool isOccupied, bool occupiedByPlayer, Vector3 position, bool isToilet)
        {
            Id = id;
            IsOccupied = isOccupied;
            OccupiedByPlayer = occupiedByPlayer;
            Position = position;
            IsToilet = isToilet;
        }
    }

    public sealed class SeatArgs
    {
        public string Id { get; private set; }

        public bool ByPlayer { get; private set; }

        internal SeatArgs(string id, bool byPlayer)
        {
            Id = id;
            ByPlayer = byPlayer;
        }
    }

    public static class Seats
    {
        internal const string Owner = "obenseuer.moddingframework.seats";
        private const float UseDistance = 1.5f;
        private static bool installed;
        private static readonly FieldInfo disableStopSitField = AccessTools.Field(typeof(InteractableChair), "disableStopSit");
        private static readonly FieldInfo currentCoroutineField = AccessTools.Field(typeof(InteractableChair), "currentCoroutine");

        public static event Action<SeatArgs> Sat;

        public static event Action<SeatArgs> Left;

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        public static List<SeatInfo> All()
        {
            List<SeatInfo> list = new List<SeatInfo>();
            try
            {
                foreach (InteractableChair chair in Chairs()) list.Add(ToInfo(chair));
                return list;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "All", e);
                return new List<SeatInfo>();
            }
        }

        public static SeatInfo Get(string id)
        {
            try
            {
                InteractableChair chair = SceneIds.Find<InteractableChair>(id);
                return chair == null ? null : ToInfo(chair);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Get", e);
                return null;
            }
        }

        public static bool Sit(string id)
        {
            try
            {
                InteractableChair chair = SceneIds.Find<InteractableChair>(id);
                if (chair == null || InteractableChair.PlayerIsSitting || !CanUse()) return false;
                OpenSewerCharacterController controller = OpenSewerCharacterController.instance;
                if (controller == null || controller.ControlsDisabled()) return false;
                if (chair.IsInteractDisabled() || chair.Occupied() || TooFar(chair)) return false;
                chair.Interact();
                return chair.PlayerSittingHere;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Sit", e);
                return false;
            }
        }

        public static bool Leave()
        {
            try
            {
                InteractableChair chair = Seated();
                if (chair == null || !CanUse()) return false;
                Toilet toilet = chair as Toilet;
                if (toilet != null && !toilet.enabled) return false;
                if ((bool)disableStopSitField.GetValue(chair) || currentCoroutineField.GetValue(chair) != null) return false;
                chair.Interact();
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Leave", e);
                return false;
            }
        }

        internal static bool HasHandlers
        {
            get
            {
                return Sat != null || Left != null;
            }
        }

        internal static void Init()
        {
            installed = disableStopSitField != null && currentCoroutineField != null && SeatsPatches.Install();
            if (installed) return;
            CorePatcher.DisableOwner(Owner);
            CoreLog.For(Owner).Warn("seat hooks incomplete, seat events are disabled");
        }

        internal static void RaiseSat(InteractableChair chair, bool byPlayer)
        {
            SeatArgs args = new SeatArgs(SceneIds.Of(chair), byPlayer);
            Events.Raise(Sat, "Seats.Sat", h => h(args));
        }

        internal static void RaiseLeft(InteractableChair chair, bool byPlayer)
        {
            SeatArgs args = new SeatArgs(SceneIds.Of(chair), byPlayer);
            Events.Raise(Left, "Seats.Left", h => h(args));
        }

        private static InteractableChair[] Chairs()
        {
            return UnityEngine.Object.FindObjectsByType<InteractableChair>(FindObjectsSortMode.None);
        }

        private static InteractableChair Seated()
        {
            foreach (InteractableChair chair in Chairs())
            {
                if (chair.PlayerSittingHere) return chair;
            }
            return null;
        }

        private static bool CanUse()
        {
            GameController game = GameController.instance;
            if (game == null || InteractObjects.instance == null || ThirdPersonCameraController.instance == null) return false;
            return !CoreInput.GameBusy() && !game.keysDisabled && !game.GameIsPaused && !PlayerState.IsSleeping;
        }

        private static bool TooFar(InteractableChair chair)
        {
            return Vector3.Distance(ThirdPersonCameraController.instance.transform.position, chair.transform.position) > UseDistance;
        }

        private static SeatInfo ToInfo(InteractableChair chair)
        {
            bool byPlayer = chair.PlayerSittingHere;
            return new SeatInfo(SceneIds.Of(chair), byPlayer || chair.Occupied(), byPlayer, chair.transform.position, chair is Toilet);
        }
    }
}
