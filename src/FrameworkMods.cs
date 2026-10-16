using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using BepInEx;
using UnityEngine;

namespace ObenseuerCore
{
    internal static class FrameworkMods
    {
        private static readonly Dictionary<string, FrameworkMod> loaded = new Dictionary<string, FrameworkMod>(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> installed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        internal static void Load(Core core)
        {
            Stopwatch timer = Stopwatch.StartNew();
            CoreLog log = CoreLog.For("ObenseuerCore");
            List<FrameworkModScan.Candidate> found = new List<FrameworkModScan.Candidate>();
            foreach (string file in Directory.GetFiles(Paths.PluginPath, "*.dll", SearchOption.AllDirectories)) found.AddRange(FrameworkModScan.Read(file));
            FrameworkModScan.PlanResult plan = FrameworkModScan.Plan(found, Plugins(), log.Warn);
            foreach (FrameworkModScan.Candidate candidate in plan.Present) installed.Add(candidate.Id);
            foreach (FrameworkModScan.Candidate candidate in plan.Ordered) Start(core, candidate, plan.Ordered, log);
            log.Info("framework mods: " + loaded.Count + " loaded, " + (found.Count - loaded.Count) + " skipped, " + timer.ElapsedMilliseconds + " ms");
        }

        internal static bool IsLoaded(string id)
        {
            return id != null && loaded.ContainsKey(id);
        }

        internal static bool IsInstalled(string id)
        {
            return id != null && installed.Contains(id);
        }

        internal static List<FrameworkMod> Loaded()
        {
            return new List<FrameworkMod>(loaded.Values);
        }

        private static void Start(Core core, FrameworkModScan.Candidate candidate, List<FrameworkModScan.Candidate> planned, CoreLog log)
        {
            Component[] before = null;
            try
            {
                string missing = NotLoaded(candidate, planned);
                if (missing != null)
                {
                    log.Warn("skipped " + candidate.TypeName + ": dependency " + missing + " did not load");
                    return;
                }
                Assembly assembly = AssemblyLoaded(candidate.AssemblyName);
                if (assembly != null && !SamePath(assembly.Location, candidate.Path))
                {
                    log.Warn("skipped " + candidate.TypeName + ": " + candidate.AssemblyName + " is already loaded from " + assembly.Location + ", not " + candidate.Path);
                    return;
                }
                if (assembly == null) assembly = Assembly.LoadFrom(candidate.Path);
                Type type = assembly.GetType(candidate.TypeName);
                if (type == null || type.IsAbstract || !typeof(FrameworkMod).IsAssignableFrom(type)) throw new TypeLoadException(candidate.TypeName + " must be a non-abstract class that derives from FrameworkMod");
                before = core.GetComponents<Component>();
                FrameworkMod mod = (FrameworkMod)core.gameObject.AddComponent(type);
                mod.RunOnLoad();
                loaded[candidate.Id] = mod;
            }
            catch (Exception e)
            {
                RemoveAdded(core, before, log);
                CoreLog.For(candidate.Id).Error("framework mod failed to load: " + e.Message, e);
                log.Error(candidate.TypeName + " in " + candidate.Path + " failed to load, other mods still run: " + e.Message, e);
            }
        }

        private static void RemoveAdded(Core core, Component[] before, CoreLog log)
        {
            if (before == null) return;
            foreach (Component added in core.GetComponents<Component>())
            {
                if (added == null || Array.IndexOf(before, added) >= 0) continue;
                try
                {
                    Behaviour behaviour = added as Behaviour;
                    if (behaviour != null) behaviour.enabled = false;
                    UnityEngine.Object.DestroyImmediate(added);
                }
                catch (Exception e)
                {
                    log.Error("a component of a framework mod that threw in OnLoad could not be removed: " + e.Message, e);
                }
            }
        }

        private static string NotLoaded(FrameworkModScan.Candidate candidate, List<FrameworkModScan.Candidate> planned)
        {
            foreach (FrameworkModScan.Dependency dependency in candidate.Dependencies)
            {
                if (FrameworkModScan.Find(planned, dependency.Id) != null && !IsLoaded(dependency.Id)) return dependency.Id;
            }
            return null;
        }

        private static Assembly AssemblyLoaded(string assemblyName)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetName().FullName == assemblyName) return assembly;
            }
            return null;
        }

        private static bool SamePath(string a, string b)
        {
            return !string.IsNullOrEmpty(a) && string.Equals(Path.GetFullPath(a), Path.GetFullPath(b), StringComparison.OrdinalIgnoreCase);
        }

        private static Dictionary<string, Version> Plugins()
        {
            Dictionary<string, Version> plugins = new Dictionary<string, Version>(StringComparer.OrdinalIgnoreCase);
            foreach (PluginInfo info in Loader.All())
            {
                plugins[info.Metadata.GUID] = info.Instance == null ? null : FrameworkModScan.PluginVersion(info.Metadata.Version.ToString());
            }
            return plugins;
        }
    }
}
