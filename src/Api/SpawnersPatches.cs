using System;

namespace ObenseuerCore
{
    internal static class SpawnersPatches
    {
        internal static bool Install()
        {
            try
            {
                return CorePatcher.PatchCore(typeof(Spawner), "TrySpawn", new[] { typeof(bool) }, null, CorePatcher.Handler(typeof(SpawnersPatches), "TrySpawnPostfix"), null, true, Spawners.Owner);
            }
            catch (Exception e)
            {
                CoreLog.For(Spawners.Owner).Error("install failed: " + e.ToString());
                return false;
            }
        }

        private static void TrySpawnPostfix(Spawner __instance, bool __result)
        {
            if (!__result) return;
            try
            {
                Spawners.RaiseSpawned(__instance);
            }
            catch (Exception e)
            {
                ApiLog.Once(Spawners.Owner, "Spawners.Spawned", e);
            }
        }
    }
}
