using System;
using System.Reflection;

namespace ObenseuerCore
{
    internal static class PhoneLinesPatches
    {
        internal static bool Install()
        {
            try
            {
                MethodInfo startPrefix = CorePatcher.Handler(typeof(PhoneLinesPatches), "StartPrefix");
                MethodInfo started = CorePatcher.Handler(typeof(PhoneLinesPatches), "StartedPostfix");
                MethodInfo ended = CorePatcher.Handler(typeof(PhoneLinesPatches), "EndedPostfix");
                MethodInfo receivePrefix = CorePatcher.Handler(typeof(PhoneLinesPatches), "ReceivePrefix");
                MethodInfo receivePostfix = CorePatcher.Handler(typeof(PhoneLinesPatches), "ReceivePostfix");
                return CorePatcher.PatchCore(typeof(Phone), "StartCall", new[] { typeof(PhoneNumber) }, startPrefix, null, null, true, PhoneLines.Owner)
                    && CorePatcher.PatchCore(typeof(Phone), "OnCallStarted", Type.EmptyTypes, null, started, null, true, PhoneLines.Owner)
                    && CorePatcher.PatchCore(typeof(Phone), "OnCallEnded", Type.EmptyTypes, null, ended, null, true, PhoneLines.Owner)
                    && CorePatcher.PatchCore(typeof(Phone), "ReceiveCall", new[] { typeof(PhoneNumber), typeof(int), typeof(Action), typeof(Action) }, receivePrefix, receivePostfix, null, true, PhoneLines.Owner);
            }
            catch (Exception e)
            {
                CoreLog.For(PhoneLines.Owner).Error("install failed: " + e.ToString());
                return false;
            }
        }

        private static void StartPrefix(PhoneNumber number)
        {
            PhoneLines.Active = number;
        }

        private static void StartedPostfix(Phone __instance)
        {
            try
            {
                PhoneLines.RaiseStarted(__instance);
            }
            catch (Exception e)
            {
                ApiLog.Once(PhoneLines.Owner, "PhoneLines.CallStarted", e);
            }
        }

        private static void EndedPostfix(Phone __instance)
        {
            try
            {
                PhoneLines.RaiseEnded(__instance);
            }
            catch (Exception e)
            {
                ApiLog.Once(PhoneLines.Owner, "PhoneLines.CallEnded", e);
            }
        }

        private static void ReceivePrefix(Phone __instance, out bool __state)
        {
            try
            {
                __state = PhoneLines.Incoming(__instance) == null;
            }
            catch (Exception e)
            {
                __state = false;
                ApiLog.Once(PhoneLines.Owner, "PhoneLines.Ringing prefix", e);
            }
        }

        private static void ReceivePostfix(Phone __instance, PhoneNumber number, bool __state)
        {
            try
            {
                if (__state) PhoneLines.RaiseRinging(__instance, number);
            }
            catch (Exception e)
            {
                ApiLog.Once(PhoneLines.Owner, "PhoneLines.Ringing", e);
            }
        }
    }
}
