using System;
using BepInEx.Configuration;

namespace ObenseuerCore
{
    internal static class TweaksDishwasher
    {
        private const string Section = "Tweaks.DishwasherSpeed";
        private static float speed = Tweaks.Default;
        private static bool patched;

        internal static void Init(ConfigFile config)
        {
            Tweaks.Multiplier(config, Section, "DishwasherSpeed", "Dishwasher speed multiplier, 2 = twice as fast. Stacks with CraftingSpeed. 1 = game default, range 0.01 to 100. Applies immediately to the next recipe the dishwasher generates.", Set);
        }

        private static void Set(float value)
        {
            speed = value;
            if (patched || speed == Tweaks.Default) return;
            patched = true;
            Tweaks.Log(Section, Tweaks.Patch(typeof(Dishwasher), "GenerateRecipe", new[] { typeof(Item), typeof(object[]), typeof(int), typeof(int) }, typeof(TweaksDishwasher), null, "GenerateRecipePostfix", null));
        }

        private static void GenerateRecipePostfix(Recipe __result)
        {
            if (__result == null || speed == Tweaks.Default || __result.ProcessTime <= 0) return;
            __result.ProcessTime = Math.Max(1, (int)(__result.ProcessTime / speed));
        }
    }
}
