using System;
using System.Reflection;
using HarmonyLib;
using OS.Items;

namespace ObenseuerCore
{
    internal static class SurvivalPatches
    {
        private static bool dispatched;
        private static bool inBreakdown;
        private static readonly MethodInfo getOrCreateItemData = AccessTools.Method(typeof(CollectibleItem), "GetOrCreateItemData", Type.EmptyTypes);
        private static readonly MethodInfo canDrinkLiquid = AccessTools.Method(typeof(CollectibleItem), "CanDrinkLiquid", new[] { typeof(ItemLiquidData).MakeByRefType() });

        private const string ConsumablesOwner = Survival.Owner + ".consumables";
        private const string NeedsOwner = Survival.Owner + ".needs";
        private const string DeathOwner = Survival.Owner + ".death";
        private const string DamageOwner = Survival.Owner + ".damage";
        private const string BreakdownOwner = Survival.Owner + ".breakdown";
        private const string DepressionOwner = Survival.Owner + ".depression";

        internal static SurvivalGroup Install()
        {
            try
            {
                bool consumables = Hook(ConsumablesOwner, typeof(ItemConsumable), "Invoke", new[] { typeof(ItemData), typeof(bool) }, "InvokePrefix", "InvokePostfix");
                consumables &= Hook(ConsumablesOwner, typeof(ItemInfo), "UseItem", new[] { typeof(int), typeof(bool) }, "UseItemPrefix", null, "DispatchFinalizer");
                consumables &= Hook(ConsumablesOwner, typeof(CollectibleItem), "InvokeActions", Type.EmptyTypes, "InvokeActionsPrefix", null, "DispatchFinalizer");
                bool needs = Hook(NeedsOwner, typeof(PlayerStats), "DeltaSeconds", new[] { typeof(int) }, null, "DeltaSecondsPostfix");
                needs &= Hook(NeedsOwner, typeof(PlayerStatsHungerAndThirst), "GetHungerRate", Type.EmptyTypes, null, "HungerRatePostfix");
                needs &= Hook(NeedsOwner, typeof(PlayerStatsHungerAndThirst), "GetThirstRate", Type.EmptyTypes, null, "ThirstRatePostfix");
                needs &= Hook(NeedsOwner, typeof(PlayerStatsHygiene), "GetHygieneRate", Type.EmptyTypes, null, "HygieneRatePostfix");
                needs &= Hook(NeedsOwner, typeof(PlayerStatsAddictions), "GetAlcoholNeedBaserate", Type.EmptyTypes, null, "AlcoholNeedRatePostfix");
                needs &= Hook(NeedsOwner, typeof(PlayerStatsAddictions), "GetMushroomNeedBaserate", Type.EmptyTypes, null, "MushroomNeedRatePostfix");
                needs &= Hook(NeedsOwner, typeof(PlayerStatsAddictions), "GetSmokingNeedBaserate", Type.EmptyTypes, null, "SmokingNeedRatePostfix");
                needs &= Hook(NeedsOwner, typeof(PlayerStatsAddictions), "GetGamblingNeedBaserate", Type.EmptyTypes, null, "GamblingNeedRatePostfix");
                bool death = Hook(DeathOwner, typeof(Death), "PlayerDeath", new[] { typeof(DamageTypeNames) }, "DeathPrefix", "DeathPostfix");
                death &= Hook(DeathOwner, typeof(WaitingController), "PassOut", new[] { typeof(bool), typeof(WaitingController.Type) }, "PassOutPrefix", "PassOutPostfix");
                bool damage = Hook(DamageOwner, typeof(PlayerStatsHealth), "Damage", new[] { typeof(float), typeof(DamageTypeNames), typeof(bool) }, "DamagePrefix", "DamagePostfix");
                bool breakdown = Hook(BreakdownOwner, typeof(PlayerStatsMentalHealth), "UpdateStats", new[] { typeof(int) }, "BreakdownEnter", null, "BreakdownExit");
                bool depression = Hook(DepressionOwner, typeof(Depression), "Suicide", Type.EmptyTypes, "SuicidePrefix", null);
                depression &= Hook(DepressionOwner, typeof(Depression), "HurtYourself", new[] { typeof(DamageTypeNames) }, "HurtPrefix", null);
                depression &= Hook(DepressionOwner, typeof(Depression), "UseRandomMushroomsFromInventory", Type.EmptyTypes, "MushroomsPrefix", null);
                depression &= Hook(DepressionOwner, typeof(Depression), "DrinkRandomAlcoholFromInventory", Type.EmptyTypes, "AlcoholPrefix", null);
                return (consumables ? SurvivalGroup.Consumables : SurvivalGroup.None)
                    | (needs ? SurvivalGroup.Needs : SurvivalGroup.None)
                    | (death ? SurvivalGroup.Death : SurvivalGroup.None)
                    | (damage ? SurvivalGroup.Damage : SurvivalGroup.None)
                    | (breakdown ? SurvivalGroup.Breakdown : SurvivalGroup.None)
                    | (depression ? SurvivalGroup.Depression : SurvivalGroup.None);
            }
            catch (Exception e)
            {
                Survival.Fail("install", e);
                return SurvivalGroup.None;
            }
        }

