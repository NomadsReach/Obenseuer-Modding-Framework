using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace ObenseuerCore
{
    public sealed class BlastSiteInfo
    {
        public string Id { get; private set; }

        public bool DynamitePlaced { get; private set; }

        public bool Detonated { get; private set; }

        public int LinkedCount { get; private set; }

        public int LinkedPlaced { get; private set; }

        internal BlastSiteInfo(string id, bool dynamitePlaced, bool detonated, int linkedCount, int linkedPlaced)
        {
            Id = id;
            DynamitePlaced = dynamitePlaced;
            Detonated = detonated;
            LinkedCount = linkedCount;
            LinkedPlaced = linkedPlaced;
        }
    }

    public static class BlastSites
    {
        internal const string Owner = "obenseuer.moddingframework.blastsites";
        private static bool installed;
        private static readonly FieldInfo linkedField = AccessTools.Field(typeof(InteractableDynamiteCone), "linkedCones");

        public static event Action<string> Detonated;

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        public static List<BlastSiteInfo> All()
        {
            List<BlastSiteInfo> list = new List<BlastSiteInfo>();
            try
            {
                foreach (InteractableDynamiteCone cone in SceneIds.Active<InteractableDynamiteCone>())
                {
                    list.Add(ToInfo(cone));
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "BlastSites.All", e);
            }
            return list;
        }

        public static BlastSiteInfo Get(string id)
        {
            InteractableDynamiteCone cone = Find(id);
            if (cone == null) return null;
            try
            {
                return ToInfo(cone);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "BlastSites.Get", e);
                return null;
            }
        }

        internal static void Init()
        {
            installed = linkedField != null && BlastSitesPatches.Install();
            if (installed) return;
            CorePatcher.DisableOwner(Owner);
            CoreLog.For(Owner).Warn("blast site hooks incomplete, Detonated event is disabled");
        }

        internal static bool HasDetonatedHandlers
        {
            get
            {
                return Detonated != null;
            }
        }

        internal static void RaiseDetonated(InteractableDynamiteCone cone)
        {
            string id = SceneIds.Of(cone);
            Events.Raise(Detonated, "BlastSites.Detonated", h => h(id));
        }

        private static InteractableDynamiteCone Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            try
            {
                foreach (InteractableDynamiteCone cone in SceneIds.Active<InteractableDynamiteCone>())
                {
                    if (SceneIds.Of(cone) == id) return cone;
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "BlastSites.Find", e);
            }
            return null;
        }

        private static BlastSiteInfo ToInfo(InteractableDynamiteCone cone)
        {
            List<InteractableDynamiteCone> linked = new List<InteractableDynamiteCone>();
            List<InteractableDynamiteCone> source = linkedField.GetValue(cone) as List<InteractableDynamiteCone>;
            if (source != null) linked.AddRange(source);
            linked.RemoveAll(member => member == null);
            if (!linked.Contains(cone)) linked.Add(cone);
            int placed = 0;
            foreach (InteractableDynamiteCone member in linked)
            {
                if (member.dynamitePlaced) placed++;
            }
            return new BlastSiteInfo(SceneIds.Of(cone), cone.dynamitePlaced, cone.detonated, linked.Count, placed);
        }
    }
}
