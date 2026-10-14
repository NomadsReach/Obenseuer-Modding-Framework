using System;
using UnityEngine;

namespace ObenseuerCore
{
    internal static class LandmarksPatches
    {
        internal static bool Install()
        {
            try
            {
                bool ok = CorePatcher.PatchCore(typeof(MapController), "AddLandMark", new[] { typeof(Landmark), typeof(Vector3), typeof(bool) }, null, CorePatcher.Handler(typeof(LandmarksPatches), "AddPostfix"), null, true, Landmarks.Owner);
                ok &= CorePatcher.PatchCore(typeof(SaveController), "SaveGame", new[] { typeof(string), typeof(string), typeof(string) }, CorePatcher.Handler(typeof(LandmarksPatches), "SavePrefix"), null, CorePatcher.Handler(typeof(LandmarksPatches), "SaveFinalizer"), true, Landmarks.Owner);
                ok &= CorePatcher.PatchCore(typeof(MapController), "OnLoadingGame", Type.EmptyTypes, null, CorePatcher.Handler(typeof(LandmarksPatches), "LoadPostfix"), null, true, Landmarks.Owner);
                return ok;
            }
            catch (Exception e)
            {
                CoreLog.For(Landmarks.Owner).Error("install failed: " + e.ToString());
                return false;
            }
        }

        private static void AddPostfix(Landmark landmark, bool __result)
        {
            if (!__result || Landmarks.Restoring) return;
            try
            {
                Landmarks.RaiseAdded(landmark);
            }
            catch (Exception e)
            {
                ApiLog.Once(Landmarks.Owner, "Landmarks.Added", e);
            }
        }

        private static void SavePrefix(out object __state)
        {
            __state = null;
            try
            {
                __state = Landmarks.Detach();
            }
            catch (Exception e)
            {
                ApiLog.Once(Landmarks.Owner, "SaveController.SaveGame prefix", e);
            }
        }

        private static void SaveFinalizer(object __state)
        {
            try
            {
                Landmarks.Reattach(__state as System.Collections.Generic.List<DetachedLandmark>);
            }
            catch (Exception e)
            {
                ApiLog.Once(Landmarks.Owner, "SaveController.SaveGame finalizer", e);
            }
        }

        private static void LoadPostfix(MapController __instance, bool __result)
        {
            if (!__result) return;
            try
            {
                __instance.StartCoroutine(Landmarks.RestoreWhenLoaded(__instance));
            }
            catch (Exception e)
            {
                ApiLog.Once(Landmarks.Owner, "MapController.OnLoadingGame postfix", e);
            }
        }
    }
}
