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

        private static bool Hook(Type type, string name, Type[] args, string postfix)
        {
            return CorePatcher.PatchCore(type, name, args, null, CorePatcher.Handler(typeof(RegistryPatches), postfix), null, true, RegistryIds.Owner);
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
