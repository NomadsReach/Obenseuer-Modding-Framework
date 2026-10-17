using System;
using System.Collections.Generic;

namespace ObenseuerCore
{
    internal struct SceneLookMatrix
    {
        public float M00;
        public float M01;
        public float M02;
        public float M03;
        public float M10;
        public float M11;
        public float M12;
        public float M13;
        public float M20;
        public float M21;
        public float M22;
        public float M23;
        public float M30;
        public float M31;
        public float M32;
        public float M33;

        internal static SceneLookMatrix Identity()
        {
            SceneLookMatrix matrix = new SceneLookMatrix();
            matrix.M00 = 1f;
            matrix.M11 = 1f;
            matrix.M22 = 1f;
            matrix.M33 = 1f;
            return matrix;
        }

        internal static SceneLookMatrix FromFile(float[] values)
        {
            SceneLookMatrix matrix = new SceneLookMatrix();
            matrix.M00 = values[0];
            matrix.M01 = values[1];
            matrix.M02 = values[2];
            matrix.M03 = values[3];
            matrix.M10 = values[4];
            matrix.M11 = values[5];
            matrix.M12 = values[6];
            matrix.M13 = values[7];
            matrix.M20 = values[8];
            matrix.M21 = values[9];
            matrix.M22 = values[10];
            matrix.M23 = values[11];
            matrix.M30 = values[12];
            matrix.M31 = values[13];
            matrix.M32 = values[14];
            matrix.M33 = values[15];
            return matrix;
        }

        internal static SceneLookMatrix Trs(float[] position, float[] rotation, float[] scale)
        {
            float x = rotation[0];
            float y = rotation[1];
            float z = rotation[2];
            float w = rotation[3];
            float xx = x * x;
            float yy = y * y;
            float zz = z * z;
            float xy = x * y;
            float xz = x * z;
            float yz = y * z;
            float wx = w * x;
            float wy = w * y;
            float wz = w * z;
            SceneLookMatrix matrix = new SceneLookMatrix();
            matrix.M00 = (1f - 2f * (yy + zz)) * scale[0];
            matrix.M01 = (2f * (xy - wz)) * scale[1];
            matrix.M02 = (2f * (xz + wy)) * scale[2];
            matrix.M10 = (2f * (xy + wz)) * scale[0];
            matrix.M11 = (1f - 2f * (xx + zz)) * scale[1];
            matrix.M12 = (2f * (yz - wx)) * scale[2];
            matrix.M20 = (2f * (xz - wy)) * scale[0];
            matrix.M21 = (2f * (yz + wx)) * scale[1];
            matrix.M22 = (1f - 2f * (xx + yy)) * scale[2];
            matrix.M03 = position[0];
            matrix.M13 = position[1];
            matrix.M23 = position[2];
            matrix.M33 = 1f;
            return matrix;
        }

        internal SceneLookMatrix Multiply(SceneLookMatrix other)
        {
            SceneLookMatrix matrix = new SceneLookMatrix();
            matrix.M00 = M00 * other.M00 + M01 * other.M10 + M02 * other.M20 + M03 * other.M30;
            matrix.M01 = M00 * other.M01 + M01 * other.M11 + M02 * other.M21 + M03 * other.M31;
            matrix.M02 = M00 * other.M02 + M01 * other.M12 + M02 * other.M22 + M03 * other.M32;
            matrix.M03 = M00 * other.M03 + M01 * other.M13 + M02 * other.M23 + M03 * other.M33;
            matrix.M10 = M10 * other.M00 + M11 * other.M10 + M12 * other.M20 + M13 * other.M30;
            matrix.M11 = M10 * other.M01 + M11 * other.M11 + M12 * other.M21 + M13 * other.M31;
            matrix.M12 = M10 * other.M02 + M11 * other.M12 + M12 * other.M22 + M13 * other.M32;
            matrix.M13 = M10 * other.M03 + M11 * other.M13 + M12 * other.M23 + M13 * other.M33;
            matrix.M20 = M20 * other.M00 + M21 * other.M10 + M22 * other.M20 + M23 * other.M30;
            matrix.M21 = M20 * other.M01 + M21 * other.M11 + M22 * other.M21 + M23 * other.M31;
            matrix.M22 = M20 * other.M02 + M21 * other.M12 + M22 * other.M22 + M23 * other.M32;
            matrix.M23 = M20 * other.M03 + M21 * other.M13 + M22 * other.M23 + M23 * other.M33;
            matrix.M30 = M30 * other.M00 + M31 * other.M10 + M32 * other.M20 + M33 * other.M30;
            matrix.M31 = M30 * other.M01 + M31 * other.M11 + M32 * other.M21 + M33 * other.M31;
            matrix.M32 = M30 * other.M02 + M31 * other.M12 + M32 * other.M22 + M33 * other.M32;
            matrix.M33 = M30 * other.M03 + M31 * other.M13 + M32 * other.M23 + M33 * other.M33;
            return matrix;
        }

