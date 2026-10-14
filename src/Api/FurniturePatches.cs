using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace ObenseuerCore
{
    internal static class FurniturePatches
    {
        internal static bool Installed;
        private static readonly FieldInfo canSellItemsField = AccessTools.Field(typeof(FurnitureShop), "canSellItems");

        internal static void Init()
        {
            bool ok = Install();
            Installed = ok;
            if (ok)
            {
                FurniturePools.Install();
                FurnitureLoot.Install();
                FurniturePlacement.Install();
                FurnitureConsole.Install();
            }
            if (!ok) CoreLog.For(FurnitureRegistry.Owner).Warn("furniture patches not installed, Define returns null");
            try
            {
                SaveData.Register<FurnitureManifest>(FurnitureRegistry.Owner, FurnitureManifestStore.Build, FurnitureManifestStore.Load);
            }
            catch (Exception e)
            {
                CoreLog.For(FurnitureRegistry.Owner).Error("save manifest not registered: " + e.ToString());
            }
            Events.HourPassed += OnHourPassed;
            SaveController.LoadingDone += FurnitureBuild.ForgetFailures;
            Events.SceneLoaded += scene => FurnitureBuild.ForgetFailures();
        }

        private static bool Install()
        {
            try
            {
                bool ok = Hook(typeof(SavableScriptableObject), "LoadFromPath", Type.EmptyTypes, "LoadFromPathPrefix", null);
                ok &= Hook(typeof(FurnitureShop), "UpdateShopItems", Type.EmptyTypes, null, "UpdateShopItemsPostfix");
                ok &= Hook(typeof(FurnitureShop), "OnLoadingGame", Type.EmptyTypes, null, "OnLoadingGamePostfix");
                ok &= Hook(typeof(FurnitureShop), "CanSell", new[] { typeof(BuildingSystem.FurnitureInfo) }, null, "CanSellPostfix");
                ok &= Hook(typeof(BuildingSystem), "AddFurniture", new[] { typeof(Furniture), typeof(GameObject), typeof(GameObject).MakeByRefType(), typeof(int), typeof(BuildingSystem.FurnitureInfo.Meta) }, "AddFurniturePrefix", null);
                ok &= Hook(typeof(Furniture), "CreateTaskItem", new[] { typeof(Furniture), typeof(BuildingSystem.FurnitureInfo.Meta), typeof(TaskItem) }, null, "CreateTaskItemPostfix");
                ok &= Hook(typeof(TaskItem), "Compare", new[] { typeof(TaskItem) }, "ComparePrefix", null);
                ok &= Hook(typeof(SaveController), "SaveGame", new[] { typeof(string), typeof(string), typeof(string) }, "SaveGamePrefix", null, "SaveGameFinalizer");
                return ok;
            }
            catch (Exception e)
            {
                CorePatcher.DisableOwner(FurnitureRegistry.Owner);
                CoreLog.For(FurnitureRegistry.Owner).Error("furniture patches failed: " + e.ToString());
                return false;
            }
        }

        private static bool Hook(Type type, string name, Type[] args, string prefix, string postfix, string finalizer = null)
        {
            return CorePatcher.PatchCore(type, name, args, CorePatcher.Handler(typeof(FurniturePatches), prefix), CorePatcher.Handler(typeof(FurniturePatches), postfix), CorePatcher.Handler(typeof(FurniturePatches), finalizer), true, FurnitureRegistry.Owner);
        }

        private static bool LoadFromPathPrefix(SavableScriptableObject __instance, ref ScriptableObject __result)
        {
            TaskItemBlueprint blueprint = __instance as TaskItemBlueprint;
            if (blueprint != null) return BlueprintPrefix(blueprint, ref __result);
            Furniture saved = __instance as Furniture;
            if (saved == null || !FurnitureRegistry.IsModId(saved.id)) return true;
            try
            {
                Furniture resolved = FurnitureBuild.Resolve(saved);
                if (resolved == null) return true;
                __result = resolved;
                return false;
            }
            catch (Exception e)
            {
                ApiLog.Once(FurnitureRegistry.Owner, "LoadFromPath prefix", e);
                return true;
            }
        }

        private static bool BlueprintPrefix(TaskItemBlueprint saved, ref ScriptableObject __result)
        {
            if (!FurnitureRegistry.IsModId(saved.id)) return true;
            try
            {
                __result = FurnitureBlueprints.Resolve(saved);
                return false;
            }
            catch (Exception e)
            {
                ApiLog.Once(FurnitureRegistry.Owner, "LoadFromPath blueprint prefix", e);
                return true;
            }
        }

        private static bool ComparePrefix(TaskItem __instance, TaskItem taskItem, ref bool __result)
        {
            if (__instance == null || taskItem == null) return true;
            try
            {
                if (!FurnitureIds.IsModId(__instance.id) && !FurnitureIds.IsModId(taskItem.id)) return true;
                if (__instance.id == taskItem.id) return true;
                __result = false;
                return false;
            }
            catch (Exception e)
            {
                ApiLog.Once(FurnitureRegistry.Owner, "TaskItem Compare prefix", e);
                return true;
            }
        }

        private static void SaveGamePrefix(out object __state)
        {
            List<StrippedBlueprint> stripped = new List<StrippedBlueprint>();
            __state = stripped;
            try
            {
                FurnitureBlueprints.Strip(stripped);
            }
            catch (Exception e)
            {
                ApiLog.Once(FurnitureRegistry.Owner, "SaveGame prefix", e);
            }
        }

        private static void SaveGameFinalizer(object __state)
        {
            try
            {
                FurnitureBlueprints.Restore(__state as List<StrippedBlueprint>);
            }
            catch (Exception e)
            {
                ApiLog.Once(FurnitureRegistry.Owner, "SaveGame finalizer", e);
            }
        }

        private static void AddFurniturePrefix(ref Furniture furniture)
        {
            if (furniture == null || !FurnitureRegistry.IsModId(furniture.id)) return;
            try
            {
                Furniture resolved = FurnitureBuild.Resolve(furniture);
                if (resolved != null) furniture = resolved;
            }
            catch (Exception e)
            {
                ApiLog.Once(FurnitureRegistry.Owner, "AddFurniture prefix", e);
            }
        }

        private static void CreateTaskItemPostfix(Furniture furniture, TaskItem __result)
        {
            if (furniture == null || __result == null || !FurnitureRegistry.IsModId(furniture.id)) return;
            try
            {
                __result.id = furniture.id;
            }
            catch (Exception e)
            {
                ApiLog.Once(FurnitureRegistry.Owner, "CreateTaskItem postfix", e);
            }
        }

        private static void UpdateShopItemsPostfix(FurnitureShop __instance)
        {
            try
            {
                FurnitureShops.Restocked(__instance);
            }
            catch (Exception e)
            {
                ApiLog.Once(FurnitureRegistry.Owner, "UpdateShopItems postfix", e);
            }
        }

        private static void OnLoadingGamePostfix(FurnitureShop __instance, bool __result)
        {
            if (!__result) return;
            try
            {
                FurnitureShops.Request(__instance);
            }
            catch (Exception e)
            {
                ApiLog.Once(FurnitureRegistry.Owner, "OnLoadingGame postfix", e);
            }
        }

        private static void CanSellPostfix(FurnitureShop __instance, BuildingSystem.FurnitureInfo furnitureInfo, ref bool __result)
        {
            if (__result || furnitureInfo == null || furnitureInfo.furniture == null || !FurnitureRegistry.IsModId(furnitureInfo.furniture.id)) return;
            try
            {
                __result = (bool)canSellItemsField.GetValue(__instance) && FurnitureShops.Sells(__instance, furnitureInfo.furniture.id);
            }
            catch (Exception e)
            {
                ApiLog.Once(FurnitureRegistry.Owner, "CanSell postfix", e);
            }
        }

        private static void OnHourPassed(int hour)
        {
            FurnitureShops.Flush();
        }
    }
}
