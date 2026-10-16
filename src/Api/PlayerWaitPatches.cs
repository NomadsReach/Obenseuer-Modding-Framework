using System;
using System.Collections.Generic;
using System.Reflection;

namespace ObenseuerCore
{
    internal static class PlayerWaitPatches
    {
        private const string LabelOwner = PlayerState.WaitResultOwner + ".label";

        internal static bool Install()
        {
            string owner = PlayerState.WaitResultOwner;
            try
            {
                CorePatcher.PatchCore(typeof(WaitingUI), "GetTypeString", Type.EmptyTypes, null, Handler("TypeStringPostfix"), null, true, LabelOwner);
                bool ok = true;
                ok &= CorePatcher.PatchCore(typeof(WaitingController), "Wakeup", new[] { typeof(bool), typeof(bool) }, null, null, Handler("WakeupFinalizer"), true, owner);
                ok &= CorePatcher.PatchCore(typeof(WaitingController), "PassOut", new[] { typeof(bool), typeof(WaitingController.Type) }, Handler("PassOutPrefix"), null, Handler("PassOutFinalizer"), true, owner);
                ok &= CorePatcher.PatchCore(typeof(WaitingController), "Wait", new[] { typeof(int), typeof(WaitingController.Type), typeof(float), typeof(bool), typeof(IEnumerable<PlayerStatsTiredness.Reason>) }, Handler("WaitPrefix"), null, null, true, owner);
                ok &= CorePatcher.PatchCore(typeof(WaitingController), "OnWait", new[] { typeof(int) }, null, Handler("OnWaitPostfix"), null, true, owner);
                ok &= CorePatcher.PatchCore(typeof(PlayerStatsTiredness), "UpdateStats", new[] { typeof(int) }, Handler("TirednessPrefix"), null, null, true, owner);
                ok &= CorePatcher.PatchCore(typeof(PlayerStatsHungerAndThirst), "UpdateStats", new[] { typeof(int) }, Handler("NeedsPrefix"), null, null, true, owner);
                ok &= CorePatcher.PatchCore(typeof(PlayerStatsMentalHealth), "UpdateStats", new[] { typeof(int) }, Handler("NeedsPrefix"), null, null, true, owner);
                ok &= CorePatcher.PatchCore(typeof(PlayerStatsHygiene), "UpdateStats", new[] { typeof(int) }, Handler("NeedsPrefix"), null, null, true, owner);
                return ok;
            }
            catch (Exception e)
            {
                CorePatcher.DisableOwner(owner);
                ApiLog.Once(owner, "install", e);
                return false;
            }
        }

        private static MethodInfo Handler(string name)
        {
            return CorePatcher.Handler(typeof(PlayerWaitPatches), name);
        }

        private static void WakeupFinalizer()
        {
            try
            {
                PlayerWait.EndIfOver();
            }
            catch (Exception e)
            {
                ApiLog.Once(PlayerState.WaitResultOwner, "WakeupFinalizer", e);
            }
        }

        private static void WaitPrefix()
        {
            try
            {
                PlayerWait.GameWaitStarting();
            }
            catch (Exception e)
            {
                ApiLog.Once(PlayerState.WaitResultOwner, "WaitPrefix", e);
            }
        }

        private static void TypeStringPostfix(ref string __result)
        {
            try
            {
                string label = PlayerWait.Label;
                WaitingController waiting = WaitingController.instance;
                if (!string.IsNullOrEmpty(label) && waiting != null && waiting.IsWaiting) __result = label;
            }
            catch (Exception e)
            {
                ApiLog.Once(LabelOwner, "TypeStringPostfix", e);
            }
        }

        private static bool PassOutPrefix(WaitingController.Type type)
        {
            try
            {
                PlayerWait.PassingOut = true;
                return !(type == WaitingController.Type.Passout && PlayerWait.BlockPassOut);
            }
            catch (Exception e)
            {
                ApiLog.Once(PlayerState.WaitResultOwner, "PassOutPrefix", e);
                return true;
            }
        }

        private static void PassOutFinalizer()
        {
            try
            {
                PlayerWait.PassingOut = false;
                PlayerWait.EndIfOver();
            }
            catch (Exception e)
            {
                ApiLog.Once(PlayerState.WaitResultOwner, "PassOutFinalizer", e);
            }
        }

        private static void OnWaitPostfix(int sec)
        {
            try
            {
                PlayerWait.Tick(sec);
            }
            catch (Exception e)
            {
                ApiLog.Once(PlayerState.WaitResultOwner, "OnWaitPostfix", e);
            }
        }

        private static bool TirednessPrefix(int sec)
        {
            try
            {
                if (!PlayerWait.FreezeNeeds) return true;
                WaitingController waiting = WaitingController.instance;
                if (waiting != null && waiting.IsWaiting)
                {
                    waiting.OnWait(sec);
                    waiting.CheckEvent(sec);
                }
                return false;
            }
            catch (Exception e)
            {
                ApiLog.Once(PlayerState.WaitResultOwner, "TirednessPrefix", e);
                return true;
            }
        }

        private static bool NeedsPrefix()
        {
            try
            {
                return !PlayerWait.FreezeNeeds;
            }
            catch (Exception e)
            {
                ApiLog.Once(PlayerState.WaitResultOwner, "NeedsPrefix", e);
                return true;
            }
        }
    }
}
