using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ObenseuerCore;

namespace SceneLookHarness
{
    internal static class SceneLookHarnessScan
    {
        private sealed class Category
        {
            public int Files;
            public double Secs;
            public int MaxMillis;
            public int Smr;
            public int CleanSmr;
            public int Unrooted;
            public int Groups;
            public int NpcGroups;
            public int NpcClean;
            public int AnimGroups;
            public int NpcObjects;
            public int NpcChar;
        }

        internal static int Run(string dataDir)
        {
            Stopwatch wall = Stopwatch.StartNew();
            List<string> names = SceneNames(dataDir);
            List<SceneLookFileScan> recs = ScanAll(dataDir);
            Print(names, recs);
            Console.WriteLine("wall time: {0:F2} s for {1} files", wall.Elapsed.TotalSeconds, recs.Count);
            return 0;
        }

        internal static List<SceneLookFileScan> ScanAll(string dataDir)
        {
            List<SceneLookFileScan> recs = new List<SceneLookFileScan>();
            SceneLookFiles files = new SceneLookFiles(dataDir);
            try
            {
                foreach (string name in FileNames(dataDir)) recs.Add(SceneLookScan.ScanFile(files, name));
            }
            finally
            {
                files.Dispose();
            }
            return recs;
        }

        internal static List<string> FileNames(string dataDir)
        {
            List<string> levels = new List<string>();
            List<string> shared = new List<string>();
            foreach (string path in Directory.GetFiles(dataDir))
            {
                string name = Path.GetFileName(path);
                if (Regex.IsMatch(name, "^level[0-9]+$")) levels.Add(name);
                else if (Regex.IsMatch(name, "^sharedassets[0-9]+\\.assets$")) shared.Add(name);
            }
            levels.Sort((a, b) => IndexOf(a).CompareTo(IndexOf(b)));
            shared.Sort((a, b) => IndexOf(a).CompareTo(IndexOf(b)));
            List<string> names = new List<string>(levels);
            names.AddRange(shared);
            if (File.Exists(Path.Combine(dataDir, "resources.assets"))) names.Add("resources.assets");
            return names;
        }

        internal static List<string> SceneNames(string dataDir)
        {
            string text = Encoding.GetEncoding("ISO-8859-1").GetString(File.ReadAllBytes(Path.Combine(dataDir, "globalgamemanagers")));
            List<string> paths = new List<string>();
            foreach (Match match in Regex.Matches(text, "Assets/[\\x20-\\x7e]+?\\.unity"))
            {
                if (!paths.Contains(match.Value)) paths.Add(match.Value);
            }
            List<string> names = new List<string>();
            foreach (string path in paths)
            {
                string file = path.Substring(path.LastIndexOf('/') + 1);
                names.Add(file.Substring(0, file.Length - ".unity".Length));
            }
            return names;
        }

        internal static bool Linked(SceneLookRoot root)
        {
            return root.Character != null || !string.IsNullOrEmpty(root.CharacterNote);
        }

        internal static int IndexOf(string name)
        {
            return int.Parse(Regex.Match(name, "[0-9]+").Value);
        }

        internal static List<T> MostCommon<T>(SceneLookCounter<T> counter)
        {
            return counter.Keys.OrderByDescending(key => counter[key]).ToList();
        }

        internal static string Json(string text)
        {
            StringBuilder builder = new StringBuilder("\"");
            foreach (char c in text)
            {
                if (c == '"' || c == '\\') builder.Append('\\').Append(c);
                else if (c < 0x20 || c > 0x7e) builder.Append("\\u").Append(((int)c).ToString("x4"));
                else builder.Append(c);
            }
            return builder.Append('"').ToString();
        }

        private static void Print(List<string> names, List<SceneLookFileScan> recs)
        {
            List<string> errors = new List<string>();
            foreach (SceneLookFileScan rec in recs)
            {
                if (rec.Error != null) errors.Add(rec.FileName);
            }
            Console.WriteLine("build scenes: {0} | files in scan: {1} | errors: {2}", names.Count, recs.Count, Py.Items(errors.Select(Py.Str)));
            List<SceneLookFileScan> good = recs.Where(rec => rec.Error == null).ToList();
            PrintCategories(good);
            PrintReasons(good);
            PrintRigs(good);
            PrintFormats(good);
            PrintShaders(good);
            PrintLevels(names, good);
        }