        internal SceneLookMatrix Inverse()
        {
            float a = M00;
            float b = M01;
            float c = M02;
            float d = M10;
            float e = M11;
            float f = M12;
            float g = M20;
            float h = M21;
            float i = M22;
            float determinant = a * (e * i - f * h) - b * (d * i - f * g) + c * (d * h - e * g);
            float inverse = 1f / determinant;
            SceneLookMatrix matrix = new SceneLookMatrix();
            matrix.M00 = (e * i - f * h) * inverse;
            matrix.M01 = (c * h - b * i) * inverse;
            matrix.M02 = (b * f - c * e) * inverse;
            matrix.M10 = (f * g - d * i) * inverse;
            matrix.M11 = (a * i - c * g) * inverse;
            matrix.M12 = (c * d - a * f) * inverse;
            matrix.M20 = (d * h - e * g) * inverse;
            matrix.M21 = (b * g - a * h) * inverse;
            matrix.M22 = (a * e - b * d) * inverse;
            matrix.M30 = 0f;
            matrix.M31 = 0f;
            matrix.M32 = 0f;
            matrix.M33 = 1f;
            matrix.M03 = -(matrix.M00 * M03 + matrix.M01 * M13 + matrix.M02 * M23);
            matrix.M13 = -(matrix.M10 * M03 + matrix.M11 * M13 + matrix.M12 * M23);
            matrix.M23 = -(matrix.M20 * M03 + matrix.M21 * M13 + matrix.M22 * M23);
            return matrix;
        }

        internal float[] Translation()
        {
            float[] translation = new float[3];
            translation[0] = M03;
            translation[1] = M13;
            translation[2] = M23;
            return translation;
        }

        internal float MaxDifference(SceneLookMatrix other)
        {
            float most = Math.Abs(M00 - other.M00);
            most = Math.Max(most, Math.Abs(M01 - other.M01));
            most = Math.Max(most, Math.Abs(M02 - other.M02));
            most = Math.Max(most, Math.Abs(M03 - other.M03));
            most = Math.Max(most, Math.Abs(M10 - other.M10));
            most = Math.Max(most, Math.Abs(M11 - other.M11));
            most = Math.Max(most, Math.Abs(M12 - other.M12));
            most = Math.Max(most, Math.Abs(M13 - other.M13));
            most = Math.Max(most, Math.Abs(M20 - other.M20));
            most = Math.Max(most, Math.Abs(M21 - other.M21));
            most = Math.Max(most, Math.Abs(M22 - other.M22));
            most = Math.Max(most, Math.Abs(M23 - other.M23));
            most = Math.Max(most, Math.Abs(M30 - other.M30));
            most = Math.Max(most, Math.Abs(M31 - other.M31));
            most = Math.Max(most, Math.Abs(M32 - other.M32));
            return Math.Max(most, Math.Abs(M33 - other.M33));
        }

        internal static float Distance(SceneLookMatrix first, SceneLookMatrix second)
        {
            float x = first.M03 - second.M03;
            float y = first.M13 - second.M13;
            float z = first.M23 - second.M23;
            return (float)Math.Sqrt(x * x + y * y + z * z);
        }
    }

