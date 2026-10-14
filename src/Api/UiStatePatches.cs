using System;

namespace ObenseuerCore
{
    internal static class UiStatePatches
    {
        internal static bool Install()
        {
            try
            {
                return CorePatcher.PatchCore(typeof(PauseMenu), "PauseMenuOpen", Type.EmptyTypes, null, CorePatcher.Handler(typeof(UiStatePatches), "OpenPostfix"), null, true, UiState.Owner)
                    && CorePatcher.PatchCore(typeof(PauseMenu), "PauseMenuClose", Type.EmptyTypes, CorePatcher.Handler(typeof(UiStatePatches), "ClosePrefix"), CorePatcher.Handler(typeof(UiStatePatches), "ClosePostfix"), null, true, UiState.Owner);
            }
            catch (Exception e)
            {
                CoreLog.For(UiState.Owner).Error("pause patch install failed: " + e.ToString());
                return false;
            }
        }

        private static void OpenPostfix()
        {
            try
            {
                UiState.RaisePauseOpened(Death.instance != null && Death.instance.dead);
            }
            catch (Exception e)
            {
                ApiLog.Once(UiState.Owner, "Open postfix", e);
            }
        }

        private static void ClosePrefix(PauseMenu __instance, out bool __state)
        {
            __state = false;
            try
            {
                __state = __instance != null && __instance.PauseIsActive();
            }
            catch (Exception e)
            {
                ApiLog.Once(UiState.Owner, "Close prefix", e);
            }
        }

        private static void ClosePostfix(bool __state)
        {
            if (!__state) return;
            try
            {
                UiState.RaisePauseClosed();
            }
            catch (Exception e)
            {
                ApiLog.Once(UiState.Owner, "Close postfix", e);
            }
        }
    }
}
