using System;
using BeautifyEffect;
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

        internal static bool InstallBlur()
        {
            try
            {
                return HookBlur("EnableBlur", "BlurOn", Type.EmptyTypes)
                    && HookBlur("DisableBlur", "BlurOff", Type.EmptyTypes)
                    && HookBlur("ChangeBeautifyProfiles", "BlurProfile", new Type[] { typeof(BeautifyProfile) })
                    && HookBlur("ChangeBeautifyProfilesToDefault", "BlurProfile", Type.EmptyTypes);
            }
            catch (Exception e)
            {
                CoreLog.For(ScreenFxBlur.Owner).Error("blur hook install failed: " + e.ToString());
                return false;
            }
        }

        private static bool HookBlur(string name, string handler, Type[] args)
        {
            return CorePatcher.PatchCore(typeof(EffectsController), name, args, null, CorePatcher.Handler(typeof(ScreenFxPatches), handler), null, true, ScreenFxBlur.Owner);
        }

        private static void BlurOn()
        {
            ScreenFxBlur.GameChanged(true);
        }

        private static void BlurOff()
        {
            ScreenFxBlur.GameChanged(false);
        }

        private static void BlurProfile()
        {
            ScreenFxBlur.ProfileChanged();
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
