using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace ObenseuerCore
{
    internal static class LocksPatches
    {
        private static readonly ConditionalWeakTable<Lock, GameObject> owners = new ConditionalWeakTable<Lock, GameObject>();

        internal static bool Install()
        {
            if (Locks.KeypadOpen == null) return false;
            try
            {
                bool ok = Hook(typeof(Lock), "Use", new[] { typeof(Action), typeof(GameObject) }, "UsePrefix", null);
                ok &= Hook(typeof(Lock), "_Lock", Type.EmptyTypes, "LockPrefix", "LockPostfix");
                ok &= Hook(typeof(Lock), "_Unlock", Type.EmptyTypes, "LockPrefix", "LockPostfix");
                ok &= Hook(typeof(Door), "Lock", Type.EmptyTypes, "DoorPrefix", "DoorPostfix");
                ok &= Hook(typeof(Door), "Unlock", Type.EmptyTypes, "DoorPrefix", "DoorPostfix");
                ok &= Hook(typeof(Door), "ToggleLock", Type.EmptyTypes, "DoorPrefix", "DoorPostfix");
                ok &= Hook(typeof(Lock), "LockpickSuccessCallback", new[] { typeof(bool) }, "LockpickPrefix", "LockpickPostfix");
                return ok;
            }
            catch (Exception e)
            {
                CoreLog.For(Interactions.Owner).Error("lock hook install failed: " + e.ToString());
                return false;
            }
        }

        private static bool Hook(Type type, string name, Type[] args, string prefix, string postfix)
        {
            return CorePatcher.PatchCore(type, name, args, CorePatcher.Handler(typeof(LocksPatches), prefix), CorePatcher.Handler(typeof(LocksPatches), postfix), null, true, Interactions.Owner);
        }

        private static void UsePrefix(Lock __instance, GameObject parent)
        {
            try
            {
                owners.Remove(__instance);
                owners.Add(__instance, parent);
            }
            catch (Exception e)
            {
                CoreLog.For(Interactions.Owner).Error("Lock owner prefix failed: " + e.ToString());
            }
        }

        private static void LockPrefix(Lock __instance, out bool __state)
        {
            __state = __instance.Locked;
        }

        private static void LockPostfix(Lock __instance, bool __state)
        {
            try
            {
                if (__state == __instance.Locked) return;
                Locks.RaiseChanged(new LockArgs(OwnerOf(__instance)), __instance.Locked);
            }
            catch (Exception e)
            {
                CoreLog.For(Interactions.Owner).Error("Locked postfix failed: " + e.ToString());
            }
        }

        private static void DoorPrefix(Door __instance, out bool __state)
        {
            __state = __instance.Locked;
        }

        private static void DoorPostfix(Door __instance, bool __state)
        {
            try
            {
                if (__state == __instance.Locked) return;
                Locks.RaiseChanged(new LockArgs(__instance.gameObject), __instance.Locked);
            }
            catch (Exception e)
            {
                CoreLog.For(Interactions.Owner).Error("Door lock postfix failed: " + e.ToString());
            }
        }

        private static void LockpickPrefix(GameObject ____parent, Action ____CallbackUnlock, out GameObject __state)
        {
            __state = ____CallbackUnlock == null ? null : ____parent;
        }

        private static void LockpickPostfix(GameObject __state, bool success)
        {
            try
            {
                if (__state == null) return;
                Locks.RaiseLockpick(new LockArgs(__state), success);
            }
            catch (Exception e)
            {
                CoreLog.For(Interactions.Owner).Error("lockpick postfix failed: " + e.ToString());
            }
        }

        private static GameObject OwnerOf(Lock target)
        {
            GameObject owner;
            owners.TryGetValue(target, out owner);
            return owner;
        }
    }
}
