using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using OS.Items;
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

        public float StoredCharge { get; private set; }

        public float MaxCharge { get; private set; }

        public List<int> FuelItemIds { get; private set; }

        public bool IsBurning { get; private set; }

        public float BurnTimeFactor { get; private set; }

        internal PowerDeviceInfo(string id, PowerDeviceKind kind, bool isOn, bool isPowered, float netPower, float fuelLevel, float maxFuel, float storedCharge, float maxCharge, List<int> fuelItemIds, bool isBurning, float burnTimeFactor)
        {
            Id = id;
            Kind = kind;
            IsOn = isOn;
            IsPowered = isPowered;
            NetPower = netPower;
            FuelLevel = fuelLevel;
            MaxFuel = maxFuel;
            StoredCharge = storedCharge;
            MaxCharge = maxCharge;
            FuelItemIds = fuelItemIds;
            IsBurning = isBurning;
            BurnTimeFactor = burnTimeFactor;
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

    public sealed class FuelRanOutArgs
    {
        public string Id { get; private set; }

        internal FuelRanOutArgs(string id)
        {
            Id = id;
        }
    }

    public static class PowerDevices
    {
        internal const string Owner = "obenseuer.moddingframework.powerdevices";
        internal const string FuelOwner = Owner + ".fuel";
        internal const string BatteryOwner = Owner + ".battery";
        private static bool installed;
        private static readonly FieldInfo batteryDrain = AccessTools.Field(typeof(BatteryPowerSource), "drainEnabled");
        private static readonly FieldInfo batteryPowering = AccessTools.Field(typeof(BatteryPowerSource), "powering");

        public static event Action<PoweredChangedArgs> PoweredChanged;

        public static event Action<FuelRanOutArgs> FuelRanOut;

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

        internal static bool HasFuelHandlers
        {
            get
            {
                return FuelRanOut != null;
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

        internal static void RaisePoweredChanged(BatteryPowerSource battery, bool powered)
        {
            PoweredChangedArgs args = new PoweredChangedArgs(SceneIds.Of(battery), powered);
            Events.Raise(PoweredChanged, "PowerDevices.PoweredChanged", h => h(args));
        }

        internal static void RaiseFuelRanOut(string id)
        {
            FuelRanOutArgs args = new FuelRanOutArgs(id);
            Events.Raise(FuelRanOut, "PowerDevices.FuelRanOut", h => h(args));
        }

        internal static bool BatteryPowering(BatteryPowerSource battery)
        {
            return Flag(batteryPowering, battery);
        }

        private static PowerDeviceInfo ToInfo(Component component)
        {
            FurnitureStats stats = component as FurnitureStats;
            if (stats != null)
            {
                PowerDeviceKind kind = stats.netPowerOn > 0f ? PowerDeviceKind.Source : PowerDeviceKind.Consumer;
                return new PowerDeviceInfo(SceneIds.Of(stats), kind, stats.isOn, stats.isActive, stats.netPowerOn, 0f, 0f, 0f, 0f, new List<int>(), false, 0f);
            }
            CraftingBase crafting = component as CraftingBase;
            if (crafting != null) return CraftingInfo(crafting);
            return BatteryInfo((BatteryPowerSource)component);
        }

        private static PowerDeviceInfo BatteryInfo(BatteryPowerSource battery)
        {
            float stored;
            float max;
            ChargeOf(battery, out stored, out max);
            float secondsPerUnit = battery.fuelDrainPerHour > 0f ? 3600f / battery.fuelDrainPerHour : 0f;
            return new PowerDeviceInfo(SceneIds.Of(battery), PowerDeviceKind.Source, Flag(batteryDrain, battery), Flag(batteryPowering, battery), 0f, stored * secondsPerUnit, max * secondsPerUnit, stored, max, new List<int>(), false, 0f);
        }

        private static void ChargeOf(BatteryPowerSource battery, out float stored, out float max)
        {
            stored = 0f;
            max = 0f;
            Storage storage = battery.GetComponent<Storage>();
            if (storage == null || storage.storageSlots == null) return;
            foreach (global::ItemStack stack in storage.storageSlots)
            {
                if (stack == null) continue;
                if (battery.fuelType.HasFlag(BatteryPowerSource.FuelType.ItemAmmoData))
                {
                    ItemAmmodata ammo = stack.GetMetaOfType<ItemAmmodata>();
                    if (ammo != null)
                    {
                        stored += ammo.ammo;
                        max += ammo.maxAmmo;
                    }
                }
                if (battery.fuelType.HasFlag(BatteryPowerSource.FuelType.ItemLiquidData))
                {
                    ItemLiquidData liquid = stack.GetMetaOfType<ItemLiquidData>();
                    if (liquid != null)
                    {
                        stored += liquid.GetLiquidAmount(stack.itemAmount);
                        max += liquid.GetLiquidMaxAmount(stack.itemAmount);
                    }
                }
            }
        }

        private static PowerDeviceInfo CraftingInfo(CraftingBase crafting)
        {
            PowerSource power = crafting._Power;
            Fuel fuel = power.RequiresFuel ? power._Fuel : null;
            PowerDeviceKind kind = crafting is Generator ? PowerDeviceKind.Source : PowerDeviceKind.Consumer;
            float fuelLevel = fuel == null ? 0f : fuel.BurnTimeRemaining;
            float maxFuel = fuel == null ? 0f : fuel.MaxBurnTime;
            return new PowerDeviceInfo(SceneIds.Of(crafting), kind, crafting.On, crafting.PoweredAndActive, 0f, fuelLevel, maxFuel, 0f, 0f, FuelItems(fuel), fuel != null && fuel.active, fuel == null ? 0f : fuel.BurnTimeFactor);
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
            foreach (FurnitureStats stats in SceneIds.Active<FurnitureStats>())
            {
                list.Add(stats);
            }
            foreach (CraftingBase crafting in SceneIds.Active<CraftingBase>())
            {
                if (crafting._Power != null) list.Add(crafting);
            }
            foreach (BatteryPowerSource battery in SceneIds.Active<BatteryPowerSource>())
            {
                list.Add(battery);
            }
            return list;
        }
    }
}
