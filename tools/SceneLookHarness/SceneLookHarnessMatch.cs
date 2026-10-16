using System;
using System.Collections.Generic;
using System.Linq;
using ObenseuerCore;

namespace SceneLookHarness
{
    internal static class SceneLookHarnessMatch
    {
        internal static int Run(string dataDir)
        {
            List<SceneLookFileScan> recs = SceneLookHarnessScan.ScanAll(dataDir);
            SceneLookRoot duck = recs.Where(rec => rec.FileName == "level15").SelectMany(rec => rec.Roots).FirstOrDefault(root => root.Name == "mr_duck");
            if (duck == null)
            {
                Console.WriteLine("no mr_duck body in level15");
                return 1;
            }
            HashSet<string> bones = new HashSet<string>(duck.BoneNames);
            SceneLookCounter<string> classes = new SceneLookCounter<string>();
            SceneLookCounter<string> byAvatar = new SceneLookCounter<string>();
            SceneLookCounter<string> missingNo = new SceneLookCounter<string>();
            Dictionary<string, HashSet<string>> characters = new Dictionary<string, HashSet<string>>();
            int humanoidNo = 0;
            int otherNo = 0;
            foreach (SceneLookFileScan rec in recs)
            {
                foreach (SceneLookRoot root in rec.Roots)
                {
                    if (!root.IsNpc) continue;
                    List<string> missing = Missing(bones, root.Hierarchy);
                    string kind = missing.Count == 0 ? "exact" : "no";
                    classes.Add(kind);
                    byAvatar.Add(kind + "|" + root.Avatar);
                    if (kind == "no")
                    {
                        missingNo.Add("('" + root.Rig + "', " + missing.Count + ")");
                        if (root.Avatar == "humanoid") humanoidNo++;
                        else otherNo++;
                    }
                    if (root.Character != null)
                    {
                        string key = root.Character.Id + "|" + root.Character.Name;
                        HashSet<string> seen;
                        if (!characters.TryGetValue(key, out seen))
                        {
                            seen = new HashSet<string>();
                            characters[key] = seen;
                        }
                        seen.Add(kind);
                    }
                }
            }
            SceneLookCounter<string> sorted = new SceneLookCounter<string>();
            foreach (string key in byAvatar.Keys.OrderBy(k => k.Split('|')[0], StringComparer.Ordinal).ThenBy(k => k.Split('|')[1], StringComparer.Ordinal)) sorted.Add(key, byAvatar[key]);
            int allExact = characters.Values.Count(seen => seen.Count == 1 && seen.Contains("exact"));
            int someExact = characters.Values.Count(seen => seen.Contains("exact") && seen.Count > 1);
            int neverExact = characters.Values.Count(seen => !seen.Contains("exact"));
            Console.WriteLine("Mr. Duck look bones {0} | NPC bodies by name match (2 aliases): {1}", bones.Count, Py.Dict(classes));
            Console.WriteLine("  by avatar: " + Py.Dict(sorted));
            Console.WriteLine("  no-match bodies (rig, missing count): " + Py.DictRaw(missingNo));
            Console.WriteLine("distinct Character IDs: {0} | exact for all instances: {1} | exact for some: {2} | never exact: {3}", characters.Count, allExact, someExact, neverExact);
            Console.WriteLine("retarget candidates (no name match, humanoid target): {0} | refuse (no name match, non-humanoid target): {1}", humanoidNo, otherNo);
            return 0;
        }

        private static List<string> Missing(HashSet<string> bones, IEnumerable<string> hierarchy)
        {
            HashSet<string> names = new HashSet<string>(hierarchy);
            if (names.Contains("Bip001 Head")) names.Add("mr_duck_head");
            names.Add("Bip001_root");
            return bones.Where(bone => !names.Contains(bone)).ToList();
        }
    }
}
