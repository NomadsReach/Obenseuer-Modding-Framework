using System;
using System.Collections.Generic;

namespace ObenseuerCore
{
    internal enum SceneLookRigPath
    {
        Refuse,
        Name,
        Retarget
    }

    internal sealed class SceneLookRigBone
    {
        public string LookName;
        public string TargetName;
        public bool Alias;
    }

    internal sealed class SceneLookRigRole
    {
        public string HumanName;
        public string LookName;
    }

    internal sealed class SceneLookRigPlan
    {
        public SceneLookRigPath Path = SceneLookRigPath.Refuse;
        public string Reason;
        public List<SceneLookRigBone> Bones = new List<SceneLookRigBone>();
        public List<SceneLookRigRole> Roles = new List<SceneLookRigRole>();
        public List<string> Unmatched = new List<string>();
        public List<string> Aliases = new List<string>();
        public List<string> Covered = new List<string>();
        public List<string> Missing = new List<string>();
        public List<string> Duplicates = new List<string>();
        public int LookBones;
        public int LookSlots;
        public int InSkeleton;
        public int RequiredBones = 15;
        public int RequiredCovered;
    }

    internal static class SceneLookRig
    {
        internal const string HeadAliasFrom = "mr_duck_head";
        internal const string HeadAliasTo = "Bip001 Head";
        internal const string RootAlias = "Bip001_root";
        internal const string RootBone = "Bip001 Pelvis";
        internal const int RequiredRoles = 15;

        private static readonly string[] Required = { "Hips", "Spine", "Head", "LeftUpperLeg", "LeftLowerLeg", "LeftFoot", "RightUpperLeg", "RightLowerLeg", "RightFoot", "LeftUpperArm", "LeftLowerArm", "LeftHand", "RightUpperArm", "RightLowerArm", "RightHand" };

        private static readonly string[][] RoleTable = {
            new[] { "Hips", "Bip001 Pelvis" },
            new[] { "Spine", "Bip001 Spine" },
            new[] { "Chest", "Bip001 Spine1" },
            new[] { "Neck", "Bip001 Neck" },
            new[] { "Head", "mr_duck_head", "Bip001 Head" },
            new[] { "LeftUpperLeg", "Bip001 L Thigh" },
            new[] { "LeftLowerLeg", "Bip001 L Calf" },
            new[] { "LeftFoot", "Bip001 L Foot" },
            new[] { "RightUpperLeg", "Bip001 R Thigh" },
            new[] { "RightLowerLeg", "Bip001 R Calf" },
            new[] { "RightFoot", "Bip001 R Foot" },
            new[] { "LeftShoulder", "Bip001 L Clavicle" },
            new[] { "LeftUpperArm", "Bip001 L UpperArm" },
            new[] { "LeftLowerArm", "Bip001 L Forearm" },
            new[] { "LeftHand", "Bip001 L Hand" },
            new[] { "RightShoulder", "Bip001 R Clavicle" },
            new[] { "RightUpperArm", "Bip001 R UpperArm" },
            new[] { "RightLowerArm", "Bip001 R Forearm" },
            new[] { "RightHand", "Bip001 R Hand" }
        };

        internal static SceneLookRigPlan Decide(SceneLookLook look, ICollection<string> targetNames, string targetRootName, string avatarKind)
        {
            SceneLookRigPlan plan = new SceneLookRigPlan();
            plan.RequiredBones = Required.Length;
            if (look == null)
            {
                plan.Reason = "no look";
                return plan;
            }
            if (targetNames == null || targetNames.Count == 0)
            {
                plan.Reason = "the target root has no names";
                return plan;
            }
            if (targetRootName == null) targetRootName = string.Empty;
            HashSet<string> targets = new HashSet<string>(targetNames);
            HashSet<string> names = new HashSet<string>();
            List<string> order = new List<string>();
            HashSet<long> nodes = new HashSet<long>();
            if (look.Nodes != null)
            {
                foreach (SceneLookNode node in look.Nodes) nodes.Add(node.Transform);
            }
            foreach (SceneLookLookRenderer renderer in look.Renderers)
            {
                foreach (SceneLookBone bone in renderer.Bones)
                {
                    plan.LookSlots++;
                    if (nodes.Contains(bone.Transform.PathId)) plan.InSkeleton++;
                    if (names.Add(bone.Name)) order.Add(bone.Name);
                }
            }
            plan.LookBones = order.Count;
            foreach (string name in order)
            {
                string target = name;
                bool alias = false;
                if (!targets.Contains(target))
                {
                    target = Alias(name, targetRootName);
                    alias = target != name;
                }
                if (targets.Contains(target))
                {
                    SceneLookRigBone bone = new SceneLookRigBone();
                    bone.LookName = name;
                    bone.TargetName = target;
                    bone.Alias = alias;
                    plan.Bones.Add(bone);
                    if (bone.Alias) plan.Aliases.Add(name + " -> " + target);
                }
                else
                {
                    plan.Unmatched.Add(name);
                }
            }
            foreach (string[] role in RoleTable)
            {
                string look0 = Match(role, names);
                if (look0 != null)
                {
                    SceneLookRigRole mapped = new SceneLookRigRole();
                    mapped.HumanName = role[0];
                    mapped.LookName = look0;
                    plan.Roles.Add(mapped);
                    plan.Covered.Add(role[0]);
                }
                else if (Array.IndexOf(Required, role[0]) >= 0)
                {
                    plan.Missing.Add(role[0]);
                }
            }
            plan.RequiredCovered = Required.Length - plan.Missing.Count;
            if (plan.Unmatched.Count == 0)
            {
                plan.Path = SceneLookRigPath.Name;
                return plan;
            }
            if (avatarKind == "humanoid")
            {
                if (plan.InSkeleton < plan.LookSlots)
                {
                    plan.Reason = "bone outside the look skeleton";
                    return plan;
                }
                if (plan.RequiredCovered == Required.Length)
                {
                    Duplicates(look, plan);
                    if (plan.Duplicates.Count > 0)
                    {
                        plan.Reason = "the look skeleton has duplicate object names: " + string.Join(", ", plan.Duplicates.ToArray());
                        return plan;
                    }
                    plan.Path = SceneLookRigPath.Retarget;
                    return plan;
                }
                plan.Reason = "no rig path: the look covers " + plan.RequiredCovered + " of the 15 humanoid roles";
                return plan;
            }
            plan.Reason = "target is not humanoid and names do not match";
            return plan;
        }

        internal static string Alias(string name, string targetRootName)
        {
            if (name == HeadAliasFrom) return HeadAliasTo;
            if (name == RootAlias) return targetRootName;
            return name;
        }

        internal static bool RequiredRole(string humanName)
        {
            return Array.IndexOf(Required, humanName) >= 0;
        }

        private static string Match(string[] role, HashSet<string> names)
        {
            for (int i = 1; i < role.Length; i++)
            {
                if (names.Contains(role[i])) return role[i];
            }
            return null;
        }

        private static void Duplicates(SceneLookLook look, SceneLookRigPlan plan)
        {
            HashSet<string> seen = new HashSet<string>();
            if (look.Nodes == null) return;
            foreach (SceneLookNode node in look.Nodes)
            {
                if (seen.Add(node.Name)) continue;
                if (!plan.Duplicates.Contains(node.Name)) plan.Duplicates.Add(node.Name);
            }
        }
    }
}
