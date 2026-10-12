using System;

namespace ObenseuerCore
{
    internal static class JobsPatches
    {
        internal static bool Install()
        {
            try
            {
                return CorePatcher.PatchCore(typeof(InteractableWaitWork), "WaitDone", new[] { typeof(int) }, CorePatcher.Handler(typeof(JobsPatches), "WorkedPrefix"), CorePatcher.Handler(typeof(JobsPatches), "WorkedPostfix"), null, true, Jobs.Owner);
            }
            catch (Exception e)
            {
                CoreLog.For(Jobs.Owner).Error("job hook install failed: " + e.ToString());
                return false;
            }
        }

        private static void WorkedPrefix(out MoneyPatches.Snapshot __state)
        {
            __state = null;
            if (!Jobs.HasHandlers) return;
            try
            {
                __state = new MoneyPatches.Snapshot(Money.instance.OCMoneyAmount, Money.instance.RMoneyAmount);
            }
            catch (Exception e)
            {
                ApiLog.Once(Jobs.Owner, "Jobs.WorkedPrefix", e);
            }
        }

        private static void WorkedPostfix(InteractableWaitWork __instance, int timeWaited, MoneyPatches.Snapshot __state)
        {
            if (__state == null) return;
            try
            {
                float payRm = (float)Math.Round(Money.instance.RMoneyAmount - __state.Rm, 1);
                Jobs.RaiseWorked(__instance, timeWaited, Money.instance.OCMoneyAmount - __state.Oc, payRm);
            }
            catch (Exception e)
            {
                ApiLog.Once(Jobs.Owner, "Jobs.Worked", e);
            }
        }
    }
}
