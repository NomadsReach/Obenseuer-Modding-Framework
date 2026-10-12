using System;
using System.Reflection;

namespace ObenseuerCore
{
    internal static class JanitorsPatches
    {
        private static int tagDepth;

        internal static bool IsReplay
        {
            get
            {
                return tagDepth == 0;
            }
        }

        internal static bool Install()
        {
            try
            {
                return Hook(typeof(StorageJanitorAction), "TriggerAction", Type.EmptyTypes, null, "StoragePostfix", null)
                    && Hook(typeof(RelayJanitor), "TriggerAction", Type.EmptyTypes, null, "RelayPostfix", null)
                    && Hook(typeof(JanitorController), "TriggerJanitorAction", new[] { typeof(string) }, "TagPrefix", null, "TagFinalizer");
            }
            catch (Exception e)
            {
                CoreLog.For(Janitors.Owner).Error("install failed: " + e.ToString());
                return false;
            }
        }

        private static bool Hook(Type type, string name, Type[] args, string prefix, string postfix, string finalizer)
        {
            return CorePatcher.PatchCore(type, name, args, CorePatcher.Handler(typeof(JanitorsPatches), prefix), CorePatcher.Handler(typeof(JanitorsPatches), postfix), CorePatcher.Handler(typeof(JanitorsPatches), finalizer), true, Janitors.Owner);
        }

        private static void TagPrefix()
        {
            tagDepth++;
        }

        private static void TagFinalizer()
        {
            if (tagDepth > 0) tagDepth--;
        }

        private static void StoragePostfix(StorageJanitorAction __instance)
        {
            try
            {
                Janitors.RaiseStorage(__instance);
            }
            catch (Exception e)
            {
                ApiLog.Once(Janitors.Owner, "Janitors.Acted", e);
            }
        }

        private static void RelayPostfix(RelayJanitor __instance)
        {
            try
            {
                Janitors.RaiseRelay(__instance);
            }
            catch (Exception e)
            {
                ApiLog.Once(Janitors.Owner, "Janitors.Acted", e);
            }
        }
    }
}