        private static bool Hook(string owner, Type type, string name, Type[] args, string prefix, string postfix)
        {
            return Hook(owner, type, name, args, prefix, postfix, null);
        }

        private static bool Hook(string owner, Type type, string name, Type[] args, string prefix, string postfix, string finalizer)
        {
            return CorePatcher.PatchCore(type, name, args, CorePatcher.Handler(typeof(SurvivalPatches), prefix), CorePatcher.Handler(typeof(SurvivalPatches), postfix), CorePatcher.Handler(typeof(SurvivalPatches), finalizer), true, owner);
        }

        private static bool InvokePrefix(ItemConsumable __instance, ItemData itemData, out bool __state)
        {
            __state = true;
            if (dispatched || !Survival.Installed(SurvivalGroup.Consumables)) return true;
            try
            {
                __state = Survival.Consumables.Dispatch(itemData, __instance);
                return __state;
            }
            catch (Exception e)
            {
                Survival.Fail("Invoke prefix", e);
                return true;
            }
        }

        private static void InvokePostfix(ItemData itemData, bool __state)
        {
            if (!__state || itemData == null || !Survival.Consumables.HasConsumedHandlers) return;
            try
            {
                Survival.Consumables.RaiseConsumed(new ConsumedArgs(itemData.item.ID));
            }
            catch (Exception e)
            {
                Survival.Fail("Invoke postfix", e);
            }
        }

        private static bool UseItemPrefix(ItemInfo __instance, int index, bool secondaryButton, out bool __state)
        {
            __state = dispatched;
            if (!Survival.Installed(SurvivalGroup.Consumables)) return true;
            try
            {
                bool buttonOff = (!__instance.itemInfoPanel.UseButtonEnabled && !secondaryButton) || (!__instance.itemInfoPanel.SecondaryUseButtonEnabled && secondaryButton);
                if (__instance.ItemData == null || buttonOff || TradePanel.instance.activeTrade != null || __instance.cooldown) return true;
                ItemConsumable consumable = __instance.childItem != null ? FirstConsumable(__instance.childItem.item.Actions) : IndexedConsumable(__instance.ItemData.item.Actions, index);
                return consumable == null || Approve(__instance.ItemData, consumable);
            }
            catch (Exception e)
            {
                Survival.Fail("UseItem prefix", e);
                return true;
            }
        }

        private static bool InvokeActionsPrefix(CollectibleItem __instance, out bool __state)
        {
            __state = dispatched;
            if (!Survival.Installed(SurvivalGroup.Consumables)) return true;
            try
            {
                if (TradePanel.instance.activeTrade != null) return true;
                ItemData itemData = (ItemData)getOrCreateItemData.Invoke(__instance, null);
                object[] drinkArgs = new object[] { null };
                bool drink = (bool)canDrinkLiquid.Invoke(__instance, drinkArgs);
                ItemLiquidData liquidData = (ItemLiquidData)drinkArgs[0];
                ItemConsumable consumable = (drink ? liquidData.liquidItem.Item.FirstAction : __instance.Item.Item.FirstAction) as ItemConsumable;
                string cantUseText;
                if (consumable == null || !consumable.CanUse(itemData, out cantUseText) || !OwnerAllows(itemData)) return true;
                return Approve(itemData, consumable);
            }
            catch (Exception e)
            {
                Survival.Fail("InvokeActions prefix", e);
                return true;
            }
        }

        private static void DispatchFinalizer(bool __state)
        {
            dispatched = __state;
        }

        private static bool Approve(ItemData itemData, ItemConsumable consumable)
        {
            dispatched = true;
            return Survival.Consumables.Dispatch(itemData, consumable);
        }

        private static ItemConsumable FirstConsumable(Item.ItemAction[] actions)
        {
            if (actions == null) return null;
            foreach (Item.ItemAction action in actions)
            {
                ItemConsumable consumable = action as ItemConsumable;
                if (consumable != null) return consumable;
            }
            return null;
        }

        private static ItemConsumable IndexedConsumable(Item.ItemAction[] actions, int index)
        {
            return actions != null && actions.Length > index ? actions[index] as ItemConsumable : null;
        }

        private static bool OwnerAllows(ItemData itemData)
        {
            if (itemData.meta == null) return true;
            foreach (object obj in itemData.meta)
            {
                if (obj.GetType() == typeof(ItemOwnerPermission)) return ((ItemOwnerPermission)obj).allowUse;
            }
            return true;
        }

