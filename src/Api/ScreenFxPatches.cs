using System;
using UnityEngine;

namespace ObenseuerCore
{
    internal static class ScreenFxPatches
    {
        internal static bool Install()
        {
            try
            {
                bool updated = Hook("Update");
                bool set = Hook("SetFOV");
                return updated && set;
            }
            catch (Exception e)
            {
                CoreLog.For(ScreenFx.Owner).Error("fov hook install failed: " + e.ToString());
                return false;
            }
        }

        private static bool Hook(string name)
        {
            return CorePatcher.PatchCore(typeof(SetCameraFOV), name, Type.EmptyTypes, CorePatcher.Handler(typeof(ScreenFxPatches), "FovPrefix"), null, null, true, ScreenFx.Owner);
        }

        private static bool FovPrefix(SetCameraFOV __instance)
        {
            float? fov = ScreenFx.FovOverride;
            if (!fov.HasValue) return true;
            try
            {
                __instance.GetComponent<Camera>().fieldOfView = fov.Value;
                return false;
            }
            catch (Exception e)
            {
                ApiLog.Once(ScreenFx.Owner, "ScreenFx.FovOverride", e);
                return true;
            }
        }
    }
}
