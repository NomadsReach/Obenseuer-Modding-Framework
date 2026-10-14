using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace ObenseuerCore
{
    public sealed class TrackTrainInfo
    {
        public string Id { get; private set; }

        public float Speed { get; private set; }

        public Vector3 Position { get; private set; }

        public string FrontCoupledTo { get; private set; }

        public string BackCoupledTo { get; private set; }

        public string WagonType { get; private set; }

        public string CargoGuid { get; private set; }

        internal TrackTrainInfo(string id, float speed, Vector3 position, string frontCoupledTo, string backCoupledTo, string wagonType, string cargoGuid)
        {
            Id = id;
            Speed = speed;
            Position = position;
            FrontCoupledTo = frontCoupledTo;
            BackCoupledTo = backCoupledTo;
            WagonType = wagonType;
            CargoGuid = cargoGuid;
        }
    }

    public sealed class TrackSegmentInfo
    {
        public string Id { get; private set; }

        public bool Occupied { get; private set; }

        public string OccupiedBy { get; private set; }

        public int ActiveBranch { get; private set; }

        public string Previous { get; private set; }

        public List<string> Next { get; private set; }

        internal TrackSegmentInfo(string id, bool occupied, string occupiedBy, int activeBranch, string previous, List<string> next)
        {
            Id = id;
            Occupied = occupied;
            OccupiedBy = occupiedBy;
            ActiveBranch = activeBranch;
            Previous = previous;
            Next = next;
        }
    }

    public sealed class MinitrainInfo
    {
        public float Fuel { get; private set; }

        public float Capacity { get; private set; }

        public bool Running { get; private set; }

        public float Speed { get; private set; }

        internal MinitrainInfo(float fuel, float capacity, bool running, float speed)
        {
            Fuel = fuel;
            Capacity = capacity;
            Running = running;
            Speed = speed;
        }
    }

    public sealed class TrainArgs
    {
        public string Id { get; private set; }

        public float Speed { get; private set; }

        internal TrainArgs(string id, float speed)
        {
            Id = id;
            Speed = speed;
        }
    }

    public sealed class TrainCrashArgs
    {
        public string Id { get; private set; }

        public float Speed { get; private set; }

        public string OtherTrainId { get; private set; }

        internal TrainCrashArgs(string id, float speed, string otherTrainId)
        {
            Id = id;
            Speed = speed;
            OtherTrainId = otherTrainId;
        }
    }

    public static class Trains
    {
        internal const string Owner = "obenseuer.moddingframework.trains";
        internal const string CrashOwner = Owner + ".crash";
        private static readonly FieldInfo BranchRotations = typeof(MinitrainSwitch).GetField("branchRotations", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly HashSet<string> crashedIds = new HashSet<string>();
        private static bool installed;
        private static int crashFrame = -1;

        public static event Action<TrainArgs> Departed;
        public static event Action<TrainArgs> Arrived;
        public static event Action<TrainCrashArgs> Crashed;

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        public static MinitrainInfo Minitrain
        {
            get
            {
                try
                {
                    global::Minitrain first = null;
                    foreach (global::Minitrain minitrain in SceneIds.Active<global::Minitrain>())
                    {
                        if (minitrain == null) continue;
                        if (minitrain.playerUsing) return ToInfo(minitrain);
                        if (first == null) first = minitrain;
                    }
                    if (first != null) return ToInfo(first);
                }
                catch (Exception e)
                {
                    ApiLog.Once(Owner, "Trains.Minitrain", e);
                }
                return null;
            }
        }

        public static List<TrackTrainInfo> All()
        {
            List<TrackTrainInfo> list = new List<TrackTrainInfo>();
            try
            {
                TrainController controller = TrainController.instance;
                if (controller == null || controller.TrackTrains == null) return list;
                foreach (TrackTrain train in controller.TrackTrains.Values)
                {
                    if (train != null) list.Add(ToInfo(train));
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Trains.All", e);
            }
            return list;
        }

        public static List<GameObject> Switches
        {
            get
            {
                List<GameObject> list = new List<GameObject>();
                try
                {
                    foreach (MinitrainSwitch found in SceneIds.Active<MinitrainSwitch>())
                    {
                        if (found != null) list.Add(found.gameObject);
                    }
                }
                catch (Exception e)
                {
                    CoreLog.For(Owner).Error("Trains.Switches failed: " + e.ToString());
                }
                return list;
            }
        }

        public static List<GameObject> Loaders
        {
            get
            {
                List<GameObject> list = new List<GameObject>();
                try
                {
                    foreach (MinitrainWagonLoader found in SceneIds.Active<MinitrainWagonLoader>())
                    {
                        if (found != null) list.Add(found.gameObject);
                    }
                }
                catch (Exception e)
                {
                    CoreLog.For(Owner).Error("Trains.Loaders failed: " + e.ToString());
                }
                return list;
            }
        }

        public static List<TrackSegmentInfo> Segments()
        {
            List<TrackSegmentInfo> list = new List<TrackSegmentInfo>();
            foreach (Track_Segment segment in SceneIds.Active<Track_Segment>())
            {
                if (segment == null) continue;
                try
                {
                    list.Add(ToInfo(segment));
                }
                catch (Exception e)
                {
                    ApiLog.Once(Owner, "Trains.Segments", e);
                }
            }
            return list;
        }

        public static int GetBranch(GameObject target)
        {
            Require(target);
            try
            {
                MinitrainSwitch track = target.GetComponent<MinitrainSwitch>();
                return track == null ? -1 : track.currentBranchIndex;
            }
            catch (Exception e)
            {
                CoreLog.For(Owner).Error("Trains.GetBranch failed: " + e.ToString());
                return -1;
            }
        }

        public static bool SetBranch(GameObject target, int branch, bool force)
        {
            Require(target);
            if (branch < 0) throw new ArgumentOutOfRangeException("branch");
            if (SaveGate.Busy) return false;
            try
            {
                MinitrainSwitch track = target.GetComponent<MinitrainSwitch>();
                if (track == null) return false;
                Vector3[] rotations = (Vector3[])BranchRotations.GetValue(track);
                if (branch >= rotations.Length) return false;
                if (!force && SwitchBlocked(track)) return false;
                if (force) track.ChangeTrackForce(branch);
                else track.ChangeTrack(branch);
                return true;
            }
            catch (Exception e)
            {
                CoreLog.For(Owner).Error("Trains.SetBranch failed: " + e.ToString());
                return false;
            }
        }

        public static bool SetLoaderOn(GameObject target, bool on)
        {
            Require(target);
            if (SaveGate.Busy) return false;
            try
            {
                MinitrainWagonLoader loader = target.GetComponent<MinitrainWagonLoader>();
                if (loader == null) return false;
                loader.Toggle(on);
                return loader.active == on;
            }
            catch (Exception e)
            {
                CoreLog.For(Owner).Error("Trains.SetLoaderOn failed: " + e.ToString());
                return false;
            }
        }

        public static bool SetLoaderFreeToTake(GameObject target, bool freeToTake)
        {
            Require(target);
            if (SaveGate.Busy) return false;
            try
            {
                MinitrainWagonLoader loader = target.GetComponent<MinitrainWagonLoader>();
                if (loader == null) return false;
                loader.SetOwnerPermissions(freeToTake);
                return loader.permissionToTake == freeToTake;
            }
            catch (Exception e)
            {
                CoreLog.For(Owner).Error("Trains.SetLoaderFreeToTake failed: " + e.ToString());
                return false;
            }
        }

        internal static void Init()
        {
            installed = TrainsPatches.Install();
            TrainsPatches.InstallCrash();
            if (installed) return;
            CorePatcher.DisableOwner(Owner);
            CoreLog.For(Owner).Warn("train hooks incomplete, train events are disabled");
        }

        internal static void RaiseDeparted(TrackTrain train)
        {
            if (Departed == null) return;
            TrainArgs args = new TrainArgs(train.GUID, train.currentSpeed);
            Events.Raise(Departed, "Trains.Departed", h => h(args));
        }

        internal static void RaiseArrived(TrackTrain train)
        {
            if (Arrived == null) return;
            TrainArgs args = new TrainArgs(train.GUID, train.currentSpeed);
            Events.Raise(Arrived, "Trains.Arrived", h => h(args));
        }

        internal static void RaiseCrashed(TrackTrain train, GameObject other, float speed)
        {
            if (Crashed == null) return;
            TrackTrain otherTrain = other == null ? null : other.GetComponentInParent<TrackTrain>();
            if (Time.frameCount != crashFrame)
            {
                crashFrame = Time.frameCount;
                crashedIds.Clear();
            }
            if (crashedIds.Contains(train.GUID)) return;
            crashedIds.Add(train.GUID);
            if (otherTrain != null) crashedIds.Add(otherTrain.GUID);
            TrainCrashArgs args = new TrainCrashArgs(train.GUID, speed, otherTrain == null ? null : otherTrain.GUID);
            Events.Raise(Crashed, "Trains.Crashed", h => h(args));
        }

        private static MinitrainInfo ToInfo(global::Minitrain minitrain)
        {
            MinitrainFuelTank tank = minitrain.fuelTank;
            TrackTrain train = minitrain.GetTrain();
            return new MinitrainInfo(tank == null ? 0f : tank.currentFuelAmount, tank == null ? 0f : tank.maxFuelAmount, minitrain.isOn, train == null ? 0f : train.currentSpeed);
        }

        private static TrackTrainInfo ToInfo(TrackTrain train)
        {
            global::MinitrainWagonData data = train.GetComponentInChildren<global::MinitrainWagonData>(true);
            string wagonType = data == null ? "None" : data.wagonType.ToString();
            string cargo = data == null || data.connectedStorage == null || string.IsNullOrEmpty(data.connectedStorage.GUID) ? null : data.connectedStorage.GUID;
            return new TrackTrainInfo(train.GUID, train.currentSpeed, train.transform.position, CoupledTo(train.frontCoupler), CoupledTo(train.backCoupler), wagonType, cargo);
        }

        private static string CoupledTo(TrackTrain.TrainCoupler coupler)
        {
            if (coupler == null || coupler.coupledTo == null || coupler.coupledTo.parent == null) return null;
            return coupler.coupledTo.parent.GUID;
        }

        private static TrackSegmentInfo ToInfo(Track_Segment segment)
        {
            List<string> next = new List<string>();
            foreach (Track_Segment.SegmentBranch branch in segment.Branches)
            {
                next.Add(branch == null || branch.nextSegment == null ? null : SceneIds.Of(branch.nextSegment));
            }
            TrackTrain train = segment.occupiedBy;
            Track_Segment previous = segment.previousSegment;
            return new TrackSegmentInfo(SceneIds.Of(segment), segment.occupied, train == null ? null : train.GUID, (int)segment.activeBranchIndex, previous == null ? null : SceneIds.Of(previous), next);
        }

        private static bool SwitchBlocked(MinitrainSwitch track)
        {
            if (track.isReversed) return track.trackSegment.previousSegment != null && track.trackSegment.previousSegment.occupied;
            return track.trackSegment.occupied;
        }

        private static void Require(GameObject target)
        {
            if (target == null) throw new ArgumentNullException("target");
        }
    }
}
