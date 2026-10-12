using System;
using System.Reflection;

namespace ObenseuerCore
{
    internal static class PursuitPatches
    {
        internal static bool Install()
        {
            if (Pursuit.ActivePolicesField == null || Pursuit.StopMethod == null) return false;
            try
            {
                bool ok = Hook("StartState");
                ok &= Hook("OnPlayerArrested");
                ok &= Hook("StopActivity");
                return ok;
            }
            catch (Exception e)
            {
                CoreLog.For(Pursuit.Owner).Error("pursuit hook install failed: " + e.ToString());
                return false;
            }
        }

        private static bool Hook(string name)
        {
            MethodInfo changed = CorePatcher.Handler(typeof(PursuitPatches), "Changed");
            return CorePatcher.PatchCore(typeof(NPC.Act_Police), name, Type.EmptyTypes, null, changed, null, true, Pursuit.Owner);
        }

        private static void Changed()
        {
            try
            {
                Pursuit.Refresh();
            }
            catch (Exception e)
            {
                ApiLog.Once(Pursuit.Owner, "Changed", e);
            }
        }
    }
}
