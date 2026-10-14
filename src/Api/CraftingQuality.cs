using System;
using UnityEngine.SceneManagement;

namespace ObenseuerCore
{
    public static class CraftingQuality
    {
        internal const string Owner = "obenseuer.moddingframework.craftingquality";
        private const string AllOutputs = "*";
        private const float MinQuality = 0.05f;
        private const float MaxQuality = 2f;
        private static bool installed;
        private static readonly PayoutFactors<string> factors = new PayoutFactors<string>(Owner);

        public static bool QualityFactorAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        public static bool SetQualityFactor(string modId, float factor)
        {
            if (!ValidMod(modId) || !QualityFactorAvailable) return false;
            if (!PayoutFactors<string>.Valid(factor))
            {
                ApiLog.Once(Owner, "quality factor", "quality factor must be 0 to " + PayoutFactors<string>.MaxFactor);
                return false;
            }
            factors.Set(modId, AllOutputs, factor);
            return true;
        }

        public static bool Clear(string modId)
        {
            if (!ValidMod(modId)) return false;
            return factors.Clear(modId);
        }

        internal static void Init()
        {
            installed = CraftingQualityPatches.Install();
            Events.SceneLoadedWithMode += OnSceneLoaded;
            if (installed) return;
            CorePatcher.DisableOwner(Owner);
            CoreLog.For(Owner).Warn("craft quality hooks incomplete, quality factors are disabled");
        }

        internal static float Adjust(float quality)
        {
            if (!factors.Any) return quality;
            float factor = factors.Of(AllOutputs);
            if (factor == 1f) return quality;
            return Math.Min(Math.Max(quality * factor, MinQuality), MaxQuality);
        }

        private static void OnSceneLoaded(string scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single) return;
            factors.Reset();
        }

        private static bool ValidMod(string modId)
        {
            if (ModIds.IsValid(modId)) return true;
            ApiLog.Once(Owner, "modId", "modId must be 1 to 64 characters of letters, digits, '.', '_' or '-'");
            return false;
        }
    }
}
