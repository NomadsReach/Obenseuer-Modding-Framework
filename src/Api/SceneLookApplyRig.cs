using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace ObenseuerCore
{
    internal static class SceneLookApplyRig
    {
        private const int MaxDepth = 256;

        internal static List<string> Names(GameObject body)
        {
            List<string> names = new List<string>();
            Transform[] transforms = body.GetComponentsInChildren<Transform>(true);
            foreach (Transform transform in transforms) names.Add(transform.name);
            return names;
        }

        internal static string AvatarKind(GameObject body)
        {
            if (HumanAvatar(body) != null) return "humanoid";
            Animator[] animators = body.GetComponentsInChildren<Animator>(true);
            foreach (Animator animator in animators)
            {
                if (animator.avatar != null) return "generic";
            }
            return "none";
        }

        internal static Avatar HumanAvatar(GameObject body)
        {
            Animator animator = HumanAnimator(body);
            return animator != null ? animator.avatar : null;
        }

        internal static Animator HumanAnimator(GameObject body)
        {
            Animator[] animators = body.GetComponentsInChildren<Animator>(true);
            foreach (Animator animator in animators)
            {
                Avatar avatar = animator.avatar;
                if (avatar != null && avatar.isHuman) return animator;
            }
            return null;
        }

        internal static Transform Find(Transform root, string name)
        {
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = Find(root.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }

        internal static bool Bind(SceneLookLook look, GameObject body, SceneLookRigPlan plan, SceneLookAssets assets, SceneLookApplied state, out string reason)
        {
            reason = null;
            Transform root = body.transform;
            Dictionary<string, string> map = new Dictionary<string, string>();
            foreach (SceneLookRigBone bone in plan.Bones) map[bone.LookName] = bone.TargetName;
            List<Transform[]> resolved = new List<Transform[]>(look.Renderers.Count);
            foreach (SceneLookLookRenderer renderer in look.Renderers)
            {
                List<Transform> bones = new List<Transform>(renderer.Bones.Count);
                foreach (SceneLookBone bone in renderer.Bones)
                {
                    string name;
                    if (!map.TryGetValue(bone.Name, out name))
                    {
                        reason = "bone " + bone.Name + " has no target name";
                        return false;
                    }
                    Transform found = Find(root, name);
                    if (found == null)
                    {
                        reason = "bone " + name + " is not under the body root";
                        return false;
                    }
                    bones.Add(found);
                }
                resolved.Add(bones.ToArray());
            }
            Dictionary<string, SceneLookMatrix> rest = TargetRest(body);
            GameObject holder = Holder(look, body, state);
            SceneLookMatrix frame = SceneLookBind.BindFrame(SceneLookApplyMesh.ToSceneLookMatrix(body.transform.localToWorldMatrix), SceneLookApplyMesh.ToSceneLookMatrix(holder.transform.localToWorldMatrix));
            Dictionary<long, SceneLookNode> byTransform = SceneLookFrame.ByTransform(look);
            Transform rootBone = Find(root, SceneLookRig.RootBone);
            if (rootBone == null) rootBone = root;
            Material target = TargetMaterial(body);
            for (int i = 0; i < look.Renderers.Count; i++)
            {
                SceneLookLookRenderer renderer = look.Renderers[i];
                GameObject owner = Child(renderer.Name, holder.transform, state);
                SceneLookNode node = SceneLookFrame.Node(look, renderer.Source.GameObject.PathId);
                if (node != null) PlaceMatrix(owner.transform, SceneLookApplyMesh.ToMatrix4x4(SceneLookFrame.Relative(look, node, byTransform)));
                Matrix4x4[] poses = BindPoses(renderer, map, rest, frame);
                if (!Attach(owner, renderer, assets, resolved[i], rootBone, target, poses, state, out reason)) return false;
            }
            return true;
        }

        internal static bool Retarget(SceneLookLook look, GameObject body, SceneLookRigPlan plan, SceneLookAssets assets, SceneLookApplied state, out string reason)
        {
            reason = null;
            Avatar targetAvatar = HumanAvatar(body);
            Animator targetAnimator = HumanAnimator(body);
            if (targetAvatar == null || targetAnimator == null)
            {
                reason = "target is not humanoid";
                return false;
            }
            float factor;
            string scaleReason;
            if (!SceneLookFrame.HolderRatio(look, BodyScale(body), out factor, out scaleReason))
            {
                reason = scaleReason;
                return false;
            }
            Dictionary<long, SceneLookMatrix> rest = LookRest(look);
            GameObject holder = Holder(look, body, state);
            Dictionary<long, Transform> nodes = new Dictionary<long, Transform>(look.Nodes.Count);
            foreach (SceneLookNode node in look.Nodes)
            {
                if (node.GameObject == look.Root)
                {
                    nodes[node.Transform] = holder.transform;
                    continue;
                }
                nodes[node.Transform] = Child(node.Name, holder.transform, state).transform;
            }
            foreach (SceneLookNode node in look.Nodes)
            {
                if (node.GameObject == look.Root) continue;
                Transform transform = nodes[node.Transform];
                Transform parent = holder.transform;
                if (node.Father.FileId == 0)
                {
                    Transform found;
                    if (nodes.TryGetValue(node.Father.PathId, out found)) parent = found;
                }
                transform.SetParent(parent, false);
            }
            Dictionary<long, SceneLookMatrix> worlds = new Dictionary<long, SceneLookMatrix>(look.Nodes.Count);
            Dictionary<long, Matrix4x4> locals = new Dictionary<long, Matrix4x4>(look.Nodes.Count);
            Dictionary<long, SceneLookNode> byTransform = SceneLookFrame.ByTransform(look);
            PlaceMatrix(holder.transform, SceneLookApplyMesh.ToMatrix4x4(SceneLookFrame.Holder(look, byTransform)));
            holder.transform.localScale = holder.transform.localScale * factor;
            foreach (SceneLookNode node in look.Nodes) Place(look, node, byTransform, nodes, rest, worlds, locals, 0);
            Avatar avatar = AvatarBuilder.BuildHumanAvatar(holder, Description(look, plan, locals));
            if (avatar == null || !avatar.isValid || !avatar.isHuman)
            {
                if (avatar != null) UnityEngine.Object.Destroy(avatar);
                reason = "the look skeleton did not build a humanoid avatar";
                return false;
            }
            state.Avatar = avatar;
            Material target = TargetMaterial(body);
            Transform rootBone = RootNode(look, plan, nodes, holder.transform);
            foreach (SceneLookLookRenderer renderer in look.Renderers)
            {
                List<Transform> bones = new List<Transform>(renderer.Bones.Count);
                foreach (SceneLookBone bone in renderer.Bones)
                {
                    Transform found;
                    if (!nodes.TryGetValue(bone.Transform.PathId, out found))
                    {
                        reason = "look bone " + bone.Name + " is not in the look skeleton";
                        return false;
                    }
                    bones.Add(found);
                }
                SceneLookNode node = SceneLookFrame.Node(look, renderer.Source.GameObject.PathId);
                Transform owner = null;
                if (node != null) nodes.TryGetValue(node.Transform, out owner);
                GameObject ownerObject = owner != null ? owner.gameObject : Child(renderer.Name, holder.transform, state);
                if (owner == null && node != null) PlaceMatrix(ownerObject.transform, SceneLookApplyMesh.ToMatrix4x4(SceneLookFrame.Relative(look, node, byTransform)));
                Matrix4x4[] poses = SceneLookApplyMesh.FileBindPoses(renderer.Mesh);
                if (poses == null || poses.Length != bones.Count)
                {
                    reason = "the look mesh " + renderer.Name + " has no usable file bind poses";
                    return false;
                }
                if (!Attach(ownerObject, renderer, assets, bones.ToArray(), rootBone, target, poses, state, out reason)) return false;
            }
            HumanPoseHandler from = null;
            HumanPoseHandler to = null;
            try
            {
                from = new HumanPoseHandler(targetAvatar, targetAnimator.transform);
                to = new HumanPoseHandler(avatar, holder.transform);
                SceneLookPoseDriver driver = holder.AddComponent<SceneLookPoseDriver>();
                driver.Setup(from, to);
                state.Driver = driver;
                from = null;
                to = null;
                return true;
            }
            catch (Exception e)
            {
                reason = e.Message;
                return false;
            }
            finally
            {
                if (from != null) from.Dispose();
                if (to != null) to.Dispose();
            }
        }

        internal static float BodyScale(GameObject body)
        {
            Vector3 scale = body.transform.lossyScale;
            return (Math.Abs(scale.x) + Math.Abs(scale.y) + Math.Abs(scale.z)) / 3f;
        }

        internal static Material TargetMaterial(GameObject body)
        {
            Renderer[] renderers = body.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                Material[] materials = renderer.sharedMaterials;
                if (materials == null) continue;
                foreach (Material material in materials)
                {
                    if (material != null) return material;
                }
            }
            return null;
        }

        internal static GameObject Holder(SceneLookLook look, GameObject body, SceneLookApplied state)
        {
            SceneLookNode node = SceneLookFrame.Node(look, look.Root);
            GameObject holder = Child(node != null ? node.Name : look.FileName, body.transform, state);
            state.Holder = holder;
            return holder;
        }

        internal static GameObject Child(string name, Transform parent, SceneLookApplied state)
        {
            GameObject created = new GameObject(name);
            created.transform.SetParent(parent, false);
            created.layer = parent.gameObject.layer;
            state.Objects.Add(created);
            return created;
        }

        internal static void Place(Transform transform, SceneLookNode node)
        {
            transform.localPosition = Vector3Of(node.Position);
            transform.localRotation = QuaternionOf(node.Rotation);
            transform.localScale = Vector3Of(node.Scale);
        }

        internal static void PlaceMatrix(Transform transform, Matrix4x4 matrix)
        {
            transform.localPosition = new Vector3(matrix.m03, matrix.m13, matrix.m23);
            transform.localRotation = matrix.rotation;
            transform.localScale = matrix.lossyScale;
        }

        private static SceneLookMatrix Place(SceneLookLook look, SceneLookNode node, Dictionary<long, SceneLookNode> byTransform, Dictionary<long, Transform> nodes, Dictionary<long, SceneLookMatrix> rest, Dictionary<long, SceneLookMatrix> worlds, Dictionary<long, Matrix4x4> locals, int depth)
        {
            SceneLookMatrix cached;
            if (worlds.TryGetValue(node.Transform, out cached)) return cached;
            if (node.GameObject == look.Root || depth > MaxDepth)
            {
                worlds[node.Transform] = SceneLookMatrix.Identity();
                locals[node.Transform] = Matrix4x4.identity;
                return SceneLookMatrix.Identity();
            }
            worlds[node.Transform] = SceneLookMatrix.Identity();
            SceneLookNode parentNode = Parent(node, byTransform);
            SceneLookMatrix parentWorld = parentNode == null ? SceneLookMatrix.Identity() : Place(look, parentNode, byTransform, nodes, rest, worlds, locals, depth + 1);
            SceneLookMatrix restWorld;
            SceneLookMatrix matrix;
            if (rest.TryGetValue(node.Transform, out restWorld)) matrix = SceneLookBind.Local(parentWorld, restWorld);
            else matrix = SceneLookApplyMesh.ToSceneLookMatrix(Local(node));
            Matrix4x4 local = SceneLookApplyMesh.ToMatrix4x4(matrix);
            locals[node.Transform] = local;
            PlaceMatrix(nodes[node.Transform], local);
            SceneLookMatrix world = parentWorld.Multiply(matrix);
            worlds[node.Transform] = world;
            return world;
        }

        private static SceneLookNode Parent(SceneLookNode node, Dictionary<long, SceneLookNode> byTransform)
        {
            if (node.Father.FileId != 0) return null;
            SceneLookNode parent;
            if (!byTransform.TryGetValue(node.Father.PathId, out parent)) return null;
            return parent;
        }

        private static Matrix4x4[] BindPoses(SceneLookLookRenderer renderer, Dictionary<string, string> map, Dictionary<string, SceneLookMatrix> rest, SceneLookMatrix frame)
        {
            Matrix4x4[] poses = new Matrix4x4[renderer.Bones.Count];
            for (int i = 0; i < renderer.Bones.Count; i++)
            {
                string name;
                SceneLookMatrix boneRest;
                if (map.TryGetValue(renderer.Bones[i].Name, out name) && rest.TryGetValue(name, out boneRest)) poses[i] = SceneLookApplyMesh.ToMatrix4x4(SceneLookBind.BindPose(boneRest, frame));
                else poses[i] = SceneLookApplyMesh.FilePose(renderer.Mesh, i);
            }
            return poses;
        }

        private static Dictionary<string, SceneLookMatrix> TargetRest(GameObject body)
        {
            List<SceneLookBindMesh<string>> meshes = new List<SceneLookBindMesh<string>>();
            SkinnedMeshRenderer[] renderers = body.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (SkinnedMeshRenderer renderer in renderers)
            {
                if (renderer == null) continue;
                Mesh mesh = renderer.sharedMesh;
                if (mesh == null) continue;
                Matrix4x4[] poses = mesh.bindposes;
                Transform[] bones = renderer.bones;
                if (poses == null || bones == null) continue;
                SceneLookBindMesh<string> item = new SceneLookBindMesh<string>();
                int count = Math.Min(poses.Length, bones.Length);
                for (int i = 0; i < count; i++)
                {
                    if (bones[i] == null) continue;
                    item.Add(bones[i].name, SceneLookApplyMesh.ToSceneLookMatrix(poses[i]));
                }
                meshes.Add(item);
            }
            int unresolved;
            return SceneLookBind.Rest(meshes, out unresolved);
        }

        private static Dictionary<long, SceneLookMatrix> LookRest(SceneLookLook look)
        {
            int unresolved;
            return SceneLookBind.Rest(SceneLookFrame.RestMeshes(look), out unresolved);
        }

        internal static string ScaleNote(SceneLookLook look)
        {
            if (look == null || look.Nodes == null) return string.Empty;
            Dictionary<long, SceneLookMatrix> rest;
            try
            {
                rest = LookRest(look);
            }
            catch (Exception)
            {
                return string.Empty;
            }
            List<string> parts = new List<string>();
            foreach (SceneLookNode node in look.Nodes)
            {
                if (rest.ContainsKey(node.Transform)) continue;
                if (node.Scale == null || node.Scale.Length < 3) continue;
                if (Unit(node.Scale[0]) && Unit(node.Scale[1]) && Unit(node.Scale[2])) continue;
                if (parts.Count >= 3) break;
                parts.Add(node.Name + " (" + Number(node.Scale[0]) + ", " + Number(node.Scale[1]) + ", " + Number(node.Scale[2]) + ")");
            }
            if (parts.Count == 0) return string.Empty;
            return " | non unit scale on a node outside the file bind poses: " + string.Join(", ", parts.ToArray());
        }

        internal static float BindScale(float[] pose)
        {
            if (pose == null || pose.Length < 11) return 0f;
            float x = (float)Math.Sqrt(pose[0] * pose[0] + pose[1] * pose[1] + pose[2] * pose[2]);
            float y = (float)Math.Sqrt(pose[4] * pose[4] + pose[5] * pose[5] + pose[6] * pose[6]);
            float z = (float)Math.Sqrt(pose[8] * pose[8] + pose[9] * pose[9] + pose[10] * pose[10]);
            return (x + y + z) / 3f;
        }

        internal static bool Unit(float value)
        {
            return value > 0.999f && value < 1.001f;
        }

        internal static string Number(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static Matrix4x4 Local(SceneLookNode node)
        {
            return Matrix4x4.TRS(Vector3Of(node.Position), QuaternionOf(node.Rotation), Vector3Of(node.Scale));
        }

        private static HumanDescription Description(SceneLookLook look, SceneLookRigPlan plan, Dictionary<long, Matrix4x4> locals)
        {
            HumanDescription description = new HumanDescription();
            description.upperArmTwist = 0.5f;
            description.lowerArmTwist = 0.5f;
            description.upperLegTwist = 0.5f;
            description.lowerLegTwist = 0.5f;
            description.armStretch = 0.05f;
            description.legStretch = 0.05f;
            List<SkeletonBone> skeleton = new List<SkeletonBone>(look.Nodes.Count);
            foreach (SceneLookNode node in look.Nodes)
            {
                Matrix4x4 matrix = locals[node.Transform];
                SkeletonBone bone = new SkeletonBone();
                bone.name = node.Name;
                bone.position = new Vector3(matrix.m03, matrix.m13, matrix.m23);
                bone.rotation = matrix.rotation;
                bone.scale = matrix.lossyScale;
                skeleton.Add(bone);
            }
            description.skeleton = skeleton.ToArray();
            List<HumanBone> human = new List<HumanBone>(plan.Roles.Count);
            foreach (SceneLookRigRole role in plan.Roles)
            {
                HumanBone bone = new HumanBone();
                bone.boneName = role.LookName;
                bone.humanName = role.HumanName;
                HumanLimit limit = new HumanLimit();
                limit.useDefaultValues = true;
                bone.limit = limit;
                human.Add(bone);
            }
            description.human = human.ToArray();
            return description;
        }

        private static Transform RootNode(SceneLookLook look, SceneLookRigPlan plan, Dictionary<long, Transform> nodes, Transform fallback)
        {
            foreach (SceneLookRigRole role in plan.Roles)
            {
                if (role.HumanName != "Hips") continue;
                foreach (SceneLookNode node in look.Nodes)
                {
                    if (node.Name != role.LookName) continue;
                    Transform found;
                    if (nodes.TryGetValue(node.Transform, out found)) return found;
                }
            }
            return fallback;
        }

        private static bool Attach(GameObject owner, SceneLookLookRenderer renderer, SceneLookAssets assets, Transform[] bones, Transform rootBone, Material target, Matrix4x4[] poses, SceneLookApplied state, out string reason)
        {
            reason = null;
            Material[] materials = Materials(assets, renderer, target, out reason);
            if (reason != null) return false;
            Mesh mesh = SceneLookApplyMesh.Build(renderer.Mesh, bones.Length > 0 ? poses : null, out reason);
            if (mesh == null) return false;
            state.Meshes.Add(mesh);
            SkinnedMeshRenderer smr = owner.AddComponent<SkinnedMeshRenderer>();
            smr.sharedMesh = mesh;
            if (bones.Length > 0)
            {
                smr.bones = bones;
                smr.rootBone = rootBone;
            }
            smr.sharedMaterials = materials;
            smr.updateWhenOffscreen = true;
            return true;
        }

        private static Material[] Materials(SceneLookAssets assets, SceneLookLookRenderer renderer, Material target, out string reason)
        {
            reason = null;
            List<Material> materials = new List<Material>(renderer.Materials.Count);
            foreach (SceneLookMaterial material in renderer.Materials)
            {
                if (material == null)
                {
                    materials.Add(null);
                    continue;
                }
                Material built = SceneLookApplyTexture.Material(assets, material, target, out reason);
                if (reason != null) return null;
                materials.Add(built);
            }
            if (materials.Count == 0)
            {
                Material plain = SceneLookApplyTexture.Plain(assets, target, out reason);
                if (reason != null) return null;
                materials.Add(plain);
            }
            return materials.ToArray();
        }

        private static Vector3 Vector3Of(float[] values)
        {
            return new Vector3(values[0], values[1], values[2]);
        }

        private static UnityEngine.Quaternion QuaternionOf(float[] values)
        {
            return new UnityEngine.Quaternion(values[0], values[1], values[2], values[3]);
        }
    }

    internal sealed class SceneLookPoseDriver : MonoBehaviour
    {
        private HumanPoseHandler from;
        private HumanPoseHandler to;
        private HumanPose pose;

        internal bool Auto = true;

        internal void Setup(HumanPoseHandler source, HumanPoseHandler look)
        {
            from = source;
            to = look;
            pose = new HumanPose();
            pose.muscles = new float[HumanTrait.MuscleCount];
        }

        internal void Sync()
        {
            if (from == null || to == null) return;
            from.GetHumanPose(ref pose);
            to.SetHumanPose(ref pose);
        }

        private void LateUpdate()
        {
            if (!Auto) return;
            Sync();
        }

        private void OnDestroy()
        {
            if (from != null) from.Dispose();
            if (to != null) to.Dispose();
            from = null;
            to = null;
        }
    }
}
