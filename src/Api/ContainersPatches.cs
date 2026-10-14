using System;

namespace ObenseuerCore
{
    internal static class ContainersPatches
    {
        internal static bool Install()
        {
            try
            {
                bool ok = Hook("EnterExternally", new Type[] { typeof(bool) }, "OpenedPostfix");
                ok &= Hook("ExitExternally", Type.EmptyTypes, "ClosedPostfix");
                ok &= Hook("OnDisable", Type.EmptyTypes, "DisabledPostfix");
                return ok;
            }
            catch (Exception e)
            {
                CoreLog.For(Containers.Owner).Error("container hook install failed: " + e.ToString());
                return false;
            }
        }

        private static bool Hook(string name, Type[] args, string postfix)
        {
            return CorePatcher.PatchCore(typeof(Storage), name, args, null, CorePatcher.Handler(typeof(ContainersPatches), postfix), null, true, Containers.Owner);
        }

        private static void OpenedPostfix(Storage __instance)
        {
            try
            {
                Containers.RaiseOpened(__instance);
            }
            catch (Exception e)
            {
                ApiLog.Once(Containers.Owner, "Containers.Opened", e);
            }
        }

        private static void ClosedPostfix(Storage __instance)
        {
            try
            {
                Containers.RaiseClosed(__instance);
            }
            catch (Exception e)
            {
                ApiLog.Once(Containers.Owner, "Containers.Closed", e);
            }
        }

        private static void DisabledPostfix(Storage __instance)
        {
            try
            {
                Containers.RaiseClosed(__instance);
            }
            catch (Exception e)
            {
                ApiLog.Once(Containers.Owner, "Containers.Closed (disabled)", e);
            }
        }
    }
}
