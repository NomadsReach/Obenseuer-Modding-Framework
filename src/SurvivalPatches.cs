using System;
using System.Reflection;
using HarmonyLib;
using OS.Items;

namespace ObenseuerCore
{
    internal static class SurvivalPatches
    {
        private static bool dispatched;
        private static readonly MethodInfo getOrCreateItemData = AccessTools.Method(typeof(CollectibleItem), "GetOrCreateItemData", Type.EmptyTypes);
        private static readonly MethodInfo canDrinkLiquid = AccessTools.Method(typeof(CollectibleItem), "CanDrinkLiquid", new[] { typeof(ItemLiquidData).MakeByRefType() });

        internal static bool Install()
        {
            try
            {
                bool ok = true;
                ok &= Hook(typeof(ItemConsumable), "Invoke", new[] { typeof(ItemData), typeof(bool) }, "InvokePrefix", "InvokePostfix");
                ok &= Hook(typeof(ItemInfo), "UseItem", new[] { typeof(int), typeof(bool) }, "UseItemPrefix", null, "DispatchFinalizer");
                ok &= Hook(typeof(CollectibleItem), "InvokeActions", Type.EmptyTypes, "InvokeActionsPrefix", null, "DispatchFinalizer");
                ok &= Hook(typeof(PlayerStats), "DeltaSeconds", new[] { typeof(int) }, null, "DeltaSecondsPostfix");
                ok &= Hook(typeof(Death), "PlayerDeath", new[] { typeof(DamageTypeNames) }, "DeathPrefix", "DeathPostfix");
                ok &= Hook(typeof(WaitingController), "PassOut", new[] { typeof(bool), typeof(WaitingController.Type) }, "PassOutPrefix", "PassOutPostfix");
                ok &= Hook(typeof(PlayerStatsHealth), "Damage", new[] { typeof(float), typeof(DamageTypeNames), typeof(bool) }, "DamagePrefix", "DamagePostfix");
                ok &= Hook(typeof(PlayerStatsHungerAndThirst), "GetHungerRate", Type.EmptyTypes, null, "HungerRatePostfix");
                ok &= Hook(typeof(PlayerStatsHungerAndThirst), "GetThirstRate", Type.EmptyTypes, null, "ThirstRatePostfix");
                ok &= Hook(typeof(PlayerStatsHygiene), "GetHygieneRate", Type.EmptyTypes, null, "HygieneRatePostfix");
                ok &= Hook(typeof(PlayerStatsAddictions), "GetAlcoholNeedBaserate", Type.EmptyTypes, null, "AlcoholNeedRatePostfix");
                ok &= Hook(typeof(PlayerStatsAddictions), "GetMushroomNeedBaserate", Type.EmptyTypes, null, "MushroomNeedRatePostfix");
                ok &= Hook(typeof(PlayerStatsAddictions), "GetSmokingNeedBaserate", Type.EmptyTypes, null, "SmokingNeedRatePostfix");
                ok &= Hook(typeof(PlayerStatsAddictions), "GetGamblingNeedBaserate", Type.EmptyTypes, null, "GamblingNeedRatePostfix");
                return ok;
            }
            catch (Exception e)
            {
                Survival.Fail("install", e);
                return false;
            }
        }

        private static bool Hook(Type type, string name, Type[] args, string prefix, string postfix)
        {
            return Hook(type, name, args, prefix, postfix, null);
        }

        private static bool Hook(Type type, string name, Type[] args, string prefix, string postfix, string finalizer)
        {
            return CorePatcher.PatchCore(type, name, args, CorePatcher.Handler(typeof(SurvivalPatches), prefix), CorePatcher.Handler(typeof(SurvivalPatches), postfix), CorePatcher.Handler(typeof(SurvivalPatches), finalizer), true, Survival.Owner);
        }

        private static bool InvokePrefix(ItemConsumable __instance, ItemData itemData, out bool __state)
        {
            __state = true;
            if (dispatched) return true;
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
            __state = __instance == null || __instance.dead;
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
            if (__instance == null || __state == WaitingController.Type.Passout || __instance.waitingType != WaitingController.Type.Passout) return;
            try
            {
                Survival.RaisePassedOut();
            }
            catch (Exception e)
            {
                Survival.Fail("PassOut postfix", e);
            }
        }

        private static void DamagePrefix(PlayerStatsHealth __instance, out float __state)
        {
            __state = __instance == null ? 0f : __instance.currentStatus;
        }

        private static void DamagePostfix(PlayerStatsHealth __instance, float __state, DamageTypeNames reason)
        {
            if (__instance == null) return;
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

        private static void ApplyDecay(Stat stat, ref float rate)
        {
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
