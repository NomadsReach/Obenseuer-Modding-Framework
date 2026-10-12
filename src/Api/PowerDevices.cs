using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace ObenseuerCore
{
    public enum PowerDeviceKind
    {
        Source,
        Consumer
    }

    public sealed class PowerDeviceInfo
    {
        public string Id { get; private set; }

        public PowerDeviceKind Kind { get; private set; }

        public bool IsOn { get; private set; }

        public bool IsPowered { get; private set; }

        public float NetPower { get; private set; }

        public float FuelLevel { get; private set; }

        public float MaxFuel { get; private set; }

        public List<int> FuelItemIds { get; private set; }

        internal PowerDeviceInfo(string id, PowerDeviceKind kind, bool isOn, bool isPowered, float netPower, float fuelLevel, float maxFuel, List<int> fuelItemIds)
        {
            Id = id;
            Kind = kind;
            IsOn = isOn;
            IsPowered = isPowered;
            NetPower = netPower;
            FuelLevel = fuelLevel;
            MaxFuel = maxFuel;
            FuelItemIds = fuelItemIds;
        }
    }

    public sealed class PoweredChangedArgs
    {
        public string Id { get; private set; }

        public bool IsPowered { get; private set; }

        internal PoweredChangedArgs(string id, bool isPowered)
        {
            Id = id;
            IsPowered = isPowered;
        }
    }

    public static class PowerDevices
    {
        internal const string Owner = "obenseuer.moddingframework.powerdevices";
        private static bool installed;
        private static readonly FieldInfo batteryDrain = AccessTools.Field(typeof(BatteryPowerSource), "drainEnabled");
        private static readonly FieldInfo batteryPowering = AccessTools.Field(typeof(BatteryPowerSource), "powering");

        public static event Action<PoweredChangedArgs> PoweredChanged;

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        public static List<PowerDeviceInfo> All()
        {
            List<PowerDeviceInfo> list = new List<PowerDeviceInfo>();
            try
            {
                foreach (Component component in Components())
                {
                    list.Add(ToInfo(component));
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "PowerDevices.All", e);
            }
            return list;
        }

        public static PowerDeviceInfo Get(string id)
        {
            try
            {
                Component component = Find(id);
                return component == null ? null : ToInfo(component);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "PowerDevices.Get", e);
                return null;
            }
        }

        public static bool SetOn(string id, bool on)
        {
            try
            {
                Component component = Find(id);
                if (SaveGate.Busy) return false;
                FurnitureStats stats = component as FurnitureStats;
                if (stats != null)
                {
                    if (on)
                    {
                        stats.turnON();
                    }
                    else
                    {
                        stats.turnOFF();
                    }
                    return stats.isOn == on;
                }
                CraftingBase crafting = component as CraftingBase;
                if (crafting != null)
                {
                    if (crafting.AlwaysOn) return false;
                    crafting.Toggle(on);
                    return crafting.On == on;
                }
                BatteryPowerSource battery = component as BatteryPowerSource;
                if (battery == null) return false;
                battery.ToggleDrain(on);
                return Flag(batteryDrain, battery) == on;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "PowerDevices.SetOn", e);
                return false;
            }
        }

        internal static bool HasHandlers
        {
            get
            {
                return PoweredChanged != null;
            }
        }

        internal static void Init()
        {
            installed = PowerDevicesPatches.Install();
            if (installed) return;
            CorePatcher.DisableOwner(Owner);
            CoreLog.For(Owner).Warn("power device hooks incomplete, PoweredChanged is disabled, the reads and SetOn still work");
        }

        internal static void RaisePoweredChanged(FurnitureStats stats)
        {
            PoweredChangedArgs args = new PoweredChangedArgs(SceneIds.Of(stats), stats.isActive);
            Events.Raise(PoweredChanged, "PowerDevices.PoweredChanged", h => h(args));
        }

        private static PowerDeviceInfo ToInfo(Component component)
        {
            FurnitureStats stats = component as FurnitureStats;
            if (stats != null)
            {
                PowerDeviceKind kind = stats.netPowerOn > 0f ? PowerDeviceKind.Source : PowerDeviceKind.Consumer;
                return new PowerDeviceInfo(SceneIds.Of(stats), kind, stats.isOn, stats.isActive, stats.netPowerOn, 0f, 0f, new List<int>());
            }
            CraftingBase crafting = component as CraftingBase;
            if (crafting != null) return CraftingInfo(crafting);
            BatteryPowerSource battery = (BatteryPowerSource)component;
            return new PowerDeviceInfo(SceneIds.Of(battery), PowerDeviceKind.Source, Flag(batteryDrain, battery), Flag(batteryPowering, battery), 0f, 0f, 0f, new List<int>());
        }

        private static PowerDeviceInfo CraftingInfo(CraftingBase crafting)
        {
            PowerSource power = crafting._Power;
            Fuel fuel = power.RequiresFuel ? power._Fuel : null;
            PowerDeviceKind kind = crafting is Generator ? PowerDeviceKind.Source : PowerDeviceKind.Consumer;
            float fuelLevel = fuel == null ? 0f : fuel.BurnTimeRemaining;
            float maxFuel = fuel == null ? 0f : fuel.MaxBurnTime;
            return new PowerDeviceInfo(SceneIds.Of(crafting), kind, crafting.On, crafting.PoweredAndActive, 0f, fuelLevel, maxFuel, FuelItems(fuel));
        }

        private static List<int> FuelItems(Fuel fuel)
        {
            List<int> ids = new List<int>();
            if (fuel == null || fuel.FuelCategories == null) return ids;
            foreach (string category in fuel.FuelCategories)
            {
                if (string.IsNullOrEmpty(category)) continue;
                foreach (ItemDef item in Items.InCategory(category))
                {
                    if (!ids.Contains(item.Id)) ids.Add(item.Id);
                }
            }
            return ids;
        }

        private static bool Flag(FieldInfo field, object target)
        {
            return field != null && (bool)field.GetValue(target);
        }

        private static Component Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (Component component in Components())
            {
                if (SceneIds.Of(component) == id) return component;
            }
            return null;
        }

        private static List<Component> Components()
        {
            List<Component> list = new List<Component>();
            foreach (FurnitureStats stats in UnityEngine.Object.FindObjectsByType<FurnitureStats>(FindObjectsSortMode.None))
            {
                list.Add(stats);
            }
            foreach (CraftingBase crafting in UnityEngine.Object.FindObjectsByType<CraftingBase>(FindObjectsSortMode.None))
            {
                if (crafting._Power != null) list.Add(crafting);
            }
            foreach (BatteryPowerSource battery in UnityEngine.Object.FindObjectsByType<BatteryPowerSource>(FindObjectsSortMode.None))
            {
                list.Add(battery);
            }
            return list;
        }
    }
}
