using System;
using HarmonyLib;
using UnityEngine;

namespace ObenseuerCore
{
    internal static class ModelPatches
    {
        internal const string Owner = "obenseuer.moddingframework.models";
        internal static bool Installed;

        internal static void Init()
        {
            Installed = Install();
        }

        private static bool Install()
        {
            return CorePatcher.PatchCore(typeof(Item.ItemAppearance), "Loadprefab", new[] { typeof(Item) }, null, CorePatcher.Handler(typeof(ModelPatches), "LoadprefabPostfix"), null, true, Owner, Priority.Last)
                && Hook(typeof(DestructibleList), "SpawnItem", new[] { typeof(GameObject), typeof(string) }, "SpawnItemPostfix")
                && Hook(typeof(CollectibleItem), "Start", Type.EmptyTypes, "StartPostfix");
        }

        private static bool Hook(Type type, string name, Type[] args, string postfix)
        {
            return CorePatcher.PatchCore(type, name, args, null, CorePatcher.Handler(typeof(ModelPatches), postfix), null, true, Owner);
        }

        private static void LoadprefabPostfix(Item item, Item.ItemAppearance __instance)
        {
            try
            {
                if (item == null) return;
                WorldModel world = ModelRegistry.WorldFor(item.ID);
                GameObject clone = world == null ? null : ModelBuild.CloneFor(world, item);
                if (clone == null) return;
                __instance.Prefab = clone;
                __instance.PrefabMany = clone;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Loadprefab postfix", e);
            }
        }

        private static void SpawnItemPostfix(GameObject GO)
        {
            try
            {
                if (GO == null) return;
                InstantiatedPrefabRef reference = GO.GetComponent<InstantiatedPrefabRef>();
                CollectibleItem collectible = GO.GetComponent<CollectibleItem>();
                if (reference == null || reference.data == null || collectible == null || collectible.Item == null) return;
                WorldModel world = GO.GetComponent<ObxModelMark>() == null ? null : ModelRegistry.WorldFor(collectible.Item.ID);
                if (world == null || world.TemplatePath == null) FloorItems.FixSavedPath(reference.data, collectible);
                else reference.data.PrefabPath = world.TemplatePath;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "SpawnItem postfix", e);
            }
        }

        private static void StartPostfix(CollectibleItem __instance)
        {
            try
            {
                if (__instance == null || __instance.Item == null) return;
                WorldModel world = ModelRegistry.WorldFor(__instance.Item.ID);
                if (world == null || __instance.GetComponent<ObxModelMark>() != null) return;
                ModelBuild.SwapInstance(__instance, world);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Start postfix", e);
            }
        }
    }
}
