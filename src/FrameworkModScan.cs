using System;
using System.Collections.Generic;
using System.Globalization;
using Mono.Cecil;

namespace ObenseuerCore
{
    internal static class FrameworkModScan
    {
        private const string FrameworkAssembly = "ObenseuerCore";
        private const string InfoAttribute = "ObenseuerCore.ModInfoAttribute";
        private const string DependencyAttribute = "ObenseuerCore.ModDependencyAttribute";
        private static readonly HashSet<string> Reserved = new HashSet<string>(new[] { "ObenseuerCore", ModIds.LegacyCoreId, "ObenseuerCore.ModKeybinds", ModIds.CoreId }, StringComparer.OrdinalIgnoreCase);

        internal sealed class Candidate
        {
            internal string Path;
            internal string AssemblyName;
            internal string TypeName;
            internal string Id;
            internal string Name;
            internal string VersionText;
            internal Version Parsed;
            internal readonly List<Dependency> Dependencies = new List<Dependency>();
        }

        internal sealed class Dependency
        {
            internal string Id;
            internal string MinVersionText;
            internal Version MinVersion;
        }

        internal sealed class PlanResult
        {
            internal readonly List<Candidate> Ordered = new List<Candidate>();
            internal readonly List<Candidate> Present = new List<Candidate>();
        }

        internal static List<Candidate> Read(string path)
        {
            List<Candidate> found = new List<Candidate>();
            try
            {
                using (AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(path))
                {
                    if (!References(assembly.MainModule)) return found;
                    foreach (TypeDefinition type in assembly.MainModule.Types)
                    {
                        List<CustomAttribute> infos = Named(type, InfoAttribute);
                        if (type.IsNested || infos.Count == 0) continue;
                        Candidate candidate = new Candidate();
                        candidate.Path = path;
                        candidate.AssemblyName = assembly.Name.FullName;
                        candidate.TypeName = type.FullName;
                        candidate.Id = Text(infos[0], 0);
                        candidate.Name = Text(infos[0], 1);
                        candidate.VersionText = Text(infos[0], 2);
                        foreach (CustomAttribute dependency in Named(type, DependencyAttribute))
                        {
                            Dependency needed = new Dependency();
                            needed.Id = Text(dependency, 0);
                            needed.MinVersionText = Text(dependency, 1);
                            candidate.Dependencies.Add(needed);
                        }
                        found.Add(candidate);
                    }
                }
            }
            catch (Exception)
            {
                return new List<Candidate>();
            }
            return found;
        }

        internal static Version ParseVersion(string text)
        {
            if (text == null) return null;
            string[] parts = text.Split('.');
            if (parts.Length < 2 || parts.Length > 4) return null;
            int[] numbers = new int[4];
            for (int i = 0; i < parts.Length; i++)
            {
                if (!int.TryParse(parts[i], NumberStyles.None, CultureInfo.InvariantCulture, out numbers[i])) return null;
            }
            return new Version(numbers[0], numbers[1], numbers[2], numbers[3]);
        }

        internal static Version PluginVersion(string text)
        {
            int cut = text.IndexOfAny(new char[] { '-', '+' });
            return ParseVersion(cut < 0 ? text : text.Substring(0, cut));
        }

        internal static Candidate Find(List<Candidate> list, string id)
        {
            foreach (Candidate candidate in list)
            {
                if (string.Equals(candidate.Id, id, StringComparison.OrdinalIgnoreCase)) return candidate;
            }
            return null;
        }

        internal static PlanResult Plan(List<Candidate> found, Dictionary<string, Version> plugins, Action<string> warn)
        {
            found.Sort(ByPathAndType);
            PlanResult result = new PlanResult();
            List<Candidate> valid = new List<Candidate>();
            foreach (Candidate candidate in found)
            {
                string problem = Problem(candidate, valid, plugins);
                if (problem == null)
                {
                    valid.Add(candidate);
                    result.Present.Add(candidate);
                }
                else warn("skipped " + Where(candidate) + ": " + problem);
            }
            Prune(valid, plugins, warn);
            result.Ordered.AddRange(Order(valid, warn));
            return result;
        }

