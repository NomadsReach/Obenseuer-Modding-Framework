using System;
using System.Collections.Generic;

namespace ObenseuerCore
{
    internal static class SceneLookFrame
    {
        internal const int MaxDepth = 256;
        internal const float MinScale = 0.0001f;

        internal static SceneLookNode Node(SceneLookLook look, long gameObject)
        {
            foreach (SceneLookNode node in look.Nodes)
            {
                if (node.GameObject == gameObject) return node;
            }
            return null;
        }

        internal static Dictionary<long, SceneLookNode> ByTransform(SceneLookLook look)
        {
            Dictionary<long, SceneLookNode> map = new Dictionary<long, SceneLookNode>(look.Nodes.Count);
            foreach (SceneLookNode node in look.Nodes) map[node.Transform] = node;
            return map;
        }

        internal static SceneLookMatrix Local(SceneLookNode node)
        {
            return SceneLookMatrix.Trs(node.Position, node.Rotation, node.Scale);
        }

        internal static SceneLookMatrix Relative(SceneLookLook look, SceneLookNode node, Dictionary<long, SceneLookNode> byTransform)
        {
            SceneLookMatrix result = Local(node);
            SceneLookNode current = node;
            int depth = 0;
            while (current != null && current.GameObject != look.Root && depth < MaxDepth)
            {
                SceneLookNode parent = Parent(current, byTransform);
                if (parent == null) break;
                result = Local(parent).Multiply(result);
                current = parent;
                depth++;
            }
            return result;
        }

        internal static SceneLookMatrix RelativeWithoutRoot(SceneLookLook look, SceneLookNode node, Dictionary<long, SceneLookNode> byTransform)
        {
            if (node.GameObject == look.Root) return SceneLookMatrix.Identity();
            SceneLookMatrix result = Local(node);
            SceneLookNode current = node;
            int depth = 0;
            while (current != null && current.GameObject != look.Root && depth < MaxDepth)
            {
                SceneLookNode parent = Parent(current, byTransform);
                if (parent == null || parent.GameObject == look.Root) break;
                result = Local(parent).Multiply(result);
                current = parent;
                depth++;
            }
            return result;
        }

        internal static int Seed(SceneLookLook look)
        {
            for (int i = 0; i < look.Renderers.Count; i++)
            {
                SceneLookLookRenderer renderer = look.Renderers[i];
                if (renderer == null || renderer.Source == null) continue;
                if (renderer.Bones.Count == 0) continue;
                if (Node(look, renderer.Source.GameObject.PathId) == null) continue;
                return i;
            }
            return -1;
        }

        internal static SceneLookNode Reference(SceneLookLook look)
        {
            int seed = Seed(look);
            if (seed < 0) return null;
            return Node(look, look.Renderers[seed].Source.GameObject.PathId);
        }

        internal static List<SceneLookLookRenderer> RestRenderers(SceneLookLook look)
        {
            List<SceneLookLookRenderer> renderers = new List<SceneLookLookRenderer>(look.Renderers.Count);
            foreach (SceneLookLookRenderer renderer in look.Renderers)
            {
                if (renderer == null || renderer.Source == null) continue;
                if (Node(look, renderer.Source.GameObject.PathId) == null) continue;
                renderers.Add(renderer);
            }
            return renderers;
        }

        internal static List<SceneLookBindMesh<long>> RestMeshes(SceneLookLook look)
        {
            List<SceneLookLookRenderer> renderers = RestRenderers(look);
            List<SceneLookBindMesh<long>> meshes = new List<SceneLookBindMesh<long>>(renderers.Count);
            foreach (SceneLookLookRenderer renderer in renderers)
            {
                SceneLookBindMesh<long> mesh = new SceneLookBindMesh<long>();
                for (int i = 0; i < renderer.Bones.Count; i++)
                {
                    SceneLookMatrix pose = SceneLookMatrix.Identity();
                    if (renderer.Mesh != null && renderer.Mesh.BindPoses != null && i < renderer.Mesh.BindPoses.Length) pose = SceneLookMatrix.FromFile(renderer.Mesh.BindPoses[i]);
                    mesh.Add(renderer.Bones[i].Transform.PathId, pose);
                }
                meshes.Add(mesh);
            }
            return meshes;
        }

        internal static SceneLookMatrix Holder(SceneLookLook look, Dictionary<long, SceneLookNode> byTransform)
        {
            SceneLookNode reference = Reference(look);
            if (reference == null) return SceneLookMatrix.Identity();
            return RelativeWithoutRoot(look, reference, byTransform);
        }

        internal static float Magnitude(float[] scale)
        {
            if (scale == null || scale.Length < 3) return 1f;
            return (Math.Abs(scale[0]) + Math.Abs(scale[1]) + Math.Abs(scale[2])) / 3f;
        }

        internal static bool Finite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        internal static float RootScale(SceneLookLook look)
        {
            SceneLookNode root = Node(look, look.Root);
            if (root == null) return 1f;
            return Magnitude(root.Scale);
        }

        internal static bool HolderRatio(SceneLookLook look, float targetScale, out float factor, out string reason)
        {
            factor = 1f;
            reason = null;
            if (!Finite(targetScale) || !(targetScale > MinScale))
            {
                reason = "the target body scale is zero or not a finite number";
                return false;
            }
            float root = RootScale(look);
            if (!Finite(root) || !(root > MinScale))
            {
                reason = "the look root scale is zero or not a finite number";
                return false;
            }
            factor = root / targetScale;
            if (!Finite(factor) || !(factor > 0f))
            {
                factor = 1f;
                reason = "the scale ratio is not a finite number above zero";
                return false;
            }
            return true;
        }

        internal static float Scale(SceneLookMatrix matrix)
        {
            double x = Length(matrix.M00, matrix.M10, matrix.M20);
            double y = Length(matrix.M01, matrix.M11, matrix.M21);
            double z = Length(matrix.M02, matrix.M12, matrix.M22);
            return (float)((x + y + z) / 3.0);
        }

        private static double Length(float x, float y, float z)
        {
            return Math.Sqrt((double)x * x + (double)y * y + (double)z * z);
        }

        private static SceneLookNode Parent(SceneLookNode node, Dictionary<long, SceneLookNode> byTransform)
        {
            if (node.Father.FileId != 0) return null;
            SceneLookNode parent;
            if (!byTransform.TryGetValue(node.Father.PathId, out parent)) return null;
            return parent;
        }
    }
}
