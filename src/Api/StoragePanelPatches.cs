using System;

namespace ObenseuerCore
{
    internal static class StoragePanelPatches
    {
        internal static bool Install()
        {
            try
            {
                return CorePatcher.PatchCore(typeof(StoragePanelUI), "UpdateStoragePanelUI", new[] { typeof(Storage) }, null, CorePatcher.Handler(typeof(StoragePanelPatches), "UpdatePostfix"), null, true, StoragePanel.Owner);
            }
            catch (Exception e)
            {
                ApiLog.Once(StoragePanel.Owner, "install", e);
                return false;
            }
        }

        private static void UpdatePostfix(StoragePanelUI __instance, Storage storage)
        {
            try
            {
                StoragePanel.Shown(__instance, storage);
            }
            catch (Exception e)
            {
                ApiLog.Once(StoragePanel.Owner, "UpdateStoragePanelUI postfix", e);
            }
        }
    }
}
