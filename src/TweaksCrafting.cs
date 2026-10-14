using System;
using BepInEx.Configuration;

namespace ObenseuerCore
{
    internal static class TweaksCrafting
    {
        private const string SectionSpeed = "Tweaks.CraftingSpeed";
        private const string SectionSkill = "Tweaks.NoMinimumSkill";
        private const string CraftingOwner = Tweaks.Owner + ".crafting";
        private static float speed = Tweaks.Default;
        private static bool noMinimumSkill;
        private static bool processTimeTried;
        private static bool processTimeOk;
        private static bool speedTried;
        private static bool skillTried;

        internal static void Init(ConfigFile config)
        {
            Tweaks.Multiplier(config, SectionSpeed, "CraftingSpeed", "Crafting speed multiplier, 2 = twice as fast. Applies to manufacturing and blueprint craft times. 1 = game default, range 0.01 to 100. Applies immediately; crafts already running keep their timer.", SetSpeed);
            Tweaks.Flag(config, SectionSkill, "NoMinimumSkill", "Manufacturing recipes ignore the minimum skill gate and its craft time penalty. Skill still affects fail chance and quality. Blueprints are not covered. Applies immediately; the recipe list shows it when it next refreshes.", SetNoMinimumSkill);
        }

        private static void SetSpeed(float value)
        {
            speed = value;
            if (speedTried || speed == Tweaks.Default) return;
            speedTried = true;
            bool ok = EnsureProcessTime();
            ok &= Tweaks.PatchAs(CraftingOwner, true, typeof(BaseCrafting), "StartCrafting", new[] { typeof(TaskItemBlueprint) }, typeof(TweaksCrafting), null, "StartCraftingPostfix", null);
            Tweaks.Log(SectionSpeed, ok);
        }

        private static void SetNoMinimumSkill(bool value)
        {
            noMinimumSkill = value;
            if (skillTried || !noMinimumSkill) return;
            skillTried = true;
            bool ok = EnsureProcessTime() && Tweaks.PatchAs(CraftingOwner, true, typeof(RecipeInfoUI), "CheckSkillSoft", new[] { typeof(Recipe), typeof(float) }, typeof(TweaksCrafting), null, "CheckSkillSoftPostfix", null);
            Tweaks.Log(SectionSkill, ok);
        }

        private static bool EnsureProcessTime()
        {
            if (!processTimeTried)
            {
                processTimeTried = true;
                processTimeOk = Tweaks.PatchAs(CraftingOwner, true, typeof(Recipe), "GetProcessTime", new[] { typeof(float), typeof(float) }, typeof(TweaksCrafting), null, "ProcessTimePostfix", null);
            }
            return processTimeOk;
        }

        // Harmony binds parameters by name: keep 'multpiler', the game's spelling.
        private static void ProcessTimePostfix(Recipe __instance, float multpiler, float equipmentQuality, ref int __result)
        {
            if (noMinimumSkill) __result = (int)((float)__instance.ProcessTime * multpiler / equipmentQuality);
            if (speed != Tweaks.Default && __result > 0) __result = Math.Max(1, (int)Math.Min(__result / (double)speed, 2000000000.0));
        }

        private static void StartCraftingPostfix(BaseCrafting __instance)
        {
            if (speed == Tweaks.Default) return;
            __instance.craftingLength /= speed;
        }

        private static void CheckSkillSoftPostfix(Recipe recipe, float equipmentquality, ref bool __result)
        {
            if (noMinimumSkill && Recipe.CurrentFailChance(recipe.FailChance, recipe.RelevantSkill, equipmentquality, recipe.SkillFactor, 0) < 1f) __result = true;
        }
    }
}
