using System;
using System.Reflection;
using UnityEngine;

namespace ObenseuerCore
{
    internal static class TrainsPatches
    {
        internal static bool Install()
        {
            try
            {
                return Hook(typeof(TrackTrain), "Update", Type.EmptyTypes, "UpdatePrefix", "UpdatePostfix");
            }
            catch (Exception e)
            {
                CoreLog.For(Trains.Owner).Error("install failed: " + e.ToString());
                return false;
            }
        }

        internal static bool InstallCrash()
        {
            try
            {
                return CorePatcher.PatchCore(typeof(TrackTrain), "Crash", new[] { typeof(GameObject), typeof(bool) }, CorePatcher.Handler(typeof(TrainsPatches), "CrashPrefix"), CorePatcher.Handler(typeof(TrainsPatches), "CrashPostfix"), null, true, Trains.CrashOwner);
            }
            catch (Exception e)
            {
                CoreLog.For(Trains.CrashOwner).Error("install crash failed: " + e.ToString());
                return false;
            }
        }

        private static bool Hook(Type type, string name, Type[] args, string prefix, string postfix)
        {
            return CorePatcher.PatchCore(type, name, args, CorePatcher.Handler(typeof(TrainsPatches), prefix), CorePatcher.Handler(typeof(TrainsPatches), postfix), null, false, Trains.Owner);
        }

        private static void UpdatePrefix(TrackTrain __instance, out bool __state)
        {
            __state = __instance.currentSpeed != 0f;
        }

        private static void UpdatePostfix(TrackTrain __instance, bool __state)
        {
            try
            {
                bool moving = __instance.currentSpeed != 0f;
                if (moving == __state) return;
                if (moving) Trains.RaiseDeparted(__instance);
                else Trains.RaiseArrived(__instance);
            }
            catch (Exception e)
            {
                ApiLog.Once(Trains.Owner, "Trains.Departed/Arrived", e);
            }
        }

        private static void CrashPrefix(TrackTrain __instance, out float __state)
        {
            __state = __instance.currentSpeed;
        }

        private static void CrashPostfix(TrackTrain __instance, GameObject other, float __state)
        {
            try
            {
                if (__state != 0f && __instance.currentSpeed == 0f) Trains.RaiseCrashed(__instance, other, __state);
            }
            catch (Exception e)
            {
                ApiLog.Once(Trains.Owner, "Trains.Crashed", e);
            }
        }
    }
}
