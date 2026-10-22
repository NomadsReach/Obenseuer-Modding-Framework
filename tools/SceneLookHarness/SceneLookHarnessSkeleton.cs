using System;
using System.Collections.Generic;
using ObenseuerCore;

namespace SceneLookHarness
{
    internal static class SceneLookHarnessSkeleton
    {
        private const string LookScene = "level15";
        private const string LookPath = "Content/mr_duck/Wasteland Hide/mr_duck";
        private const string DefaultTarget = "Ville";

        internal static int Run(string dataDir, string sceneArg, string lookArg, string targetArg)
        {
            SceneLookFiles files = new SceneLookFiles(dataDir);
            try
            {
                string scene = string.IsNullOrEmpty(sceneArg) ? LookScene : sceneArg;
                string path = string.IsNullOrEmpty(lookArg) ? LookPath : lookArg;
                string targetName = string.IsNullOrEmpty(targetArg) ? DefaultTarget : targetArg;
                string reason;
                SceneLookFileScan scan = SceneLookScan.ScanFile(files, scene);
                if (scan.Error != null)
                {
                    Console.WriteLine("scene " + scene + " is not readable: " + scan.Error);
                    return 1;
                }
                SceneLookFile file = files.Get(scene, out reason);
                if (file == null)
                {
                    Console.WriteLine("scene " + scene + " is not readable: " + reason);
                    return 1;
                }
                long lookRoot = LookRoot(file, scan.Roots, path, out reason);
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
                SceneLookRoot target = SceneLookHarnessBindPoses.Target(scan.Roots, targetName);
                if (target == null)
                {
                    Console.WriteLine("target " + targetName + " was not found in " + scene);
                    return 1;
                }
                Console.WriteLine("skeleton case: look {0} in {1} | target {2} (GameObject pid {3}) | rig {4} | avatar {5} | {6} names under the root", path, scene, target.Path, target.GameObject, target.Rig, target.Avatar, target.Hierarchy.Count);
                int failures = 0;
                failures += Exact(look, target);
                failures += Roles(look, target);
                failures += Unmapped(look, target);
                failures += Poses(files, file, look, target);
                Console.WriteLine();
                Console.WriteLine(failures == 0 ? "skeleton: PASS" : "skeleton: FAIL " + failures + " check(s)");
                return failures == 0 ? 0 : 1;
            }
            finally
            {
                files.Dispose();
            }
        }

        private static int Exact(SceneLookLook look, SceneLookRoot target)
        {
            Dictionary<string, string> roleMap = SceneLookRig.Roles(target.Hierarchy);
            SceneLookRigPlan plan = SceneLookRig.Skeleton(look, target.Hierarchy, roleMap);
            Console.WriteLine("case exact names (the role map comes from the target's own role bone names, which stands in offline for the avatar human description): path {0} | mapped {1} of {2} | by name {3} | by role {4} | unmatched {5}", Label(plan.Path), plan.Bones.Count, plan.LookBones, plan.NameMapped, plan.RoleMapped, plan.Unmatched.Count);
            if (plan.Reason != null) Console.WriteLine("  reason: {0}", plan.Reason);
            if (plan.Path != SceneLookRigPath.Skeleton || plan.Bones.Count != plan.LookBones)
            {
                Console.WriteLine("  check: FAIL every look bone should map on this target");
                return 1;
            }
            SceneLookRigPlan decided = SceneLookRig.Decide(look, target.Hierarchy, target.Name, target.Avatar);
            if (decided.Path != SceneLookRigPath.Name || !Same(Map(plan), Map(decided)))
            {
                Console.WriteLine("  check: FAIL the skeleton map differs from the name path map on this target");
                return 1;
            }
            Console.WriteLine("  check: PASS all {0} look bones map by exact name and agree with the name path map", plan.LookBones);
            return 0;
        }

        private static int Roles(SceneLookLook look, SceneLookRoot target)
        {
            SceneLookRigPlan exact = SceneLookRig.Skeleton(look, target.Hierarchy, SceneLookRig.Roles(target.Hierarchy));
            if (exact.Roles.Count == 0)
            {
                Console.WriteLine("case role fallback: FAIL the look covers no humanoid role");
                return 1;
            }
            HashSet<string> taken = new HashSet<string>();
            Dictionary<string, string> roleMap = new Dictionary<string, string>();
            foreach (SceneLookRigRole role in exact.Roles)
            {
                taken.Add(role.LookName);
                roleMap[role.HumanName] = "skeleton." + role.HumanName;
            }
            List<string> names = new List<string>();
            foreach (string name in target.Hierarchy)
            {
                if (!taken.Contains(name)) names.Add(name);
            }
            foreach (SceneLookRigRole role in exact.Roles) names.Add(roleMap[role.HumanName]);
            SceneLookRigPlan plan = SceneLookRig.Skeleton(look, names, roleMap);
            Console.WriteLine("case role fallback (the look's {0} role bones are removed from the target names): path {1} | mapped {2} of {3} | by name {4} | by role {5} | unmatched {6}", taken.Count, Label(plan.Path), plan.Bones.Count, plan.LookBones, plan.NameMapped, plan.RoleMapped, plan.Unmatched.Count);
            if (plan.Reason != null) Console.WriteLine("  reason: {0}", plan.Reason);
            int failures = 0;
            if (plan.Path != SceneLookRigPath.Skeleton)
            {
                Console.WriteLine("  check: FAIL the role map should complete the skeleton map");
                failures++;
            }
            if (plan.RoleMapped != taken.Count || plan.NameMapped != plan.LookBones - taken.Count || plan.Unmatched.Count != 0)
            {
                Console.WriteLine("  check: FAIL the role and name counts are not {0} by role and {1} by name", taken.Count, plan.LookBones - taken.Count);
                failures++;
            }
            if (failures == 0)
            {
                int wrong = 0;
                foreach (SceneLookRigBone bone in plan.Bones)
                {
                    string role = SceneLookRig.RoleOf(bone.LookName);
                    if (role == null) continue;
                    if (bone.TargetName != "skeleton." + role) wrong++;
                }
                if (wrong != 0)
                {
                    Console.WriteLine("  check: FAIL {0} role bones did not land on their role bone", wrong);
                    failures++;
                }
                else
                {
                    Console.WriteLine("  check: PASS the {0} role bones map to the body bone their humanoid role names", taken.Count);
                }
            }
            return failures;
        }

