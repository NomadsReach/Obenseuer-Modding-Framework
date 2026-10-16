using System;
using System.Collections.Generic;
using ObenseuerCore;

namespace SceneLookHarness
{
    internal static class SceneLookHarnessApplyPlan
    {
        private const string LookScene = "level15";
        private const string LookPath = "Content/mr_duck/Wasteland Hide/mr_duck";

        internal static int Run(string dataDir, string sceneArg, string lookArg, string targetArg)
        {
            SceneLookFiles files = new SceneLookFiles(dataDir);
            try
            {
                string scene = string.IsNullOrEmpty(sceneArg) ? LookScene : sceneArg;
                string path = string.IsNullOrEmpty(lookArg) ? LookPath : lookArg;
                string reason;
                List<SceneLookRoot> roots = Roots(files, scene, out reason);
                if (roots == null)
                {
                    Console.WriteLine("scene " + scene + " is not readable: " + reason);
                    return 1;
                }
                long lookRoot = FindRoot(files, scene, roots, path, out reason);
                if (lookRoot == 0)
                {
                    Console.WriteLine("look root " + path + " was not found in " + scene + ": " + reason);
                    return 1;
                }
                SceneLookLook look = SceneLookLoad.Load(files, scene, lookRoot, out reason);
                if (look == null)
                {
                    Console.WriteLine("look " + path + " was refused: " + reason);
                    return 1;
                }
                Console.WriteLine("look: {0} {1} (GameObject pid {2}) | {3} renderers | {4} nodes", scene, path, lookRoot, look.Renderers.Count, look.Nodes.Count);
                int failures = Layout(look);
                if (!string.IsNullOrEmpty(targetArg))
                {
                    SceneLookRoot target = FindTarget(roots, targetArg, true);
                    if (target == null)
                    {
                        Console.WriteLine("target " + targetArg + " was not found in " + scene);
                        return 1;
                    }
                    failures += Case(files, look, target, null);
                }
                else
                {
                    failures += Named(files, roots, look, "Ville", "name");
                    failures += Named(files, roots, look, "Pentti", "retarget");
                    failures += Generic(dataDir, files, look);
                }
                Console.WriteLine();
                Console.WriteLine(failures == 0 ? "applyplan: PASS" : "applyplan: FAIL " + failures + " case(s)");
                return failures == 0 ? 0 : 1;
            }
            finally
            {
                files.Dispose();
            }
        }

        private static int Layout(SceneLookLook look)
        {
            int bad = 0;
            foreach (SceneLookLookRenderer renderer in look.Renderers)
            {
                string reason;
                SceneLookMeshPlan.Plan(renderer.Mesh, out reason);
                if (reason == null) continue;
                Console.WriteLine("  mesh layout: {0} refused: {1}", renderer.Mesh.Name, reason);
                bad++;
            }
            Console.WriteLine("mesh layout: {0} of {1} look meshes match the implicit channel order and their vertex byte rule", look.Renderers.Count - bad, look.Renderers.Count);
            return bad;
        }

        private static int Named(SceneLookFiles files, List<SceneLookRoot> roots, SceneLookLook look, string prefix, string expect)        {
            SceneLookRoot target = FindTarget(roots, prefix, false);
            if (target == null)
            {
                Console.WriteLine();
                Console.WriteLine("case " + prefix + ": no target root was found, expected " + expect);
                return 1;
            }
            return Case(files, look, target, expect);
        }

        private static int Generic(string dataDir, SceneLookFiles files, SceneLookLook look)
        {
            List<string> names = SceneLookHarnessScan.FileNames(dataDir);
            for (int i = 0; i < names.Count; i++)
            {
                string name = names[i];
                if (!name.StartsWith("level", StringComparison.Ordinal)) continue;
                string reason;
                List<SceneLookRoot> roots = Roots(files, name, out reason);
                if (roots == null) continue;
                foreach (SceneLookRoot root in roots)
                {
                    if (root.Rig != "Blender-style") continue;
                    if (root.Avatar != "generic") continue;
                    if (!root.IsNpc) continue;
                    if (root.Renderers == 0 || root.BoneNames.Count == 0) continue;
                    Console.WriteLine();
                    Console.WriteLine("case generic Blender-style: first found in " + name);
                    return Case(files, look, root, "refuse");
                }
            }
            Console.WriteLine();
            Console.WriteLine("case generic Blender-style: no generic Blender-style NPC root was found");
            return 1;
        }

