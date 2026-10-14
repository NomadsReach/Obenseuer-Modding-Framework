using System;
using System.Reflection;
using OS.Items;
using UnityEngine;

namespace ObenseuerCore
{
    internal static class PowerDevicesPatches
    {
        private static int restoring;

        internal static bool Install()
        {
            bool furniture = InstallFurniture();
            if (furniture)
            {
                InstallFuel();
                InstallBattery();
            }
            return furniture;
        }

        private static bool InstallFurniture()
        {
            try
            {
                MethodInfo quietPrefix = CorePatcher.Handler(typeof(PowerDevicesPatches), "QuietPrefix");
                MethodInfo quietFinalizer = CorePatcher.Handler(typeof(PowerDevicesPatches), "QuietFinalizer");
                MethodInfo transitionPrefix = CorePatcher.Handler(typeof(PowerDevicesPatches), "TransitionPrefix");
                MethodInfo transitionPostfix = CorePatcher.Handler(typeof(PowerDevicesPatches), "TransitionPostfix");
                return CorePatcher.PatchCore(typeof(FurnitureStats), "Awake", Type.EmptyTypes, quietPrefix, null, quietFinalizer, true, PowerDevices.Owner)
                    && CorePatcher.PatchCore(typeof(FurnitureStats), "OutputON", Type.EmptyTypes, transitionPrefix, transitionPostfix, null, true, PowerDevices.Owner)
                    && CorePatcher.PatchCore(typeof(FurnitureStats), "OutputOFF", Type.EmptyTypes, transitionPrefix, transitionPostfix, null, true, PowerDevices.Owner);
            }
            catch (Exception e)
            {
                CoreLog.For(PowerDevices.Owner).Error("power device hook install failed: " + e.ToString());
                return false;
            }
        }

        private static void InstallFuel()
        {
            try
            {
                CorePatcher.PatchCore(typeof(Fuel), "OnFuelConsumeStopped", Type.EmptyTypes, CorePatcher.Handler(typeof(PowerDevicesPatches), "FuelStopPrefix"), CorePatcher.Handler(typeof(PowerDevicesPatches), "FuelStopPostfix"), null, true, PowerDevices.FuelOwner);
            }
            catch (Exception e)
            {
                CorePatcher.DisableOwner(PowerDevices.FuelOwner);
                CoreLog.For(PowerDevices.FuelOwner).Error("fuel hook install failed: " + e.ToString());
            }
        }

        private static void InstallBattery()
        {
            try
            {
                MethodInfo batteryPrefix = CorePatcher.Handler(typeof(PowerDevicesPatches), "BatteryPrefix");
                MethodInfo batteryPostfix = CorePatcher.Handler(typeof(PowerDevicesPatches), "BatteryPostfix");
                CorePatcher.PatchCore(typeof(BatteryPowerSource), "PowerON", Type.EmptyTypes, batteryPrefix, batteryPostfix, null, true, PowerDevices.BatteryOwner);
                CorePatcher.PatchCore(typeof(BatteryPowerSource), "PowerOFF", Type.EmptyTypes, batteryPrefix, batteryPostfix, null, true, PowerDevices.BatteryOwner);
            }
            catch (Exception e)
            {
                CorePatcher.DisableOwner(PowerDevices.BatteryOwner);
                CoreLog.For(PowerDevices.BatteryOwner).Error("battery hook install failed: " + e.ToString());
            }
        }

        private static void QuietPrefix(out bool __state)
        {
            restoring++;
            __state = true;
        }

        private static void QuietFinalizer(bool __state)
        {
            if (__state) restoring--;
        }

        private static void TransitionPrefix(FurnitureStats __instance, out bool __state)
        {
            __state = __instance.isActive;
        }

        private static void TransitionPostfix(FurnitureStats __instance, bool __state)
        {
            if (__state == __instance.isActive || restoring > 0 || SaveGate.Loading || !PowerDevices.HasHandlers) return;
            try
            {
                PowerDevices.RaisePoweredChanged(__instance);
            }
            catch (Exception e)
            {
                ApiLog.Once(PowerDevices.Owner, "PowerDevices.PoweredChanged", e);
            }
        }

        private static void FuelStopPrefix(Fuel __instance, out bool __state)
        {
            __state = false;
            try
            {
                __state = __instance.fuelCounterOn && __instance.BurnTimeRemaining <= 0;
            }
            catch (Exception e)
            {
                ApiLog.Once(PowerDevices.FuelOwner, "PowerDevices.FuelRanOut", e);
            }
        }

        private static void FuelStopPostfix(Fuel __instance, bool __state)
        {
            if (!__state || SaveGate.Busy || !PowerDevices.HasFuelHandlers) return;
            try
            {
                GameObject parent = __instance.Parent;
                if (parent == null) return;
                CraftingBase crafting = parent.GetComponent<CraftingBase>();
                if (crafting == null) return;
                PowerDevices.RaiseFuelRanOut(SceneIds.Of(crafting));
            }
            catch (Exception e)
            {
                ApiLog.Once(PowerDevices.FuelOwner, "PowerDevices.FuelRanOut", e);
            }
        }

        private static void BatteryPrefix(BatteryPowerSource __instance, out bool __state)
        {
            __state = PowerDevices.BatteryPowering(__instance);
        }

        private static void BatteryPostfix(BatteryPowerSource __instance, bool __state)
        {
            if (SaveGate.Loading || !PowerDevices.HasHandlers) return;
            try
            {
                bool powering = PowerDevices.BatteryPowering(__instance);
                if (powering == __state) return;
                PowerDevices.RaisePoweredChanged(__instance, powering);
            }
            catch (Exception e)
            {
                ApiLog.Once(PowerDevices.Owner, "PowerDevices.PoweredChanged", e);
            }
        }
    }
}
