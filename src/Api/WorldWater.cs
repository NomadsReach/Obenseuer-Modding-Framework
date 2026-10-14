using System;
using System.Collections.Generic;
using UnityEngine;

namespace ObenseuerCore
{
    public sealed class WaterVolumeInfo
    {
        public string Id { get; private set; }

        public float Density { get; private set; }

        public float Drag { get; private set; }

        public float DrownDamage { get; private set; }

        public int SwimHygiene { get; private set; }

        internal WaterVolumeInfo(string id, float density, float drag, float drownDamage, int swimHygiene)
        {
            Id = id;
            Density = density;
            Drag = drag;
            DrownDamage = drownDamage;
            SwimHygiene = swimHygiene;
        }
    }

    public static class WorldWater
    {
        internal const string Owner = "obenseuer.moddingframework.worldwater";
        internal const string EventsOwner = Owner + ".events";

        public const float MaxBreathTicks = WaterController.maxBreathHoldingTime;

        public static bool IsAvailable
        {
            get
            {
                return Source() != null;
            }
        }

        public static float Amount
        {
            get
            {
                LiquidStorage source = Source();
                return source == null ? 0f : source.LiquidAmount;
            }
        }

        public static bool IsInfinite
        {
            get
            {
                LiquidStorage source = Source();
                return source != null && source.HasInfiniteOutputCapasity();
            }
        }

        public static bool IsPlayerSwimming
        {
            get
            {
                return WaterController.instance != null && WaterController.instance.PlayerIsSwimming;
            }
        }

        public static bool IsPlayerUnderwater
        {
            get
            {
                return UnderwaterEffect.instance != null && UnderwaterEffect.instance.playerIsUnderWater;
            }
        }

        public static float BreathTicks
        {
            get
            {
                return WaterController.instance == null ? 0f : WaterController.instance.timeInWater;
            }
        }

        public static event Action PlayerSubmerged;

        public static event Action PlayerSurfaced;

        public static List<WaterVolumeInfo> Volumes()
        {
            List<WaterVolumeInfo> list = new List<WaterVolumeInfo>();
            try
            {
                foreach (Water water in SceneIds.Active<Water>())
                {
                    list.Add(new WaterVolumeInfo(SceneIds.Of(water), water.density, water.drag, water.drowndamage, water.setPlayerHygiene));
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "WorldWater.Volumes", e);
            }
            return list;
        }

        public static bool SetVolume(string id, float density, float drag, float drownDamage, int swimHygiene)
        {
            if (SaveGate.Busy || !Valid(density) || !Valid(drag) || !Valid(drownDamage) || swimHygiene < -1) return false;
            try
            {
                Water water = SceneIds.Find<Water>(id);
                if (water == null) return false;
                water.density = density;
                water.drag = drag;
                water.drowndamage = drownDamage;
                water.setPlayerHygiene = swimHygiene;
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "WorldWater.SetVolume", e);
                return false;
            }
        }

        public static bool ResetBreath()
        {
            if (SaveGate.Busy) return false;
            try
            {
                WaterController controller = WaterController.instance;
                if (controller == null) return false;
                controller.timeInWater = 0f;
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "WorldWater.ResetBreath", e);
                return false;
            }
        }

        internal static void Init()
        {
            if (WorldWaterPatches.Install()) return;
            CorePatcher.DisableOwner(EventsOwner);
            CoreLog.For(EventsOwner).Warn("water hooks incomplete, PlayerSubmerged and PlayerSurfaced are disabled");
        }

        internal static void RaisePlayerSubmerged()
        {
            Events.Raise(PlayerSubmerged, "WorldWater.PlayerSubmerged", h => h());
        }

        internal static void RaisePlayerSurfaced()
        {
            Events.Raise(PlayerSurfaced, "WorldWater.PlayerSurfaced", h => h());
        }

        internal static bool HasSubmergedHandlers
        {
            get
            {
                return PlayerSubmerged != null;
            }
        }

        internal static bool HasSurfacedHandlers
        {
            get
            {
                return PlayerSurfaced != null;
            }
        }

        private static bool Valid(float value)
        {
            return value >= 0f && value <= float.MaxValue;
        }

        private static LiquidStorage Source()
        {
            return WaterSourceController.instance == null ? null : WaterSourceController.instance.globalStorage;
        }
    }
}
