using System;
using System.Reflection;

namespace ObenseuerCore
{
    internal static class ElevatorsPatches
    {
        internal static bool Install()
        {
            try
            {
                return Elevators.MovingField != null && Elevators.QueueField != null && Hook(typeof(Elevator), "FixedUpdate", Type.EmptyTypes, "MovePrefix", "MovePostfix");
            }
            catch (Exception e)
            {
                CoreLog.For(Elevators.Owner).Error("install failed: " + e.ToString());
                return false;
            }
        }

        private static bool Hook(Type type, string name, Type[] args, string prefix, string postfix)
        {
            return CorePatcher.PatchCore(type, name, args, CorePatcher.Handler(typeof(ElevatorsPatches), prefix), CorePatcher.Handler(typeof(ElevatorsPatches), postfix), null, false, Elevators.Owner);
        }

        private static void MovePrefix(int ___moving, out bool __state)
        {
            __state = ___moving != 0;
        }

        private static void MovePostfix(Elevator __instance, int ___moving, bool __state)
        {
            try
            {
                if (__state && ___moving == 0) Elevators.RaiseArrived(__instance);
            }
            catch (Exception e)
            {
                ApiLog.Once(Elevators.Owner, "Elevators.Arrived", e);
            }
        }
    }
}