        private static int Unmapped(SceneLookLook look, SceneLookRoot target)
        {
            SceneLookRigPlan exact = SceneLookRig.Skeleton(look, target.Hierarchy, SceneLookRig.Roles(target.Hierarchy));
            Dictionary<string, string> roleMap = new Dictionary<string, string>();
            List<string> names = new List<string>();
            foreach (SceneLookRigRole role in exact.Roles)
            {
                roleMap[role.HumanName] = "skeleton." + role.HumanName;
                names.Add(roleMap[role.HumanName]);
            }
            SceneLookRigPlan plan = SceneLookRig.Skeleton(look, names, roleMap);
            Console.WriteLine("case role bones only (a body with no other matching name): path {0} | mapped {1} of {2} | by role {3} | unmatched {4}", Label(plan.Path), plan.Bones.Count, plan.LookBones, plan.RoleMapped, plan.Unmatched.Count);
            if (plan.Reason != null) Console.WriteLine("  reason: {0}", plan.Reason);
            if (plan.Path != SceneLookRigPath.Refuse || plan.Unmatched.Count != plan.LookBones - plan.RoleMapped)
            {
                Console.WriteLine("  check: FAIL the skeleton map should refuse the bones it cannot map");
                return 1;
            }
            if (plan.Reason == null || !plan.Reason.StartsWith("the look bones cannot all be mapped", StringComparison.Ordinal))
            {
                Console.WriteLine("  check: FAIL the refusal reason does not say which bones could not be mapped");
                return 1;
            }
            Console.WriteLine("  check: PASS the unmapped bones refuse the map with a reason");
            return 0;
        }

        private static int Poses(SceneLookFiles files, SceneLookFile file, SceneLookLook look, SceneLookRoot target)
        {
            Dictionary<string, SceneLookMatrix> rest = SceneLookHarnessBindPoses.TargetRest(files, file, target);
            Dictionary<string, string> map = Map(SceneLookRig.Skeleton(look, target.Hierarchy, SceneLookRig.Roles(target.Hierarchy)));
            Console.WriteLine("skeleton bind poses: target rest {0} bones | mapped look bones {1}", rest.Count, map.Count);
            return SceneLookHarnessPlacement.Run(look, map, rest, "skeleton path");
        }

        private static Dictionary<string, string> Map(SceneLookRigPlan plan)
        {
            Dictionary<string, string> map = new Dictionary<string, string>();
            foreach (SceneLookRigBone bone in plan.Bones) map[bone.LookName] = bone.TargetName;
            return map;
        }

        private static bool Same(Dictionary<string, string> first, Dictionary<string, string> second)
        {
            if (first.Count != second.Count) return false;
            foreach (KeyValuePair<string, string> pair in first)
            {
                string other;
                if (!second.TryGetValue(pair.Key, out other)) return false;
                if (other != pair.Value) return false;
            }
            return true;
        }

        private static string Label(SceneLookRigPath path)
        {
            if (path == SceneLookRigPath.Name) return "name";
            if (path == SceneLookRigPath.Retarget) return "retarget";
            if (path == SceneLookRigPath.Skeleton) return "skeleton";
            return "refuse";
        }

        private static long LookRoot(SceneLookFile file, List<SceneLookRoot> roots, string path, out string reason)
        {
            reason = null;
            foreach (SceneLookRoot root in roots)
            {
                if (root.Path == path) return root.GameObject;
            }
            string name = path;
            int slash = path.LastIndexOf('/');
            if (slash >= 0) name = path.Substring(slash + 1);
            foreach (long candidate in file.Order)
            {
                if (file.ClassId(candidate) != SceneLookClass.GameObject) continue;
                if (SceneLookReader.GameObject(file, candidate).Name != name) continue;
                if (SceneLookReader.PathName(file, candidate) == path) return candidate;
            }
            reason = "no GameObject has that path";
            return 0;
        }
    }
}