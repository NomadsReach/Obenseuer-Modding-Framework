using System;
using NPC;

namespace ObenseuerCore
{
    internal static class RobbersPatches
    {
        internal static bool Install()
        {
            try
            {
                return CorePatcher.PatchCore(typeof(Act_Robber), "RobPlayer", Type.EmptyTypes, null, CorePatcher.Handler(typeof(RobbersPatches), "RobPostfix"), null, false, Robbers.Owner);
            }
            catch (Exception e)
            {
                ApiLog.Once(Robbers.Owner, "install", e);
                return false;
            }
        }

        private static void RobPostfix(Act_Robber __instance)
        {
            try
            {
                Robbers.OnRobbed(__instance);
            }
            catch (Exception e)
            {
                ApiLog.Once(Robbers.Owner, "RobPlayer postfix", e);
            }
        }
    }
}