    internal sealed class SceneLookBindMesh<TKey>
    {
        internal readonly List<TKey> Bones = new List<TKey>();
        internal readonly List<SceneLookMatrix> Poses = new List<SceneLookMatrix>();

        internal void Add(TKey bone, SceneLookMatrix pose)
        {
            Bones.Add(bone);
            Poses.Add(pose);
        }
    }

    internal static class SceneLookBind
    {
        internal static SceneLookMatrix RestWorld(float[] fileBindPose)
        {
            return SceneLookMatrix.FromFile(fileBindPose).Inverse();
        }

        internal static SceneLookMatrix Local(SceneLookMatrix parentWorld, SceneLookMatrix world)
        {
            return parentWorld.Inverse().Multiply(world);
        }

        internal static SceneLookMatrix BindFrame(SceneLookMatrix bodyWorld, SceneLookMatrix holderWorld)
        {
            return bodyWorld.Inverse().Multiply(holderWorld);
        }

        internal static SceneLookMatrix BindPose(SceneLookMatrix boneWorldRest, SceneLookMatrix rendererWorld)
        {
            return boneWorldRest.Inverse().Multiply(rendererWorld);
        }

        internal static Dictionary<TKey, SceneLookMatrix> Rest<TKey>(IList<SceneLookBindMesh<TKey>> meshes, out int unresolved)
        {
            return Rest(meshes, null, out unresolved);
        }

        internal static Dictionary<TKey, SceneLookMatrix> Rest<TKey>(IList<SceneLookBindMesh<TKey>> meshes, Dictionary<TKey, int> sources, out int unresolved)
        {
            Dictionary<TKey, SceneLookMatrix> rest = new Dictionary<TKey, SceneLookMatrix>();
            unresolved = 0;
            if (meshes == null) return rest;
            int reference = -1;
            for (int i = 0; i < meshes.Count; i++)
            {
                if (meshes[i].Bones.Count == 0) continue;
                reference = i;
                break;
            }
            if (reference < 0) return rest;
            SceneLookBindMesh<TKey> first = meshes[reference];
            for (int i = 0; i < first.Bones.Count; i++)
            {
                if (rest.ContainsKey(first.Bones[i])) continue;
                rest[first.Bones[i]] = first.Poses[i].Inverse();
                if (sources != null) sources[first.Bones[i]] = reference;
            }
            bool changed = true;
            while (changed)
            {
                changed = false;
                for (int m = 0; m < meshes.Count; m++)
                {
                    SceneLookBindMesh<TKey> mesh = meshes[m];
                    bool known;
                    SceneLookMatrix frame = Frame(mesh, rest, out known);
                    if (!known) continue;
                    for (int i = 0; i < mesh.Bones.Count; i++)
                    {
                        if (rest.ContainsKey(mesh.Bones[i])) continue;
                        rest[mesh.Bones[i]] = frame.Multiply(mesh.Poses[i].Inverse());
                        if (sources != null) sources[mesh.Bones[i]] = m;
                        changed = true;
                    }
                }
            }
            for (int m = 0; m < meshes.Count; m++)
            {
                SceneLookBindMesh<TKey> mesh = meshes[m];
                for (int i = 0; i < mesh.Bones.Count; i++)
                {
                    if (!rest.ContainsKey(mesh.Bones[i])) unresolved++;
                }
            }
            return rest;
        }

        internal static SceneLookMatrix Frame<TKey>(SceneLookBindMesh<TKey> mesh, Dictionary<TKey, SceneLookMatrix> rest, out bool known)
        {
            known = false;
            SceneLookMatrix frame = SceneLookMatrix.Identity();
            for (int i = 0; i < mesh.Bones.Count; i++)
            {
                SceneLookMatrix bone;
                if (!rest.TryGetValue(mesh.Bones[i], out bone)) continue;
                frame = bone.Multiply(mesh.Poses[i]);
                known = true;
                break;
            }
            return frame;
        }
    }
}
