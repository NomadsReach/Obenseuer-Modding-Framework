using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace ObenseuerCore
{
    public sealed class DishwasherInfo
    {
        public string Id { get; private set; }

        public Vector3 Position { get; private set; }

        public bool Busy { get; private set; }

        internal DishwasherInfo(string id, Vector3 position, bool busy)
        {
            Id = id;
            Position = position;
            Busy = busy;
        }
    }

    public static class Dishwashers
    {
        internal const string Owner = "obenseuer.moddingframework.dishwashers";
        private static bool installed;
        private static FieldInfo craftsInProgresses;

        public static event Action<DishwasherInfo> Finished;

        internal static bool HasHandlers
        {
            get
            {
                return Finished != null;
            }
        }

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        public static List<DishwasherInfo> All()
        {
            List<DishwasherInfo> list = new List<DishwasherInfo>();
            try
            {
                foreach (Dishwasher dishwasher in UnityEngine.Object.FindObjectsByType<Dishwasher>(FindObjectsSortMode.None))
                {
                    if (dishwasher != null && !string.IsNullOrEmpty(dishwasher.GUID)) list.Add(ToInfo(dishwasher));
                }
                return list;
            }
            catch (Exception e)
            {
                ApiLog.Once(Dishwashers.Owner, "All", e);
                return new List<DishwasherInfo>();
            }
        }

        public static DishwasherInfo Get(string id)
        {
            try
            {
                Dishwasher dishwasher = Find(id);
                return dishwasher == null ? null : ToInfo(dishwasher);
            }
            catch (Exception e)
            {
                ApiLog.Once(Dishwashers.Owner, "Get", e);
                return null;
            }
        }

        public static List<ItemStackInfo> Contents(string id)
        {
            try
            {
                Dishwasher dishwasher = Find(id);
                if (dishwasher == null || dishwasher.InputOutputStorage == null) return new List<ItemStackInfo>();
                return Containers.GetItems(new ContainerInfo(dishwasher.InputOutputStorage));
            }
            catch (Exception e)
            {
                ApiLog.Once(Dishwashers.Owner, "Contents", e);
                return new List<ItemStackInfo>();
            }
        }

        public static bool Ready(string id)
        {
            try
            {
                Dishwasher dishwasher = Find(id);
                return dishwasher != null && dishwasher.PoweredAndActive && !dishwasher.CraftingIProgress();
            }
            catch (Exception e)
            {
                ApiLog.Once(Dishwashers.Owner, "Ready", e);
                return false;
            }
        }

        public static float Progress(string id)
        {
            try
            {
                Dishwasher dishwasher = Find(id);
                if (dishwasher == null || craftsInProgresses == null) return 0f;
                List<CraftInProgress> running = (List<CraftInProgress>)craftsInProgresses.GetValue(dishwasher);
                if (running == null || running.Count == 0 || running[0] == null || running[0].timer == null || running[0].timer.timeInSeconds <= 0) return 0f;
                return Mathf.Clamp01(1f - (float)running[0].timer.remainingInSeconds / running[0].timer.timeInSeconds);
            }
            catch (Exception e)
            {
                ApiLog.Once(Dishwashers.Owner, "Progress", e);
                return 0f;
            }
        }

        internal static void Init()
        {
            installed = DishwashersPatches.Install() && LoadCrafts();
            if (installed) return;
            CorePatcher.DisableOwner(Owner);
            CoreLog.For(Owner).Warn("dishwasher hooks incomplete, dishwasher events are disabled");
        }

        internal static DishwasherInfo ToInfo(Dishwasher dishwasher)
        {
            return new DishwasherInfo(dishwasher.GUID, dishwasher.transform.position, dishwasher.CraftingIProgress());
        }

        internal static void RaiseFinished(DishwasherInfo info)
        {
            Events.Raise(Finished, "Dishwashers.Finished", h => h(info));
        }

        private static bool LoadCrafts()
        {
            try
            {
                craftsInProgresses = AccessTools.Field(typeof(manufacturing), "craftsInProgresses");
                return craftsInProgresses != null;
            }
            catch (Exception e)
            {
                ApiLog.Once(Dishwashers.Owner, "FieldRefAccess", e);
                return false;
            }
        }

        private static Dishwasher Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (Dishwasher dishwasher in UnityEngine.Object.FindObjectsByType<Dishwasher>(FindObjectsSortMode.None))
            {
                if (dishwasher != null && dishwasher.GUID == id) return dishwasher;
            }
            return null;
        }
    }
}