        private static int Case(SceneLookFiles files, SceneLookLook look, SceneLookRoot target, string expect)
        {
            Console.WriteLine();
            if (expect != null) Console.WriteLine("case: expected {0}", expect);
            Console.WriteLine("  target root: {0} (GameObject pid {1}) | rig {2} | avatar {3} | {4} names under the root | {5} renderers | {6} skeleton bone names", target.Path, target.GameObject, target.Rig, target.Avatar, target.Hierarchy.Count, target.Renderers, target.BoneNames.Count);
            SceneLookRigPlan plan = SceneLookRig.Decide(look, target.Hierarchy, target.Name, target.Avatar);
            Console.WriteLine("  look: {0} renderers | {1} bone names | {2} bone slots | {3} of {2} in the look skeleton", look.Renderers.Count, plan.LookBones, plan.LookSlots, plan.InSkeleton);
            Console.WriteLine("  decision: {0}", Label(plan.Path));
            if (plan.Reason != null) Console.WriteLine("  reason: {0}", plan.Reason);
            Console.WriteLine("  aliases used: {0}", plan.Aliases.Count == 0 ? "(none)" : string.Join("; ", plan.Aliases.ToArray()));
            Console.WriteLine("  bones mapped: {0} of {1}", plan.Bones.Count, plan.LookBones);
            Console.WriteLine("  unmatched bones ({0}): {1}", plan.Unmatched.Count, plan.Unmatched.Count == 0 ? "(none)" : string.Join(", ", plan.Unmatched.ToArray()));
            Console.WriteLine("  required roles covered: {0} of {1} ({2})", plan.RequiredCovered, plan.RequiredBones, string.Join(", ", plan.Covered.ToArray()));
            Console.WriteLine("  required roles missing: {0}", plan.Missing.Count == 0 ? "(none)" : string.Join(", ", plan.Missing.ToArray()));
            if (plan.Duplicates.Count > 0) Console.WriteLine("  duplicate look object names: {0}", string.Join(", ", plan.Duplicates.ToArray()));
            if (expect == null) return 0;
            bool ok = Label(plan.Path) == expect;
            Console.WriteLine("  check: {0}", ok ? "PASS" : "FAIL (expected " + expect + ")");
            return ok ? 0 : 1;
        }

        private static string Label(SceneLookRigPath path)
        {
            if (path == SceneLookRigPath.Name) return "name";
            if (path == SceneLookRigPath.Retarget) return "retarget";
            return "refuse";
        }

        private static List<SceneLookRoot> Roots(SceneLookFiles files, string scene, out string reason)
        {
            reason = null;
            SceneLookFileScan scan = SceneLookScan.ScanFile(files, scene);
            if (scan.Error != null)
            {
                reason = scan.Error;
                return null;
            }
            return scan.Roots;
        }

        private static long FindRoot(SceneLookFiles files, string scene, List<SceneLookRoot> roots, string path, out string reason)
        {
            reason = null;
            foreach (SceneLookRoot root in roots)
            {
                if (root.Path == path) return root.GameObject;
            }
            string name = path;
            int slash = path.LastIndexOf('/');
            if (slash >= 0) name = path.Substring(slash + 1);
            SceneLookFile file = files.Get(scene, out reason);
            if (file == null) return 0;
            foreach (long candidate in file.Order)
            {
                if (file.ClassId(candidate) != SceneLookClass.GameObject) continue;
                if (SceneLookReader.GameObject(file, candidate).Name != name) continue;
                if (SceneLookReader.PathName(file, candidate) == path) return candidate;
            }
            reason = "no GameObject has that path";
            return 0;
        }

        private static SceneLookRoot FindTarget(List<SceneLookRoot> roots, string wanted, bool exact)
        {
            foreach (SceneLookRoot root in roots)
            {
                if (root.Renderers == 0) continue;
                if (exact ? root.Name == wanted : root.Name.StartsWith(wanted, StringComparison.OrdinalIgnoreCase)) return root;
            }
            return null;
        }
    }
}
