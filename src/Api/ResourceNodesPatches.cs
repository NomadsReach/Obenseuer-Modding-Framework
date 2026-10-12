using System;
using System.Reflection;
using UnityEngine;

namespace ObenseuerCore
{
    internal static class ResourceNodesPatches
    {
        internal static bool Install()
        {
            try
            {
                return CorePatcher.PatchCore(typeof(WorkableResourceSource), "Damage", new[] { typeof(int), typeof(Vector3), typeof(Vector3), typeof(DamageReceiver.DamageType) }, CorePatcher.Handler(typeof(ResourceNodesPatches), "DamagePrefix"), CorePatcher.Handler(typeof(ResourceNodesPatches), "DamagePostfix"), null, true, ResourceNodes.Owner);
            }
            catch (Exception e)
            {
                ApiLog.Once(ResourceNodes.Owner, "install", e);
                return false;
            }
        }

        private static void DamagePrefix(int ___currentStock, out int __state)
        {
            __state = ___currentStock;
        }

        private static void DamagePostfix(WorkableResourceSource __instance, DamageReceiver.DamageType type, int ___currentStock, int __state)
        {
            try
            {
                if (!ResourceNodes.HasHandlers || __state < 1 || !__instance.CheckDamageType(type)) return;
                string id = SceneIds.Of(__instance);
                ResourceNodes.RaiseHit(new ResourceHitArgs(id, ___currentStock, ___currentStock < __state));
                if (___currentStock < 1) ResourceNodes.RaiseDepleted(new ResourceDepletedArgs(id));
            }
            catch (Exception e)
            {
                ApiLog.Once(ResourceNodes.Owner, "Damage postfix", e);
            }
        }
    }
}
