using System;
using System.Collections.Generic;
using System.Reflection;

namespace ObenseuerCore
{
    internal static class PlayerPatches
    {
        internal static bool Install()
        {
            try
            {
                return Hook(typeof(WaitingController), "Sleep", new[] { typeof(int) }, "SleepPostfix")
                    & Hook(typeof(WaitingController), "Wait", new[] { typeof(int), typeof(WaitingController.Type), typeof(float), typeof(bool), typeof(IEnumerable<PlayerStatsTiredness.Reason>) }, "WaitPostfix")
                    & Hook(typeof(WaitingController), "Wakeup", new[] { typeof(bool), typeof(bool) }, "WakeupPrefix", "WakeupPostfix");
            }
            catch (Exception e)
            {
                ApiLog.Once(PlayerState.Owner, "install", e);
                CorePatcher.DisableOwner(PlayerState.Owner);
                return false;
            }
        }

        private static bool Hook(Type type, string name, Type[] args, string postfix)
        {
            return Hook(type, name, args, null, postfix);
        }

        private static bool Hook(Type type, string name, Type[] args, string prefix, string postfix)
        {
            return CorePatcher.PatchCore(type, name, args, CorePatcher.Handler(typeof(PlayerPatches), prefix), CorePatcher.Handler(typeof(PlayerPatches), postfix), null, true, PlayerState.Owner);
        }

        private static void SleepPostfix(int currentSleepSetTime, bool __result)
        {
            try
            {
                if (__result) PlayerState.RaiseSleepStarted(currentSleepSetTime);
            }
            catch (Exception e)
            {
                ApiLog.Once(PlayerState.Owner, "SleepPostfix", e);
            }
        }

        private static void WaitPostfix(WaitingController __instance)
        {
            try
            {
                int kind = (int)__instance.waitingType;
                if (Enum.IsDefined(typeof(WaitKind), kind)) PlayerState.RaiseWaitStarted((WaitKind)kind);
            }
            catch (Exception e)
            {
                ApiLog.Once(PlayerState.Owner, "WaitPostfix", e);
            }
        }

        private static void WakeupPrefix(WaitingController __instance, out bool __state)
        {
            __state = false;
            try
            {
                __state = __instance.IsSleeping || __instance.IsWaiting;
            }
            catch (Exception e)
            {
                ApiLog.Once(PlayerState.Owner, "WakeupPrefix", e);
            }
        }

        private static void WakeupPostfix(WaitingController __instance, bool __state)
        {
            try
            {
                if (__state && !__instance.IsSleeping && !__instance.IsWaiting) PlayerState.RaiseWokeUp();
            }
            catch (Exception e)
            {
                ApiLog.Once(PlayerState.Owner, "WakeupPostfix", e);
            }
        }
    }
}
