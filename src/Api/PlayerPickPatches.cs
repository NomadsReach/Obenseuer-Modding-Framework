using System;

namespace ObenseuerCore
{
    internal static class PlayerPickPatches
    {
        internal static bool Install()
        {
            string owner = PlayerState.PickOwner;
            try
            {
                bool ok = true;
                ok &= CorePatcher.PatchCore(typeof(WaitingUI), "StartSleep", Type.EmptyTypes, CorePatcher.Handler(typeof(PlayerPickPatches), "StartSleepPrefix"), null, null, true, owner);
                ok &= CorePatcher.PatchCore(typeof(WaitingUI), "CloseTimeSetUI", Type.EmptyTypes, CorePatcher.Handler(typeof(PlayerPickPatches), "CloseTimeSetUIPrefix"), null, null, true, owner);
                ok &= CorePatcher.PatchCore(typeof(WaitingUI), "UpdateSleptTime", new[] { typeof(float) }, null, CorePatcher.Handler(typeof(PlayerPickPatches), "SleptTimePostfix"), null, true, owner);
                ok &= CorePatcher.PatchCore(typeof(WaitingController), "PassOut", new[] { typeof(bool), typeof(WaitingController.Type) }, CorePatcher.Handler(typeof(PlayerPickPatches), "PassOutPrefix"), null, null, true, owner);
                return ok;
            }
            catch (Exception e)
            {
                CorePatcher.DisableOwner(owner);
                ApiLog.Once(owner, "install", e);
                return false;
            }
        }

        private static bool StartSleepPrefix()
        {
            if (!PlayerPick.IsOpen) return true;
            try
            {
                PlayerPick.Confirm();
            }
            catch (Exception e)
            {
                ApiLog.Once(PlayerState.PickOwner, "StartSleepPrefix", e);
            }
            return false;
        }

        private static bool CloseTimeSetUIPrefix()
        {
            if (!PlayerPick.IsOpen) return true;
            try
            {
                PlayerPick.Cancel();
            }
            catch (Exception e)
            {
                ApiLog.Once(PlayerState.PickOwner, "CloseTimeSetUIPrefix", e);
            }
            return true;
        }

        private static void SleptTimePostfix()
        {
            try
            {
                PlayerPick.ShowTitle();
            }
            catch (Exception e)
            {
                ApiLog.Once(PlayerState.PickOwner, "SleptTimePostfix", e);
            }
        }

        private static void PassOutPrefix()
        {
            if (!PlayerPick.IsOpen || WaitingUI.instance == null) return;
            try
            {
                WaitingUI.instance.CloseTimeSetUI();
            }
            catch (Exception e)
            {
                ApiLog.Once(PlayerState.PickOwner, "PassOutPrefix", e);
            }
        }
    }
}
