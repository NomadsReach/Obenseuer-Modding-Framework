using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ObenseuerCore
{
    internal static class SceneLookApplyMesh
    {
        internal static Matrix4x4[] FileBindPoses(SceneLookMesh source)
        {
            if (source == null || source.BindPoses == null) return null;
            Matrix4x4[] poses = new Matrix4x4[source.BindPoses.Length];
            for (int i = 0; i < poses.Length; i++) poses[i] = ToMatrix4x4(SceneLookMatrix.FromFile(source.BindPoses[i]));
            return poses;
        }

        internal static Matrix4x4 FilePose(SceneLookMesh source, int index)
        {
            if (source == null || source.BindPoses == null || index < 0 || index >= source.BindPoses.Length) return Matrix4x4.identity;
            return ToMatrix4x4(SceneLookMatrix.FromFile(source.BindPoses[index]));
        }

        internal static Matrix4x4[] BindPoses(SceneLookMatrix[] boneRestWorld, SceneLookMatrix rendererRestWorld)
        {
            Matrix4x4[] poses = new Matrix4x4[boneRestWorld.Length];
            for (int i = 0; i < poses.Length; i++) poses[i] = ToMatrix4x4(SceneLookBind.BindPose(boneRestWorld[i], rendererRestWorld));
            return poses;
        }

        internal static SceneLookMatrix ToSceneLookMatrix(Matrix4x4 matrix)
        {
            SceneLookMatrix result = new SceneLookMatrix();
            result.M00 = matrix.m00;
            result.M01 = matrix.m01;
            result.M02 = matrix.m02;
            result.M03 = matrix.m03;
            result.M10 = matrix.m10;
            result.M11 = matrix.m11;
            result.M12 = matrix.m12;
            result.M13 = matrix.m13;
            result.M20 = matrix.m20;
            result.M21 = matrix.m21;
            result.M22 = matrix.m22;
            result.M23 = matrix.m23;
            result.M30 = matrix.m30;
            result.M31 = matrix.m31;
            result.M32 = matrix.m32;
            result.M33 = matrix.m33;
            return result;
        }

        internal static Matrix4x4 ToMatrix4x4(SceneLookMatrix matrix)
        {
            Matrix4x4 result = new Matrix4x4();
            result.m00 = matrix.M00;
            result.m01 = matrix.M01;
            result.m02 = matrix.M02;
            result.m03 = matrix.M03;
            result.m10 = matrix.M10;
            result.m11 = matrix.M11;
            result.m12 = matrix.M12;
            result.m13 = matrix.M13;
            result.m20 = matrix.M20;
            result.m21 = matrix.M21;
            result.m22 = matrix.M22;
            result.m23 = matrix.M23;
            result.m30 = matrix.M30;
            result.m31 = matrix.M31;
            result.m32 = matrix.M32;
            result.m33 = matrix.M33;
            return result;
        }

        internal static Mesh Build(SceneLookMesh source, Matrix4x4[] bindPoses, out string reason)
        {
            reason = null;
            if (source == null)
            {
                reason = "no mesh";
                return null;
            }
            if (source.VertexData == null || source.IndexData == null)
            {
                reason = "mesh buffers are not loaded";
                return null;
            }
            if (source.VertexCount == 0 || source.VertexCount > int.MaxValue)
            {
                reason = "mesh vertex count " + source.VertexCount + " is not usable";
                return null;
            }
            long indexStride = source.IndexFormat == 0 ? 2 : 4;
            long indexCount = SceneLookReaderMesh.IndexTotal(source);
            if (source.IndexDataSize != indexCount * indexStride)
            {
                reason = "mesh index bytes do not match the index count";
                return null;
            }
            List<SceneLookMeshStream> streams = SceneLookMeshPlan.Plan(source, out reason);
            if (reason != null) return null;
            List<VertexAttributeDescriptor> attributes = new List<VertexAttributeDescriptor>();
            foreach (int attribute in SceneLookMeshPlan.Attributes(source))
            {
                SceneLookChannel channel = source.Channels[attribute];
                attributes.Add(new VertexAttributeDescriptor((VertexAttribute)attribute, (VertexAttributeFormat)channel.Format, channel.Dimension, channel.Stream));
            }
            Mesh mesh = new Mesh();
            try
            {
                mesh.name = source.Name;
                mesh.SetVertexBufferParams((int)source.VertexCount, attributes.ToArray());
                for (int i = 0; i < streams.Count; i++)
                {
                    SceneLookMeshStream stream = streams[i];
                    int count = (int)(source.VertexCount * stream.Stride);
                    mesh.SetVertexBufferData<byte>(source.VertexData, stream.Start, 0, count, stream.Stream);
                }
                if (indexCount > 0)
                {
                    mesh.indexFormat = source.IndexFormat == 0 ? IndexFormat.UInt16 : IndexFormat.UInt32;
                    mesh.SetIndexBufferParams((int)indexCount, mesh.indexFormat);
                    if (source.IndexFormat == 0)
                    {
                        ushort[] indices = new ushort[(int)indexCount];
                        Buffer.BlockCopy(source.IndexData, 0, indices, 0, (int)(indexCount * 2));
                        mesh.SetIndexBufferData<ushort>(indices, 0, 0, indices.Length);
                    }
                    else
                    {
                        uint[] indices = new uint[(int)indexCount];
                        Buffer.BlockCopy(source.IndexData, 0, indices, 0, (int)(indexCount * 4));
                        mesh.SetIndexBufferData<uint>(indices, 0, 0, indices.Length);
                    }
                    mesh.subMeshCount = source.Submeshes.Count;
                    for (int i = 0; i < source.Submeshes.Count; i++)
                    {
                        SceneLookSubmesh submesh = source.Submeshes[i];
                        if (submesh.FirstByte % indexStride != 0)
                        {
                            reason = "submesh " + i + " does not start on an index";
                            UnityEngine.Object.Destroy(mesh);
                            return null;
                        }
                        long start = submesh.FirstByte / indexStride;
                        if (start + submesh.IndexCount > indexCount)
                        {
                            reason = "submesh " + i + " is past the index buffer";
                            UnityEngine.Object.Destroy(mesh);
                            return null;
                        }
                        SubMeshDescriptor descriptor = new SubMeshDescriptor((int)start, (int)submesh.IndexCount, (MeshTopology)submesh.Topology);
                        descriptor.baseVertex = (int)submesh.BaseVertex;
                        descriptor.firstVertex = (int)submesh.FirstVertex;
                        descriptor.vertexCount = (int)submesh.VertexCount;
                        mesh.SetSubMesh(i, descriptor);
                    }
                }
                if (bindPoses != null) mesh.bindposes = bindPoses;
                mesh.RecalculateBounds();
            }
            catch (Exception e)
            {
                UnityEngine.Object.Destroy(mesh);
                reason = e.Message;
                return null;
            }
            return mesh;
        }
    }
}