        internal static string SelfCheck()
        {
            Dictionary<string, Version> plugins = new Dictionary<string, Version>(StringComparer.OrdinalIgnoreCase);
            plugins["obenseuer.moddingframework"] = ParseVersion("0.11");
            plugins["test.plugin"] = ParseVersion("1.0.0");
            plugins["test.pre"] = PluginVersion("1.2.0-beta.1");
            plugins["test.build"] = PluginVersion("1.2.0+abc");
            plugins["Foo.Bar"] = ParseVersion("1.0");
            List<Candidate> found = new List<Candidate>();
            found.Add(Needs(Sample("p/a.dll", "test.a", "1.0"), "test.b", null));
            found.Add(Needs(Sample("p/b.dll", "test.b", "1.0"), "obenseuer.moddingframework", "0.11.0"));
            found.Add(Needs(Sample("p/c.dll", "test.c", "1.0"), "test.missing", null));
            found.Add(Needs(Sample("p/d.dll", "test.d", "1.0"), "test.e", null));
            found.Add(Needs(Sample("p/e.dll", "test.e", "1.0"), "test.d", null));
            found.Add(Needs(Sample("p/g.dll", "test.g", "1.0"), "test.plugin", "1.0.0.0"));
            found.Add(Needs(Sample("p/i.dll", "test.p1", "1.0"), "test.pre", "1.2.0"));
            found.Add(Needs(Sample("p/j.dll", "test.p2", "1.0"), "test.build", "1.2.0"));
            found.Add(Needs(Sample("p/k.dll", "test.p3", "1.0"), "test.pre", "1.2.1"));
            found.Add(Needs(Sample("p/o.dll", "test.k2", "1.0"), "test.pre", "1.2.0-beta"));
            found.Add(Sample("p/l.dll", "test.h", "1.2.0-beta"));
            found.Add(Sample("p/z.dll", "test.a", "2.0"));
            found.Add(Sample("p/v.dll", "test.v", "1"));
            found.Add(Sample("p/s.dll", "test.s", " 1.0 "));
            found.Add(Sample("p/q.dll", "test.q", "+1.0"));
            found.Add(Needs(Sample("p/m.dll", "test.m", "1.0"), "obenseuer.moddingframework", "99.0"));
            found.Add(Sample("p/x.dll", "bad id!", "1.0"));
            found.Add(Sample("p/p.dll", "obenseuer.moddingframework", "1.0"));
            found.Add(Sample("p/r.dll", "OBENSEUERCORE", "1.0"));
            found.Add(Sample("p/f.dll", "foo.bar", "1.0"));
            found.Add(Needs(Sample("p/u.dll", "test.u", "1.0"), null, null));
            found.Add(Sample("p/t.dll", "con", "1.0"));
            found.Add(Sample("p/w.dll", "obenseuer.moddingframework.hud", "1.0"));
            found.Add(Sample("p/y.dll", "obenseuer.moddingframeworkextra", "1.0"));
            Candidate unnamed = Sample("p/n.dll", "test.n", "1.0");
            unnamed.Name = null;
            found.Add(unnamed);
            List<string> warnings = new List<string>();
            PlanResult plan = Plan(found, plugins, warnings.Add);
            List<string> ids = new List<string>();
            foreach (Candidate candidate in plan.Ordered) ids.Add(candidate.Id);
            string order = string.Join(",", ids);
            bool kept = plan.Present.Count == 10 && Find(plan.Present, "test.c") != null && Find(plan.Ordered, "test.c") == null;
            bool ok = order == "test.b,test.a,test.g,test.p1,test.p2" && warnings.Count == 20 && kept;
            return "framework mod scan self-check " + (ok ? "ok" : "FAILED") + ": order " + order + ", " + warnings.Count + " skipped, " + plan.Present.Count + " present";
        }

        private static Candidate Sample(string path, string id, string version)
        {
            Candidate candidate = new Candidate();
            candidate.Path = path;
            candidate.TypeName = id + ".Mod";
            candidate.AssemblyName = id;
            candidate.Id = id;
            candidate.Name = id;
            candidate.VersionText = version;
            return candidate;
        }

        private static Candidate Needs(Candidate candidate, string id, string minVersion)
        {
            Dependency needed = new Dependency();
            needed.Id = id;
            needed.MinVersionText = minVersion;
            candidate.Dependencies.Add(needed);
            return candidate;
        }

        private static bool References(ModuleDefinition module)
        {
            foreach (AssemblyNameReference reference in module.AssemblyReferences)
            {
                if (reference.Name == FrameworkAssembly) return true;
            }
            return false;
        }

        private static List<CustomAttribute> Named(TypeDefinition type, string name)
        {
            List<CustomAttribute> attributes = new List<CustomAttribute>();
            foreach (CustomAttribute attribute in type.CustomAttributes)
            {
                if (attribute.AttributeType.FullName == name) attributes.Add(attribute);
            }
            return attributes;
        }

