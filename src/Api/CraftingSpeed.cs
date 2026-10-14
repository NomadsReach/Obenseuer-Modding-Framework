using System;
using UnityEngine.SceneManagement;

namespace ObenseuerCore
{
    public static class CraftingSpeed
    {
        internal const string Owner = "obenseuer.moddingframework.craftingspeed";
        public const string AllStations = "*";
        private const float MinSpeed = 0.01f;
        private static bool installed;
        private static readonly PayoutFactors<string> speeds = new PayoutFactors<string>(Owner);

        public static bool SpeedFactorsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        public static bool SetSpeedFactor(string modId, string stationKind, float speed)
        {
            if (!ValidMod(modId) || !SpeedFactorsAvailable) return false;
            if (string.IsNullOrEmpty(stationKind) || float.IsNaN(speed) || speed < MinSpeed || speed > PayoutFactors<string>.MaxFactor)
            {
                ApiLog.Once(Owner, "speed factor", "speed must be " + MinSpeed + " to " + PayoutFactors<string>.MaxFactor + " and the station kind must be set");
                return false;
            }
            speeds.Set(modId, stationKind, speed);
            return true;
        }

        public static bool Clear(string modId)
        {
            if (!ValidMod(modId)) return false;
            return speeds.Clear(modId);
        }

        internal static void Init()
        {
            installed = CraftingSpeedPatches.Install();
            Events.SceneLoadedWithMode += OnSceneLoaded;
            if (installed) return;
            CorePatcher.DisableOwner(Owner);
            CoreLog.For(Owner).Warn("craft speed hooks incomplete, speed factors are disabled");
        }

        internal static float FactorFor(CraftingBase station)
        {
            if (!speeds.Any) return 1f;
            float speed = speeds.Of(station.GetType().Name) * speeds.Of(AllStations);
            return Math.Min(Math.Max(speed, MinSpeed), PayoutFactors<string>.MaxFactor);
        }

        private static void OnSceneLoaded(string scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single) return;
            speeds.Reset();
        }

        private static bool ValidMod(string modId)
        {
            if (ModIds.IsValid(modId)) return true;
            ApiLog.Once(Owner, "modId", "modId must be 1 to 64 characters of letters, digits, '.', '_' or '-'");
            return false;
        }
    }
}
