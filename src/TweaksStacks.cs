using System;
using BepInEx.Configuration;

namespace ObenseuerCore
{
    internal static class TweaksStacks
    {
        private const string Section = "Tweaks.Stacks";
        private static float multiplier = Tweaks.Default;
        private static bool patched;

        internal static void Init(ConfigFile config)
        {
            Tweaks.Multiplier(config, Section, "StackMultiplier", "Multiplier for the stack size of items that stack (above 1). 1 = game default, range 0.01 to 100. Applies immediately. Lowering it leaves stacks already over the new limit over it. Liquid capacity per slot scales too.", Set);
        }

        private static void Set(float value)
        {
            multiplier = value;
            if (patched || multiplier == Tweaks.Default) return;
            patched = true;
            Tweaks.Log(Section, Tweaks.Patch(typeof(Item), "get_Stackable", Type.EmptyTypes, typeof(TweaksStacks), null, "StackablePostfix", null));
        }

        private static void StackablePostfix(ref int __result)
        {
            if (multiplier == Tweaks.Default || __result <= 1) return;
            __result = (int)Math.Min(int.MaxValue, Math.Max(1d, Math.Round(__result * (double)multiplier)));
        }
    }
}