        private static string Text(CustomAttribute attribute, int index)
        {
            return attribute.ConstructorArguments.Count > index ? attribute.ConstructorArguments[index].Value as string : null;
        }

        private static string Problem(Candidate candidate, List<Candidate> valid, Dictionary<string, Version> plugins)
        {
            if (!ModIds.IsValid(candidate.Id)) return "invalid id '" + candidate.Id + "'";
            if (ModIds.IsReservedName(candidate.Id)) return "id " + candidate.Id + " is a Windows device name";
            if (ReservedId(candidate.Id)) return "id " + candidate.Id + " is reserved for the framework";
            if (string.IsNullOrWhiteSpace(candidate.Name)) return "name is empty";
            candidate.Parsed = ParseVersion(candidate.VersionText);
            if (candidate.Parsed == null) return "version '" + candidate.VersionText + "' is not 2 to 4 numbers";
            foreach (Dependency dependency in candidate.Dependencies)
            {
                if (!ModIds.IsValid(dependency.Id)) return "dependency '" + dependency.Id + "' is not a valid id";
                if (dependency.MinVersionText == null) continue;
                dependency.MinVersion = ParseVersion(dependency.MinVersionText);
                if (dependency.MinVersion == null) return "dependency " + dependency.Id + " has an invalid minimum version '" + dependency.MinVersionText + "'";
            }
            Candidate owner = Find(valid, candidate.Id);
            if (owner != null) return "id " + candidate.Id + " is already used by " + Where(owner);
            if (plugins.ContainsKey(candidate.Id)) return "id " + candidate.Id + " is already a BepInEx plugin";
            return null;
        }

        private static bool ReservedId(string id)
        {
            foreach (string reserved in Reserved)
            {
                if (id.StartsWith(reserved, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        private static void Prune(List<Candidate> valid, Dictionary<string, Version> plugins, Action<string> warn)
        {
            bool changed = true;
            while (changed)
            {
                changed = false;
                for (int i = valid.Count - 1; i >= 0; i--)
                {
                    string problem = Unmet(valid[i], valid, plugins);
                    if (problem == null) continue;
                    warn("skipped " + Where(valid[i]) + ": " + problem);
                    valid.RemoveAt(i);
                    changed = true;
                }
            }
        }

        private static string Unmet(Candidate candidate, List<Candidate> valid, Dictionary<string, Version> plugins)
        {
            foreach (Dependency dependency in candidate.Dependencies)
            {
                Candidate framework = Find(valid, dependency.Id);
                Version have = framework == null ? null : framework.Parsed;
                if (framework == null && (!plugins.TryGetValue(dependency.Id, out have) || have == null)) return "dependency " + dependency.Id + " is not loaded";
                if (dependency.MinVersion != null && have < dependency.MinVersion) return "dependency " + dependency.Id + " is " + have + ", needs " + dependency.MinVersionText + " or later";
            }
            return null;
        }

        private static List<Candidate> Order(List<Candidate> valid, Action<string> warn)
        {
            List<Candidate> pending = new List<Candidate>(valid);
            pending.Sort(ById);
            List<Candidate> ordered = new List<Candidate>();
            bool progress = true;
            while (pending.Count > 0 && progress)
            {
                progress = false;
                for (int i = 0; i < pending.Count; i++)
                {
                    if (!Ready(pending[i], valid, ordered)) continue;
                    ordered.Add(pending[i]);
                    pending.RemoveAt(i);
                    progress = true;
                    break;
                }
            }
            foreach (Candidate candidate in pending) warn("skipped " + Where(candidate) + ": its dependencies form a cycle, or depend on one");
            return ordered;
        }

        private static bool Ready(Candidate candidate, List<Candidate> valid, List<Candidate> ordered)
        {
            foreach (Dependency dependency in candidate.Dependencies)
            {
                if (Find(valid, dependency.Id) != null && Find(ordered, dependency.Id) == null) return false;
            }
            return true;
        }

        private static string Where(Candidate candidate)
        {
            return candidate.TypeName + " in " + candidate.Path;
        }

        private static int ById(Candidate a, Candidate b)
        {
            return string.CompareOrdinal(a.Id, b.Id);
        }

        private static int ByPathAndType(Candidate a, Candidate b)
        {
            int byPath = string.CompareOrdinal(a.Path, b.Path);
            return byPath != 0 ? byPath : string.CompareOrdinal(a.TypeName, b.TypeName);
        }
    }
}