        private static void PrintCategories(List<SceneLookFileScan> good)
        {
            List<string> order = new List<string>();
            Dictionary<string, Category> cats = new Dictionary<string, Category>();
            foreach (SceneLookFileScan rec in good)
            {
                string name = rec.FileName.StartsWith("level", StringComparison.Ordinal) ? "levels" : rec.FileName.StartsWith("sharedassets", StringComparison.Ordinal) ? "sharedassets" : "resources";
                Category t;
                if (!cats.TryGetValue(name, out t))
                {
                    t = new Category();
                    cats[name] = t;
                    order.Add(name);
                }
                t.Files++;
                t.Secs += rec.Seconds;
                t.MaxMillis = Math.Max(t.MaxMillis, (int)(rec.Seconds * 1000));
                t.Smr += rec.Renderers;
                t.CleanSmr += rec.CleanRenderers;
                t.Unrooted += rec.Unrooted;
                t.Groups += rec.Roots.Count;
                t.NpcGroups += rec.Roots.Count(g => g.IsNpc);
                t.NpcClean += rec.Roots.Count(g => g.IsNpc && g.Clean);
                t.AnimGroups += rec.Roots.Count(g => !g.IsNpc);
                t.NpcObjects += rec.Npcs.Count;
                t.NpcChar += rec.Npcs.Count(n => n.Character != null);
            }
            foreach (string name in order)
            {
                Category t = cats[name];
                Console.WriteLine("{0,-14} files {1,3} secs {2,8:F1} max_file_secs {3,6:F2} smr {4,6} clean_smr {5,6} unrooted {6,6} groups {7,5} npc_groups {8,4} npc_clean {9,4} anim_groups {10,4} npc_objects {11,4} npc_char {12,4}", name, t.Files, t.Secs, t.MaxMillis / 1000.0, t.Smr, t.CleanSmr, t.Unrooted, t.Groups, t.NpcGroups, t.NpcClean, t.AnimGroups, t.NpcObjects, t.NpcChar);
            }
        }

        private static void PrintReasons(List<SceneLookFileScan> good)
        {
            SceneLookCounter<string> reasons = new SceneLookCounter<string>();
            foreach (SceneLookFileScan rec in good)
            {
                foreach (string key in rec.ReasonCounts.Keys) reasons.Add(key, rec.ReasonCounts[key]);
            }
            Console.WriteLine("failure reasons (SMRs per reason):");
            foreach (string key in MostCommon(reasons)) Console.WriteLine("  {0,5} {1}", reasons[key], key);
        }

        private static void PrintRigs(List<SceneLookFileScan> good)
        {
            foreach (bool npc in new[] { true, false })
            {
                SceneLookCounter<string> rig = new SceneLookCounter<string>();
                SceneLookCounter<string> avatars = new SceneLookCounter<string>();
                foreach (SceneLookFileScan rec in good)
                {
                    foreach (SceneLookRoot root in rec.Roots)
                    {
                        if (root.IsNpc != npc) continue;
                        rig.Add(root.Rig);
                        avatars.Add(root.Avatar);
                    }
                }
                Console.WriteLine("{0} rig kinds {1} | avatar kinds {2}", npc ? "NPC bodies" : "animated non-NPC roots", Py.Dict(rig), Py.Dict(avatars));
            }
            SceneLookCounter<string> unrooted = new SceneLookCounter<string>();
            foreach (SceneLookFileScan rec in good)
            {
                foreach (string key in rec.UnrootedRigs.Keys) unrooted.Add(key, rec.UnrootedRigs[key]);
            }
            Console.WriteLine("unrooted renderers rig kinds " + Py.Dict(unrooted));
        }

        private static void PrintFormats(List<SceneLookFileScan> good)
        {
            SceneLookCounter<int> objects = new SceneLookCounter<int>();
            SceneLookCounter<int> used = new SceneLookCounter<int>();
            foreach (SceneLookFileScan rec in good)
            {
                foreach (int key in rec.TextureFormats.Keys) objects.Add(key, rec.TextureFormats[key]);
                foreach (int key in rec.FormatsUsed.Keys) used.Add(key, rec.FormatsUsed[key]);
            }
            Console.WriteLine("Texture2D objects by format (all scanned files):");
            foreach (int key in objects.Keys.OrderBy(k => k)) Console.WriteLine("  {0,7} {1}", objects[key], Program.FormatName(key));
            Console.WriteLine("texture formats used by renderers: " + string.Join("; ", used.Keys.OrderBy(k => k).Select(k => Program.FormatName(k) + " x" + used[k])));
        }