        private static void DeltaSecondsPostfix()
        {
            if (!Survival.Installed(SurvivalGroup.Needs)) return;
            try
            {
                Survival.Tick();
            }
            catch (Exception e)
            {
                Survival.Fail("DeltaSeconds postfix", e);
            }
        }

        private static void DeathPrefix(Death __instance, out bool __state)
        {
            __state = __instance == null || __instance.dead || !Survival.Installed(SurvivalGroup.Death);
        }

        private static void DeathPostfix(bool __state, DamageTypeNames reason)
        {
            if (__state) return;
            try
            {
                Survival.RaisePlayerDied(reason);
            }
            catch (Exception e)
            {
                Survival.Fail("PlayerDeath postfix", e);
            }
        }

        private static void PassOutPrefix(WaitingController __instance, out WaitingController.Type __state)
        {
            __state = __instance == null ? WaitingController.Type.None : __instance.waitingType;
        }

        private static void PassOutPostfix(WaitingController __instance, WaitingController.Type __state)
        {
            if (__instance == null || __state == WaitingController.Type.Passout || __instance.waitingType != WaitingController.Type.Passout || !Survival.Installed(SurvivalGroup.Death)) return;
            try
            {
                Survival.RaisePassedOut();
            }
            catch (Exception e)
            {
                Survival.Fail("PassOut postfix", e);
            }
        }

        private static void DamagePrefix(PlayerStatsHealth __instance, ref float damage, DamageTypeNames reason, bool silent, out float __state)
        {
            __state = __instance == null ? 0f : __instance.currentStatus;
            if (!Survival.Installed(SurvivalGroup.Damage)) return;
            try
            {
                damage = Survival.DispatchDamage(damage, reason, silent);
            }
            catch (Exception e)
            {
                Survival.Fail("Damage intercept", e);
            }
        }

        private static void DamagePostfix(PlayerStatsHealth __instance, float __state, DamageTypeNames reason)
        {
            if (__instance == null || !Survival.Installed(SurvivalGroup.Damage)) return;
            try
            {
                float lost = __state - __instance.currentStatus;
                if (lost > 0f) Survival.RaiseDamaged(lost, reason);
            }
            catch (Exception e)
            {
                Survival.Fail("Damage postfix", e);
            }
        }

        private static void HungerRatePostfix(ref float __result)
        {
            ApplyDecay(Stat.Hunger, ref __result);
        }

        private static void ThirstRatePostfix(ref float __result)
        {
            ApplyDecay(Stat.Thirst, ref __result);
        }

        private static void HygieneRatePostfix(ref float __result)
        {
            ApplyDecay(Stat.Hygiene, ref __result);
        }

        private static void AlcoholNeedRatePostfix(ref float __result)
        {
            ApplyDecay(Stat.AlcoholNeed, ref __result);
        }

        private static void MushroomNeedRatePostfix(ref float __result)
        {
            ApplyDecay(Stat.MushroomNeed, ref __result);
        }

        private static void SmokingNeedRatePostfix(ref float __result)
        {
            ApplyDecay(Stat.SmokingNeed, ref __result);
        }

        private static void GamblingNeedRatePostfix(ref float __result)
        {
            ApplyDecay(Stat.GamblingNeed, ref __result);
        }

        private static void BreakdownEnter()
        {
            if (!Survival.Installed(SurvivalGroup.Breakdown)) return;
            inBreakdown = true;
        }

        private static void BreakdownExit()
        {
            if (!Survival.Installed(SurvivalGroup.Breakdown)) return;
            inBreakdown = false;
        }

        private static bool SuicidePrefix()
        {
            return BreakdownAllowed(BreakdownKind.Suicide);
        }

        private static bool HurtPrefix()
        {
            return BreakdownAllowed(BreakdownKind.SelfHarm);
        }

        private static bool MushroomsPrefix(ref bool __result)
        {
            if (BreakdownAllowed(BreakdownKind.Mushrooms)) return true;
            __result = false;
            return false;
        }

        private static bool AlcoholPrefix(ref bool __result)
        {
            if (BreakdownAllowed(BreakdownKind.Alcohol)) return true;
            __result = false;
            return false;
        }

        private static bool BreakdownAllowed(BreakdownKind kind)
        {
            if (!inBreakdown || !Survival.Installed(SurvivalGroup.Depression)) return true;
            try
            {
                return Survival.RaiseBreakdown(kind);
            }
            catch (Exception e)
            {
                Survival.Fail("Breakdown " + kind, e);
                return true;
            }
        }

        private static void ApplyDecay(Stat stat, ref float rate)
        {
            if (!Survival.Installed(SurvivalGroup.Needs)) return;
            try
            {
                rate = Survival.Decay.Apply(stat, rate);
            }
            catch (Exception e)
            {
                Survival.Fail("decay " + stat, e);
            }
        }
    }
}
