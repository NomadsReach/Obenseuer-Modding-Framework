using System;
using System.Collections.Generic;

namespace ObenseuerCore
{
    public sealed class CraftingUpgradeInfo
    {
        public string Id { get; private set; }

        public string Name { get; private set; }

        public bool On { get; private set; }

        public List<string> Connected { get; private set; }

        internal CraftingUpgradeInfo(string id, string name, bool on, List<string> connected)
        {
            Id = id;
            Name = name;
            On = on;
            Connected = connected;
        }
    }

    public static class CraftingUpgrades
    {
        internal const string Owner = "obenseuer.moddingframework.craftingupgrades";

        public static List<CraftingUpgradeInfo> All()
        {
            List<CraftingUpgradeInfo> list = new List<CraftingUpgradeInfo>();
            try
            {
                foreach (CraftingUpgrade upgrade in UnityEngine.Object.FindObjectsByType<CraftingUpgrade>(UnityEngine.FindObjectsSortMode.None))
                {
                    list.Add(ToInfo(upgrade));
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "CraftingUpgrades.All", e);
            }
            return list;
        }

        public static CraftingUpgradeInfo Get(string id)
        {
            try
            {
                CraftingUpgrade upgrade = Find(id);
                return upgrade == null ? null : ToInfo(upgrade);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "CraftingUpgrades.Get", e);
                return null;
            }
        }

        public static bool SetOn(string id, bool on)
        {
            CraftingUpgrade upgrade = Find(id);
            if (upgrade == null || SaveGate.Busy || CraftingBase.currentCraftingBase != null) return false;
            try
            {
                upgrade.Toggle(on);
                return upgrade.Active == on;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "CraftingUpgrades.SetOn", e);
                return false;
            }
        }

        private static string IdOf(CraftingUpgrade upgrade)
        {
            return SceneIds.Of(upgrade);
        }

        private static CraftingUpgrade Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            try
            {
                foreach (CraftingUpgrade upgrade in UnityEngine.Object.FindObjectsByType<CraftingUpgrade>(UnityEngine.FindObjectsSortMode.None))
                {
                    if (IdOf(upgrade) == id) return upgrade;
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "CraftingUpgrades.Find", e);
            }
            return null;
        }

        private static CraftingUpgradeInfo ToInfo(CraftingUpgrade upgrade)
        {
            List<string> connected = new List<string>();
            foreach (CraftingBase station in upgrade.Connected)
            {
                if (station != null) connected.Add(SceneIds.Of(station));
            }
            return new CraftingUpgradeInfo(IdOf(upgrade), upgrade.name, upgrade.Active, connected);
        }
    }
}