        private static void PrintShaders(List<SceneLookFileScan> good)
        {
            SceneLookCounter<string> objects = new SceneLookCounter<string>();
            SceneLookCounter<string> used = new SceneLookCounter<string>();
            foreach (SceneLookFileScan rec in good)
            {
                foreach (string key in rec.ShaderObjects.Keys) objects.Add(key, rec.ShaderObjects[key]);
                foreach (string key in rec.ShadersUsed.Keys) used.Add(key, rec.ShadersUsed[key]);
            }
            Console.WriteLine("Shader objects by name (all scanned files):");
            foreach (string key in MostCommon(objects)) Console.WriteLine("  {0,7} {1}", objects[key], key);
            Console.WriteLine("shaders used by renderer materials: " + string.Join("; ", MostCommon(used).Select(k => k + " x" + used[k])));
        }

        private static void PrintLevels(List<string> names, List<SceneLookFileScan> good)
        {
            Console.WriteLine("per-level rows with NPC bodies: index name secs smr clean npc_groups npc_clean rigs avatars linked_characters");
            foreach (SceneLookFileScan rec in good)
            {
                if (!rec.FileName.StartsWith("level", StringComparison.Ordinal)) continue;
                List<SceneLookRoot> npcs = rec.Roots.Where(g => g.IsNpc).ToList();
                if (npcs.Count == 0) continue;
                int index = IndexOf(rec.FileName);
                SceneLookCounter<string> rigs = new SceneLookCounter<string>();
                SceneLookCounter<string> avatars = new SceneLookCounter<string>();
                foreach (SceneLookRoot root in npcs)
                {
                    rigs.Add(root.Rig);
                    avatars.Add(root.Avatar);
                }
                string name = index < names.Count ? names[index] : "?";
                if (name.Length > 40) name = name.Substring(0, 40);
                Console.WriteLine("  {0,3} {1,-40} {2,6:F2}s smr {3,4} clean {4,4} npc {5,3} clean {6,3} {7} {8} char {9}", index, name, rec.Seconds, rec.Renderers, rec.CleanRenderers, npcs.Count, npcs.Count(g => g.Clean), Py.Dict(rigs), Py.Dict(avatars), npcs.Count(Linked));
            }
            foreach (SceneLookFileScan rec in good)
            {
                if (rec.FileName != "level15") continue;
                foreach (SceneLookRoot root in rec.Roots)
                {
                    if (root.Name == "mr_duck") Console.WriteLine("level15 mr_duck: " + DuckJson(root));
                }
                List<string> items = new List<string>();
                foreach (SceneLookNpc npc in rec.Npcs)
                {
                    string character = npc.Character != null ? "[" + npc.Character.Id + ", " + Py.Str(npc.Character.Name) + "]" : npc.CharacterNote != null ? Py.Str(npc.CharacterNote) : "None";
                    items.Add("(" + Py.Str(npc.Name) + ", " + character + ", " + (npc.Body == 0 ? "None" : npc.Body.ToString()) + ")");
                }
                Console.WriteLine("level15 NPC objects: " + Py.Items(items));
            }
        }

        private static string DuckJson(SceneLookRoot root)
        {
            List<string> infos = new List<string>();
            foreach (SceneLookAvatarInfo info in root.Avatars)
            {
                infos.Add("[" + Json(info.Kind) + ", " + info.Roles + ", " + (info.ControllerName == null ? "null" : Json(info.ControllerName)) + "]");
            }
            List<string> fields = new List<string>();
            fields.Add("\"path\": " + Json(root.Path));
            fields.Add("\"renderers\": " + root.Renderers);
            fields.Add("\"bones\": " + root.BoneNames.Count);
            fields.Add("\"rig\": " + Json(root.Rig));
            fields.Add("\"vertices\": " + root.Vertices);
            fields.Add("\"triangles\": " + root.Triangles);
            fields.Add("\"avatar\": " + Json(root.Avatar));
            fields.Add("\"avatar_info\": [" + string.Join(", ", infos) + "]");
            fields.Add("\"clean\": " + (root.Clean ? "true" : "false"));
            fields.Add("\"reasons\": [" + string.Join(", ", root.Reasons.Select(Json)) + "]");
            fields.Add("\"fmts\": [" + string.Join(", ", root.Formats) + "]");
            fields.Add("\"shaders\": [" + string.Join(", ", root.Shaders.Select(Json)) + "]");
            fields.Add("\"npc\": " + (root.IsNpc ? "true" : "false"));
            return "{" + string.Join(", ", fields) + "}";
        }
    }
}
