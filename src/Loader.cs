using System.Collections.Generic;
using BepInEx;
#if BEPINEX5
using BepInEx.Bootstrap;
#else
using BepInEx.Unity.Mono.Bootstrap;
#endif

namespace ObenseuerCore
{
    public static class Loader
    {
        public static bool IsBepInEx5
        {
#if BEPINEX5
            get { return true; }
#else
            get { return false; }
#endif
        }

        public static string Name
        {
#if BEPINEX5
            get { return "BepInEx " + typeof(Chainloader).Assembly.GetName().Version; }
#else
            get { return "BepInEx " + Paths.BepInExVersion; }
#endif
        }

        public static PluginInfo Find(string guid)
        {
            Dictionary<string, PluginInfo> plugins = Table();
            PluginInfo info;
            return guid != null && plugins != null && plugins.TryGetValue(guid, out info) ? info : null;
        }

        public static bool Has(string id)
        {
            PluginInfo info = Find(id);
            return (info != null && info.Instance != null) || FrameworkMods.IsLoaded(id);
        }

        internal static bool Installed(string id)
        {
            return Find(id) != null || FrameworkMods.IsInstalled(id);
        }

        public static List<PluginInfo> All()
        {
            Dictionary<string, PluginInfo> plugins = Table();
            return plugins == null ? new List<PluginInfo>() : new List<PluginInfo>(plugins.Values);
        }

        private static Dictionary<string, PluginInfo> Table()
        {
#if BEPINEX5
            return Chainloader.PluginInfos;
#else
            return UnityChainloader.Instance == null ? null : UnityChainloader.Instance.Plugins;
#endif
        }
    }
}
