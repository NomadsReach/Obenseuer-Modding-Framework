using System;
using BepInEx.Configuration;
using HarmonyLib;

namespace ObenseuerCore
{
    internal static class TweaksRenovations
    {
        private const string Section = "Tweaks.Renovations";
        private const string CostOwner = Tweaks.Owner + ".renovations.cost";
        private const string MoneyOwner = Tweaks.Owner + ".renovations.moneyonly";
        private const string BuildOwner = Tweaks.Owner + ".renovations.build";
        private static float costMultiplier = Tweaks.Default;
        private static bool costPatched;
        private static bool moneyOnly;
        private static bool moneyPatched;
        private static float buildTimeMultiplier = Tweaks.Default;
        private static bool instantBuild;
        private static bool buildPatched;
        private static bool renovationScope;

        internal static bool MoneyOnly
        {
            get { return moneyOnly; }
        }

        internal static void Init(ConfigFile config)
        {
            Tweaks.Multiplier(config, Section, "CostMultiplier", "Renovation cost multiplier (OC first payment and RM final payment). 1 = game default, range 0.01 to 100. Applies immediately.", SetCost);
            Tweaks.Flag(config, Section, "MoneyOnly", "true: renovations need only money, not the required items. Applies immediately.", SetMoneyOnly);
            Tweaks.Multiplier(config, Section, "BuildTimeMultiplier", "Renovation build time multiplier, 2 = twice as long. 1 = game default, range 0.01 to 100. Applies to orders placed from now on and to the displayed total; orders already placed keep their saved progress.", SetBuildTime);
            Tweaks.Flag(config, Section, "InstantBuild", "true: renovations placed from now on start with zero build time. Applies immediately; orders already placed keep their saved progress.", SetInstantBuild);
        }

        private static void SetCost(float value)
        {
            costMultiplier = value;
            if (costPatched || costMultiplier == Tweaks.Default) return;
            costPatched = true;
            bool ok = Tweaks.PatchAs(CostOwner, true, typeof(TenementUpgrade), "GetPriceStartOC", Type.EmptyTypes, typeof(TweaksRenovations), null, "CostPostfix", null);
            ok &= Tweaks.PatchAs(CostOwner, true, typeof(TenementUpgrade), "GetPriceFinalRM", Type.EmptyTypes, typeof(TweaksRenovations), null, "CostPostfix", null);
            Tweaks.Log(Section + ".CostMultiplier", ok);
        }

        private static void SetMoneyOnly(bool value)
        {
            moneyOnly = value;
            if (moneyPatched || !moneyOnly) return;
            moneyPatched = true;
            bool ok = Tweaks.PatchAs(MoneyOwner, true, typeof(TenementController), "CheckForItem", new[] { typeof(int), typeof(float), typeof(TenementContractor) }, typeof(TweaksRenovations), "CheckForItemPrefix", null, null, Priority.First);
            ok &= Tweaks.PatchAs(MoneyOwner, true, typeof(TenementController), "RemoveRequiredItems", new[] { typeof(int), typeof(int) }, typeof(TweaksRenovations), "RemoveRequiredItemsPrefix", null, null, Priority.First);
            ok &= Tweaks.PatchAs(MoneyOwner, true, typeof(TenementUpgrade), "CanUpgrade", new[] { typeof(TenementApartment), typeof(bool), typeof(TenementContractor) }, typeof(TweaksRenovations), "ScopePrefix", null, "ScopeFinalizer", Priority.First);
            ok &= Tweaks.PatchAs(MoneyOwner, true, typeof(UpgradeUI), "UpdateInfo", new[] { typeof(TenementApartment), typeof(TenementUpgrade), typeof(bool), typeof(string), typeof(bool), typeof(UpgradeButtonUI), typeof(bool) }, typeof(TweaksRenovations), "ScopePrefix", null, "ScopeFinalizer", Priority.First);
            Tweaks.Log(Section + ".MoneyOnly", ok);
        }

        private static void SetBuildTime(float value)
        {
            buildTimeMultiplier = value;
            PatchBuild("BuildTimeMultiplier");
        }

        private static void SetInstantBuild(bool value)
        {
            instantBuild = value;
            PatchBuild("InstantBuild");
        }

        private static void PatchBuild(string key)
        {
            if (buildPatched || (buildTimeMultiplier == Tweaks.Default && !instantBuild)) return;
            buildPatched = true;
            bool ok = Tweaks.PatchAs(BuildOwner, true, typeof(TenementUpgradeProgress), "AddUpgrade", new[] { typeof(TenementUpgrade), typeof(float), typeof(TenementContractor) }, typeof(TweaksRenovations), "AddUpgradePrefix", null, null);
            ok &= Tweaks.PatchAs(BuildOwner, true, typeof(TenementUpgrade), "GetRequiredTimeHours", Type.EmptyTypes, typeof(TweaksRenovations), null, "RequiredTimeHoursPostfix", null);
            Tweaks.Log(Section + "." + key, ok);
        }

        private static void CostPostfix(ref int __result)
        {
            if (costMultiplier == Tweaks.Default) return;
            __result = (int)Math.Round(__result * costMultiplier);
        }

        private static bool CheckForItemPrefix(ref bool __result)
        {
            if (!moneyOnly || !renovationScope) return true;
            __result = true;
            return false;
        }

        private static bool RemoveRequiredItemsPrefix()
        {
            return !moneyOnly;
        }

        private static void ScopePrefix(out bool __state)
        {
            __state = moneyOnly && !renovationScope;
            if (__state) renovationScope = true;
        }

        private static void ScopeFinalizer(bool __state)
        {
            if (__state) renovationScope = false;
        }

        private static void AddUpgradePrefix(ref float timeHoursLeft)
        {
            if (!instantBuild && buildTimeMultiplier == Tweaks.Default) return;
            timeHoursLeft = instantBuild ? 0f : timeHoursLeft * buildTimeMultiplier;
        }

        private static void RequiredTimeHoursPostfix(ref int __result)
        {
            if (buildTimeMultiplier == Tweaks.Default) return;
            __result = (int)Math.Ceiling(__result * buildTimeMultiplier);
        }
    }
}
