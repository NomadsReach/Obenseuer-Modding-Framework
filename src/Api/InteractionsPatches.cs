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

        private static void UsePrefix(InteractObjects __instance, out UseState __state)
        {
            Component target = null;
            if (__instance.savedForceInteractable != null) target = __instance.savedForceInteractable;
            else if (__instance.interactable == null) target = __instance.savedInteractable;
            else target = __instance.interactable as Component;
            __state = new UseState { Target = target, Previous = Interactions.Dispatching };
            Interactions.Dispatching = target;
        }

        private static void UsePostfix(UseState __state)
        {
            if (__state == null) return;
            Interactions.Dispatching = __state.Previous;
            if (__state.Target == null || !Interactions.HasUsedHandlers) return;
            try
            {
                Interactions.RaiseUsed(new InteractionArgs(__state.Target.gameObject, __state.Target));
            }
            catch (Exception e)
            {
                CoreLog.For(Interactions.Owner).Error("Used postfix failed: " + e.ToString());
            }
        }

        private static void UseFinalizer(UseState __state)
        {
            if (__state != null) Interactions.Dispatching = __state.Previous;
        }

        internal sealed class UseState
        {
            internal Component Target;
            internal Component Previous;
        }
    }
}
