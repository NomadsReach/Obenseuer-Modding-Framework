using System;
using System.Reflection;

namespace ObenseuerCore
{
    internal static class PlayerMovementPatches
    {
        internal static bool Install()
        {
            try
            {
                return CorePatcher.PatchCore(typeof(OpenSewerCharacterController), "Awake", Type.EmptyTypes, null, CorePatcher.Handler(typeof(PlayerMovementPatches), "AwakePostfix"), null, true, PlayerMovement.Owner);
            }
            catch (Exception e)
            {
                CoreLog.For(PlayerMovement.Owner).Error("controller hook install failed: " + e.ToString());
                return false;
            }
        }

        private static void AwakePostfix(OpenSewerCharacterController __instance)
        {
            PlayerMovement.ApplyTo(__instance);
        }
    }
}
