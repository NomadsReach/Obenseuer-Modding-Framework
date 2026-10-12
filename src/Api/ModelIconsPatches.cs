using System;
using System.Reflection;

namespace ObenseuerCore
{
    internal static class ModelIconsPatches
    {
        internal static bool Installed;

        internal static void Init()
        {
            Installed = Install();
        }

        private static bool Install()
        {
            try
            {
                return Hook(typeof(ItemDatabase), "LoadItemReferencedAssets", new[] { typeof(Item), typeof(bool) }, "LoadItemReferencedAssetsPostfix");
            }
            catch (Exception e)
            {
                CorePatcher.DisableOwner(ModelIcons.Owner);
                CoreLog.For(ModelIcons.Owner).Warn("icon patches not installed: " + e.ToString());
                return false;
            }
        }

        private static bool Hook(Type type, string name, Type[] args, string postfix)
        {
            return CorePatcher.PatchCore(type, name, args, null, CorePatcher.Handler(typeof(ModelIconsPatches), postfix), null, true, ModelIcons.Owner);
        }

        private static void LoadItemReferencedAssetsPostfix(Item i)
        {
            try
            {
                ItemIcons.Reapply(i);
            }
            catch (Exception e)
            {
                ApiLog.Once(ModelIcons.Owner, "LoadItemReferencedAssets postfix", e);
            }
        }
    }
}
