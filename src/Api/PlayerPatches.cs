using System;
using System.Collections.Generic;
using System.Reflection;

namespace ObenseuerCore
{
    internal static class PlayerPatches
    {
        internal static bool InstallSleep()
        {
            try
            {
                return Hook(PlayerState.Owner, typeof(WaitingController), "Sleep", new[] { typeof(int) }, null, "SleepPostfix");
            }
            catch (Exception e)
            {
                CorePatcher.DisableOwner(PlayerState.Owner);
                ApiLog.Once(PlayerState.Owner, "install sleep", e);
                return false;
            }
        }

        internal static bool InstallWait()
        {
            try
            {
                return Hook(PlayerState.WaitOwner, typeof(WaitingController), "Wait", new[] { typeof(int), typeof(WaitingController.Type), typeof(float), typeof(bool), typeof(IEnumerable<PlayerStatsTiredness.Reason>) }, null, "WaitPostfix");
            }
            catch (Exception e)
            {
                CorePatcher.DisableOwner(PlayerState.WaitOwner);
                ApiLog.Once(PlayerState.WaitOwner, "install", e);
                return false;
            }
        }

        internal static bool InstallWakeup()
        {
            try
            {
                return Hook(PlayerState.WakeupOwner, typeof(WaitingController), "Wakeup", new[] { typeof(bool), typeof(bool) }, "WakeupPrefix", "WakeupPostfix");
            }
            catch (Exception e)
            {
                CorePatcher.DisableOwner(PlayerState.WakeupOwner);
                ApiLog.Once(PlayerState.WakeupOwner, "install", e);
                return false;
            }
        }

        private static bool Hook(string owner, Type type, string name, Type[] args, string prefix, string postfix)
        {
            return CorePatcher.PatchCore(type, name, args, CorePatcher.Handler(typeof(PlayerPatches), prefix), CorePatcher.Handler(typeof(PlayerPatches), postfix), null, true, owner);
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
