using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using UnityEngine.SceneManagement;

namespace ObenseuerCore
{
    internal static class CoreDebugEvents
    {
        internal static void Init()
        {
            Events.SceneLoaded += Scene;
            Events.Saved += Saved;
            Events.Loaded += Loaded;
            CorePatcher.Installed += Installed;
        }

        internal static void Begin()
        {
            Dependents();
            DumpPatches();
            Snapshot("enabled", SceneManager.GetActiveScene().name);
        }

        private static void Scene(string scene)
        {
            Snapshot("scene loaded", scene);
        }

        private static void Saved()
        {
            Snapshot("saved", SceneManager.GetActiveScene().name);
        }

        private static void Loaded()
        {
            Snapshot("loaded", SceneManager.GetActiveScene().name);
        }

        private static void Snapshot(string when, string scene)
        {
            if (!CoreDebug.Enabled) return;
            CoreDebug.Write(when + ": scene " + scene + ", day " + GameTime.Day + " " + GameTime.Hour.ToString("00") + ":" + GameTime.Minute.ToString("00") + ", player " + CoreDebugFormat.Value(PlayerState.Position) + ", saving " + SaveGate.Saving + ", loading " + SaveGate.Loading);
        }

        private static void Installed(string owner, string target, MethodInfo[] handlers, bool ok)
        {
            if (!CoreDebug.Enabled) return;
            string[] names = { "prefix", "postfix", "transpiler", "finalizer" };
            List<string> kinds = new List<string>();
            for (int i = 0; i < handlers.Length; i++)
            {
                if (handlers[i] != null) kinds.Add(names[i]);
            }
            CoreDebug.Write("patch " + (ok ? "installed" : "failed") + " owner " + owner + " target " + target + " kinds " + string.Join(",", kinds.ToArray()));
            if (ok) CoreDebug.Extend(handlers);
        }

        private static void Dependents()
        {
            List<string> names = new List<string>();
            foreach (PluginInfo info in Loader.All())
            {
                foreach (BepInDependency dependency in info.Dependencies)
                {
                    if (dependency.DependencyGUID == ModIds.CoreId) names.Add(info.Metadata.GUID + " " + info.Metadata.Version);
                }
            }
            CoreDebug.Write("dependent plugins " + names.Count + (names.Count == 0 ? "" : ": " + string.Join(", ", names.ToArray())));
        }

        private static void DumpPatches()
        {
            int count = 0;
            foreach (MethodBase method in Harmony.GetAllPatchedMethods())
            {
                Patches patches = Harmony.GetPatchInfo(method);
                if (patches == null) continue;
                count++;
                CoreDebug.Write("patched " + CoreDebugFormat.Method(method) + " owners " + string.Join(",", patches.Owners) + " kinds " + Kinds(patches));
            }
            CoreDebug.Write("patched methods at enable: " + count);
        }

        private static string Kinds(Patches patches)
        {
            List<string> kinds = new List<string>();
            if (patches.Prefixes.Count > 0) kinds.Add("prefix");
            if (patches.Postfixes.Count > 0) kinds.Add("postfix");
            if (patches.Transpilers.Count > 0) kinds.Add("transpiler");
            if (patches.Finalizers.Count > 0) kinds.Add("finalizer");
            if (patches.ILManipulators.Count > 0) kinds.Add("ilmanipulator");
            return string.Join(",", kinds.ToArray());
        }
    }
}
