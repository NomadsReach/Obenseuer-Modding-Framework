using System;

namespace ObenseuerCore
{
    internal static class InputCapturePatches
    {
        internal static bool Install()
        {
            bool down = Hook("GetKeyDown", new[] { typeof(string) }, InputCapture.DownOwner);
            bool key = Hook("GetKey", new[] { typeof(string) }, InputCapture.KeyOwner);
            bool hold = Hook("GetKeyWithHold", new[] { typeof(string), typeof(int) }, InputCapture.HoldOwner);
            bool up = Hook("GetKeyUp", new[] { typeof(string) }, InputCapture.UpOwner);
            bool only = Hook("GetKeyDownKeyboardOnly", new[] { typeof(string) }, InputCapture.KeyboardOnlyOwner);
            return down || key || hold || up || only;
        }

        private static bool Hook(string name, Type[] args, string owner)
        {
            try
            {
                return CorePatcher.PatchCore(typeof(InputManager), name, args, null, CorePatcher.Handler(typeof(InputCapturePatches), "BlockedPostfix"), null, true, owner);
            }
            catch (Exception e)
            {
                ApiLog.Once(owner, "input patch " + name, e);
                return false;
            }
        }

        private static void BlockedPostfix(string __0, ref bool __result)
        {
            if (!__result) return;
            try
            {
                if (InputCapture.Swallowing(__0)) __result = false;
            }
            catch (Exception e)
            {
                ApiLog.Once(InputCapture.Owner, "key block check", e);
            }
        }
    }
}
