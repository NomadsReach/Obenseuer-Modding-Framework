using System;
using System.Collections.Generic;
using System.Reflection;

namespace ObenseuerCore
{
    internal static class RegistryPatches
    {
        internal static bool InstallFailed;
        internal static bool Installed;

        internal static void Init()
        {
            bool ok = Install();
            InstallFailed = !ok;
            Installed = ok;
            if (!ok) CoreLog.For(RegistryIds.Owner).Warn("item and recipe patches not installed, Define returns -1");
            InstallPlaceholders();
            try
            {
                SaveData.Register<RegistryManifest>(RegistryIds.Owner, RegistryManifestStore.Build, RegistryManifestStore.Warn);
            }
            catch (Exception e)
            {
                CoreLog.For(RegistryIds.Owner).Error("save manifest not registered: " + e.ToString());
            }
        }

        private static bool Install()
        {
            try
            {
                bool ok = Hook(typeof(ItemDatabase), "DeSerialize", new[] { typeof(Type), typeof(string) }, "DeSerializePostfix");
                ok &= Hook(typeof(ItemDatabase), "Awake", Type.EmptyTypes, "ItemsAssignedPostfix");
                ok &= Hook(typeof(ItemDatabase), "OnEnable", Type.EmptyTypes, "ItemsAssignedPostfix");
                ok &= Hook(typeof(ItemDatabase), "Reload", Type.EmptyTypes, "ItemsAssignedPostfix");
                ok &= Hook(typeof(RecipeDatabase), "LoadDatabase", Type.EmptyTypes, "RecipesLoadedPostfix");
                return ok;
            }
            catch (Exception e)
            {
                CorePatcher.DisableOwner(RegistryIds.Owner);
                CoreLog.For(RegistryIds.Owner).Error("registry patches failed: " + e.ToString());
                return false;
            }
        }

        private static void InstallPlaceholders()
        {
            bool ok = PlaceholderHook(typeof(ItemDatabase), "FetchItemByID", new[] { typeof(int) }, "FetchItemByIdPostfix", true);
            ok &= PlaceholderHook(typeof(ItemReference), ".ctor", new[] { typeof(Item) }, "ItemReferencePostfix", true);
            ok &= PlaceholderHook(typeof(ItemReference), ".ctor", new[] { typeof(int) }, "ItemReferencePostfix", true);
            PlaceholderHook(typeof(Trade), "CheckIfAllowed", new[] { typeof(ItemData) }, "CheckIfAllowedPostfix", false);
            CorePatcher.PatchCore(typeof(ItemOperations), "SetCollectibleItemValues", new[] { typeof(ItemStack), typeof(UnityEngine.GameObject), typeof(bool) }, CorePatcher.Handler(typeof(RegistryPatches), "DropPrefix"), null, null, false, RegistryIds.PlaceholderOwner);
            if (!ok) CoreLog.For(RegistryIds.Owner).Warn("missing item placeholders not installed: an unknown item id shows as coin again");
        }

        private static bool PlaceholderHook(Type type, string name, Type[] args, string postfix, bool core)
        {
            return CorePatcher.PatchCore(type, name, args, null, CorePatcher.Handler(typeof(RegistryPatches), postfix), null, core, RegistryIds.PlaceholderOwner);
        }

        private static bool Hook(Type type, string name, Type[] args, string postfix)
        {
            return CorePatcher.PatchCore(type, name, args, null, CorePatcher.Handler(typeof(RegistryPatches), postfix), null, true, RegistryIds.Owner);
        }

        private static void FetchItemByIdPostfix(int id, ref Item __result)
        {
            try
            {
                if (id < RegistryIds.FirstId) return;
                if (__result != null)
                {
                    RegistryPlaceholders.Forget(id);
                    return;
                }
                __result = RegistryPlaceholders.For(id);
            }
            catch (Exception e)
            {
                ApiLog.Once(RegistryIds.Owner, "item placeholder", e);
            }
        }

        private static void ItemReferencePostfix(ItemReference __instance)
        {
            try
            {
                if (RegistryPlaceholders.IsPlaceholder(__instance.ID)) __instance.name = "";
            }
            catch (Exception e)
            {
                ApiLog.Once(RegistryIds.Owner, "item placeholder reference", e);
            }
        }

        private static void DropPrefix(ItemStack item, UnityEngine.GameObject gameObject)
        {
            try
            {
                if (item == null || gameObject == null || !RegistryPlaceholders.IsMissing(item.itemId)) return;
                CollectibleItem collectible = gameObject.GetComponent<CollectibleItem>();
                if (collectible != null) collectible.Item = new ItemReference(item.itemId);
                else ApiLog.Once(RegistryIds.Owner, "item placeholder drop without CollectibleItem", "the dropped placeholder's prefab has no CollectibleItem, so its item id is not set on the drop");
            }
            catch (Exception e)
            {
                ApiLog.Once(RegistryIds.Owner, "item placeholder drop", e);
            }
        }

        private static void CheckIfAllowedPostfix(ItemData itemData, ref bool __result)
        {
            try
            {
                if (__result && itemData != null && itemData.item != null && RegistryPlaceholders.IsPlaceholder(itemData.item.ID)) __result = false;
            }
            catch (Exception e)
            {
                ApiLog.Once(RegistryIds.Owner, "item placeholder sale", e);
            }
        }

        private static void DeSerializePostfix(Type type, object __result)
        {
            if (type != typeof(List<Item>)) return;
            try
            {
                ItemRegistry.Apply(__result as List<Item>);
            }
            catch (Exception e)
            {
                CoreLog.For(RegistryIds.Owner).Error("item registry failed on load: " + e.ToString());
            }
        }

        private static void ItemsAssignedPostfix()
        {
            try
            {
                ItemRegistry.OnAssigned();
            }
            catch (Exception e)
            {
                CoreLog.For(RegistryIds.Owner).Error("item registry failed after load: " + e.ToString());
            }
        }

        private static void RecipesLoadedPostfix()
        {
            try
            {
                RecipeRegistry.OnDatabaseLoaded();
            }
            catch (Exception e)
            {
                CoreLog.For(RegistryIds.Owner).Error("recipe registry failed after load: " + e.ToString());
            }
        }
    }
}
