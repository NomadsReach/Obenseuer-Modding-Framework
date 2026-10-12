using System;
using System.Reflection;

namespace ObenseuerCore
{
    internal static class DoorsPatches
    {
        internal static bool Install()
        {
            try
            {
                bool ok = Hook("OnFullyOpen", "OpenedPostfix");
                ok &= Hook("OnFullyClosed", "ClosedPostfix");
                return ok;
            }
            catch (Exception e)
            {
                CoreLog.For(Doors.Owner).Error("door hook install failed: " + e.ToString());
                return false;
            }
        }

        private static bool Hook(string name, string postfix)
        {
            return CorePatcher.PatchCore(typeof(Door), name, Type.EmptyTypes, null, CorePatcher.Handler(typeof(DoorsPatches), postfix), null, true, Doors.Owner);
        }

        private static void OpenedPostfix(Door __instance)
        {
            try
            {
                Doors.RaiseChanged(__instance, true);
            }
            catch (Exception e)
            {
                ApiLog.Once(Doors.Owner, "Doors.Opened", e);
            }
        }

        private static void ClosedPostfix(Door __instance)
        {
            try
            {
                Doors.RaiseChanged(__instance, false);
            }
            catch (Exception e)
            {
                ApiLog.Once(Doors.Owner, "Doors.Closed", e);
            }
        }
    }
}
