using System;
using System.Reflection;
using UnityEngine;

namespace ObenseuerCore
{
    internal static class InteractionsPatches
    {
        internal static bool Install()
        {
            try
            {
                return CorePatcher.PatchCore(typeof(InteractObjects), "UseInteractable", Type.EmptyTypes, CorePatcher.Handler(typeof(InteractionsPatches), "UsePrefix"), CorePatcher.Handler(typeof(InteractionsPatches), "UsePostfix"), CorePatcher.Handler(typeof(InteractionsPatches), "UseFinalizer"), true, Interactions.Owner);
            }
            catch (Exception e)
            {
                CoreLog.For(Interactions.Owner).Error("interaction hook install failed: " + e.ToString());
                return false;
            }
        }

        private static void UsePrefix(InteractObjects __instance, out Component __state)
        {
            __state = null;
            if (__instance.savedForceInteractable != null) __state = __instance.savedForceInteractable;
            else if (__instance.interactable == null) __state = __instance.savedInteractable;
            else __state = __instance.interactable as Component;
            Interactions.Dispatching = __state;
        }

        private static void UsePostfix(Component __state)
        {
            Interactions.Dispatching = null;
            if (__state == null || !Interactions.HasUsedHandlers) return;
            try
            {
                Interactions.RaiseUsed(new InteractionArgs(__state.gameObject, __state));
            }
            catch (Exception e)
            {
                CoreLog.For(Interactions.Owner).Error("Used postfix failed: " + e.ToString());
            }
        }

        private static void UseFinalizer()
        {
            Interactions.Dispatching = null;
        }
    }
}
