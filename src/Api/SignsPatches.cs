using System;

namespace ObenseuerCore
{
    internal static class SignsPatches
    {
        internal static bool Install()
        {
            try
            {
                return CorePatcher.PatchCore(typeof(EditableSign), "StartEditing", Type.EmptyTypes, null, CorePatcher.Handler(typeof(SignsPatches), "StartEditingPostfix"), null, true, Signs.Owner)
                    && CorePatcher.PatchCore(typeof(EditableSign), "StopEditing", new[] { typeof(bool) }, null, CorePatcher.Handler(typeof(SignsPatches), "StopEditingPostfix"), null, true, Signs.Owner);
            }
            catch (Exception e)
            {
                CoreLog.For(Signs.Owner).Error("sign hook install failed: " + e.ToString());
                return false;
            }
        }

        private static void StartEditingPostfix(EditableSign __instance)
        {
            if (!Signs.HasHandlers) return;
            try
            {
                if (__instance.textComponent != null && __instance.textComponent.interactable) Signs.BeginEdit(__instance);
            }
            catch (Exception e)
            {
                ApiLog.Once(Signs.Owner, "Signs.StartEditing", e);
            }
        }

        private static void StopEditingPostfix(EditableSign __instance)
        {
            try
            {
                Signs.EndEdit(__instance);
            }
            catch (Exception e)
            {
                ApiLog.Once(Signs.Owner, "Signs.StopEditing", e);
            }
        }
    }
}
