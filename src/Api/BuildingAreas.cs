using System;

namespace ObenseuerCore
{
    public sealed class BuildAreaInfo
    {
        public string ManagerGuid { get; private set; }

        public bool RemoveOnly { get; private set; }

        public bool BuildMenuDisabled { get; private set; }

        public bool NotFullyExamined { get; private set; }

        public float NetPower { get; private set; }

        internal BuildAreaInfo(string managerGuid, bool removeOnly, bool buildMenuDisabled, bool notFullyExamined, float netPower)
        {
            ManagerGuid = managerGuid;
            RemoveOnly = removeOnly;
            BuildMenuDisabled = buildMenuDisabled;
            NotFullyExamined = notFullyExamined;
            NetPower = netPower;
        }
    }

    public static partial class Building
    {
        internal const string AreasOwner = Housing.FurnitureOwner + ".areas";

        public static event Action<BuildAreaInfo> AreaEntered;

        public static event Action<BuildAreaInfo> AreaExited;

        public static BuildAreaInfo CurrentArea
        {
            get
            {
                try
                {
                    FurnitureManager manager = BuildingSystem.instance == null ? null : BuildingSystem.activeManager;
                    return manager == null ? null : Snapshot(manager);
                }
                catch (Exception e)
                {
                    ApiLog.Once(Housing.Owner, "CurrentArea", e);
                    return null;
                }
            }
        }

        internal static BuildAreaInfo Snapshot(FurnitureManager manager)
        {
            return new BuildAreaInfo(manager.GUID, manager.areaType == BuildingArea.AreaType.RemoveOnly, manager.BuildMenuDisabled, manager.notFullyExamined, manager.NetPower);
        }

        internal static void RaiseAreaEntered(BuildAreaInfo info)
        {
            Events.Raise(AreaEntered, "Building.AreaEntered", h => h(info));
        }

        internal static void RaiseAreaExited(BuildAreaInfo info)
        {
            Events.Raise(AreaExited, "Building.AreaExited", h => h(info));
        }
    }
}
