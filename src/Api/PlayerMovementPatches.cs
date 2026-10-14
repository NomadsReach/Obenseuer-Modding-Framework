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

        internal static bool InstallLook()
        {
            try
            {
                bool disable = CorePatcher.PatchCore(typeof(ThirdPersonCameraController), "DisableRotation", Type.EmptyTypes, null, CorePatcher.Handler(typeof(PlayerMovementPatches), "DisableRotationPostfix"), null, false, PlayerMovement.Owner);
                bool enable = CorePatcher.PatchCore(typeof(ThirdPersonCameraController), "EnableRotation", Type.EmptyTypes, null, CorePatcher.Handler(typeof(PlayerMovementPatches), "EnableRotationPostfix"), null, false, PlayerMovement.Owner);
                return disable && enable;
            }
            catch (Exception e)
            {
                CoreLog.For(PlayerMovement.Owner).Error("look hook install failed: " + e.ToString());
                return false;
            }
        }

        private static void AwakePostfix(OpenSewerCharacterController __instance)
        {
            PlayerMovement.ApplyTo(__instance);
        }

        private static void DisableRotationPostfix()
        {
            PlayerMovement.GameRotation(true);
        }

        private static void EnableRotationPostfix()
        {
            PlayerMovement.GameRotation(false);
        }
    }
}
