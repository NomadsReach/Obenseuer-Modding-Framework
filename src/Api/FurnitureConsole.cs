using System;
using System.Collections.Generic;

namespace ObenseuerCore
{
    internal static class FurnitureConsole
    {
        internal static bool Installed;

        internal static void Install()
        {
            bool ok;
            try
            {
                ok = Hook("GetFurniture", new[] { typeof(string) });
                ok &= Hook("AddAllFurniture", new[] { typeof(int) });
                ok &= Hook("FindFurniture", new[] { typeof(string) });
            }
            catch (Exception e)
            {
                ApiLog.Once(FurnitureRegistry.Owner, "console install", e);
                ok = false;
            }
            Installed = ok;
            if (!ok) CoreLog.For(FurnitureRegistry.Owner).Warn("console not installed, mod furniture is not in the console list");
        }

        private static bool Hook(string name, Type[] args)
        {
            return CorePatcher.PatchCore(typeof(DeveloperConsoleFurniture), name, args, CorePatcher.Handler(typeof(FurnitureConsole), "ListPrefix"), null, null, false, FurnitureRegistry.Owner);
        }

        private static void ListPrefix(DeveloperConsoleFurniture __instance)
        {
            if (!Installed || __instance == null) return;
            try
            {
                Append(__instance.furnitures);
            }
            catch (Exception e)
            {
                ApiLog.Once(FurnitureRegistry.Owner, "console list", e);
            }
        }

        internal static void Append(List<Furniture> list)
        {
            if (list == null || !FurnitureManifestStore.Ready) return;
            foreach (FurnitureEntry entry in FurnitureRegistry.All())
            {
                Furniture furniture = FurnitureBuild.Registered(entry);
                if (furniture != null && !list.Contains(furniture)) list.Add(furniture);
            }
        }
    }
}
