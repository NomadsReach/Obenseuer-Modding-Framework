using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ObenseuerCore
{
    internal static class SceneLookReaderMesh
    {
        private static readonly int[] VertexFormatSizes = { 4, 2, 1, 1, 2, 2, 1, 1, 2, 2, 4, 4 };
        private const int MaxMips = 16;
        private static readonly int[] SupportedFormats = { 3, 4, 10, 12 };
        private static readonly string[] ShaderNames = { "Standard (Specular setup)", "Standard", "Unlit/Texture", "Unlit/Color", "Legacy Shaders/Diffuse", "Legacy Shaders/Bumped Diffuse", "Mobile/Diffuse", "Sprites/Default", "UI/Default", "Skybox/Cubemap", "Particles/Standard Unlit" };
        private static readonly Dictionary<int, string> FormatNames = new Dictionary<int, string> { { 1, "Alpha8" }, { 2, "ARGB4444" }, { 3, "RGB24" }, { 4, "RGBA32" }, { 5, "ARGB32" }, { 7, "RGB565" }, { 10, "DXT1" }, { 12, "DXT5" }, { 13, "RGBA4444" }, { 14, "BGRA32" }, { 24, "BC6H" }, { 25, "BC7" }, { 26, "BC4" }, { 27, "BC5" }, { 28, "DXT1Crunched" }, { 29, "DXT5Crunched" } };

        internal static string FormatLabel(int format)
        {
            string name;
            return FormatNames.TryGetValue(format, out name) ? name : format.ToString();
        }

        internal static bool IsSupported(int format)
        {
            return Array.IndexOf(SupportedFormats, format) >= 0;
        }

        internal static long ExpectedBytes(int format, int width, int height, int mips)
        {
            bool block = format == 10 || format == 12;
            long unit = format == 3 ? 3 : format == 4 ? 4 : format == 10 ? 8 : 16;
            if (width < 0 || height < 0 || mips < 0 || mips > MaxMips) return -1;
            long total = 0;
            for (int i = 0; i < mips; i++)
            {
                long mw = Math.Max(1L, i < 62 ? (long)width >> i : 0);
                long mh = Math.Max(1L, i < 62 ? (long)height >> i : 0);
                long level = LevelBytes(block, mw, mh, unit);
                if (level < 0 || total > long.MaxValue - level) return -1;
                total += level;
            }
            return total;
        }

        private static long LevelBytes(bool block, long width, long height, long unit)
        {
            if (block)
            {
                long blocksWide = (width + 3) / 4;
                long blocksHigh = (height + 3) / 4;
                if (blocksWide != 0 && blocksHigh > long.MaxValue / blocksWide / unit) return -1;
                return blocksWide * blocksHigh * unit;
            }
            if (width != 0 && height > long.MaxValue / width / unit) return -1;
            return width * height * unit;
        }

        internal static SceneLookRenderer ParseRenderer(long pathId, byte[] bytes)
        {
            SceneLookCursor c = new SceneLookCursor(bytes, 0);
            SceneLookRenderer renderer = new SceneLookRenderer();
            renderer.PathId = pathId;
            renderer.GameObject = c.Pptr();
            c.Skip(60);
            int materials = c.Count();
            renderer.Materials = new List<SceneLookRef>(Math.Min(materials, 64));
            for (int i = 0; i < materials; i++) renderer.Materials.Add(c.Pptr());
            c.U16();
            c.U16();
            c.Pptr();
            c.Pptr();
            c.Pptr();
            c.I32();
            c.I16();
            c.I16();
            c.I32();
            c.U8();
            c.U8();
            c.Align(4);
            renderer.Mesh = c.Pptr();
            int bones = c.Count();
            renderer.Bones = new List<SceneLookRef>(Math.Min(bones, 256));
            for (int i = 0; i < bones; i++) renderer.Bones.Add(c.Pptr());
            int weights = c.Count();
            renderer.BlendShapeWeights = c.Floats(weights);
            renderer.RootBone = c.Pptr();
            c.Skip(24);
            c.U8();
            c.Align(4);
            if (c.Position != bytes.Length) throw new InvalidDataException("renderer has " + bytes.Length + " bytes and " + c.Position + " were read");
            return renderer;
        }

        internal static SceneLookMesh ParseMesh(SceneLookFile f, long pathId, bool keep)
        {
            byte[] bytes = f.ReadObject(pathId);
            SceneLookCursor c = new SceneLookCursor(bytes, 0);
            SceneLookMesh mesh = new SceneLookMesh();
            mesh.PathId = pathId;
            mesh.FileName = f.Name;
            mesh.Name = c.Text();
            int submeshes = c.Count();
            mesh.Submeshes = new List<SceneLookSubmesh>(Math.Min(submeshes, 64));
            for (int i = 0; i < submeshes; i++)
            {
                SceneLookSubmesh submesh = new SceneLookSubmesh();
                submesh.FirstByte = c.U32();
                submesh.IndexCount = c.U32();
                submesh.Topology = c.I32();
                submesh.BaseVertex = c.U32();
                submesh.FirstVertex = c.U32();
                submesh.VertexCount = c.U32();
                c.Skip(24);
                mesh.Submeshes.Add(submesh);
            }
            int shapeVertices = c.Count();
            mesh.BlendShapeVertexCount = shapeVertices;
            int shapeStart = c.Position;
            c.Skip(40L * shapeVertices);
            int shapes = c.Count();
            mesh.BlendShapes = new List<SceneLookBlendShape>(Math.Min(shapes, 256));
            for (int i = 0; i < shapes; i++)
            {
                SceneLookBlendShape shape = new SceneLookBlendShape();
                shape.FirstVertex = c.U32();
                shape.VertexCount = c.U32();
                shape.HasNormals = c.U8() != 0;
                shape.HasTangents = c.U8() != 0;
                c.Align(4);
                mesh.BlendShapes.Add(shape);
            }
            int shapeNames = c.Count();
            mesh.BlendShapeNames = new List<string>(Math.Min(shapeNames, 256));
            mesh.BlendShapeChannels = new List<SceneLookBlendShapeChannel>(Math.Min(shapeNames, 256));
            for (int i = 0; i < shapeNames; i++)
            {
                mesh.BlendShapeNames.Add(c.Text());
                SceneLookBlendShapeChannel channel = new SceneLookBlendShapeChannel();
                channel.Hash = c.I32();
                channel.FirstFrame = c.I32();
                channel.Frames = c.I32();
                mesh.BlendShapeChannels.Add(channel);
            }
            int frameWeights = c.Count();
            mesh.BlendShapeFrameWeights = c.Floats(frameWeights);
            int bindPoses = c.Count();
            if ((long)bindPoses * 64 > c.Length - c.Position) throw new InvalidDataException("bind pose count " + bindPoses + " is past the end of the object");
            if (keep)
            {
                mesh.BindPoses = new float[bindPoses][];
                for (int i = 0; i < bindPoses; i++) mesh.BindPoses[i] = c.Floats(16);
            }
            else
            {
                c.Skip(64L * bindPoses);
            }
            int boneHashes = c.Count();
            c.Skip(4L * boneHashes);
            c.U32();
            int bounds = c.Count();
            c.Skip(24L * bounds);
            int visibility = c.Count();
            c.Skip(visibility);
            c.Align(4);
            c.Skip(4);
            c.Align(4);
            mesh.IndexFormat = c.I32();
            int indexBytes = c.Count();
            mesh.IndexDataSize = indexBytes;
            if (keep) mesh.IndexData = c.Raw(indexBytes);
            else c.Skip(indexBytes);
            c.Align(4);
            mesh.VertexCount = c.U32();
            int channels = c.Count();
            mesh.Channels = new List<SceneLookChannel>(Math.Min(channels, 14));
            for (int i = 0; i < channels; i++)
            {
                SceneLookChannel channel = new SceneLookChannel();
                channel.Stream = c.U8();
                channel.Offset = c.U8();
                channel.Format = c.U8();
                channel.Dimension = c.U8();
                mesh.Channels.Add(channel);
            }
            int vertexBytes = c.Count();
            mesh.VertexDataSize = vertexBytes;
            if (keep) mesh.VertexData = c.Raw(vertexBytes);
            else c.Skip(vertexBytes);
            mesh.ExpectedVertexDataSize = ExpectedVertexBytes(mesh);
            if (keep && shapeVertices > 0)
            {
                mesh.BlendShapeVertexData = new byte[40 * shapeVertices];
                Buffer.BlockCopy(bytes, shapeStart, mesh.BlendShapeVertexData, 0, mesh.BlendShapeVertexData.Length);
            }
            return mesh;
        }

        internal static long IndexTotal(SceneLookMesh mesh)
        {
            long indices = 0;
            foreach (SceneLookSubmesh submesh in mesh.Submeshes) indices += submesh.IndexCount;
            return indices;
        }

        internal static List<string> MeshReasons(SceneLookMesh mesh)
        {
            List<string> reasons = new List<string>();
            long indices = IndexTotal(mesh);
            long indexBytes = indices * (mesh.IndexFormat == 0 ? 2 : 4);
            if (mesh.VertexDataSize == 0 && mesh.ExpectedVertexDataSize > 0) reasons.Add("mesh vertex data not inline");
            else if (mesh.VertexDataSize != mesh.ExpectedVertexDataSize) reasons.Add("mesh vertex size mismatch");
            else if (mesh.IndexDataSize == 0 && indices > 0) reasons.Add("mesh index data not inline");
            else if (mesh.IndexDataSize != indexBytes) reasons.Add("mesh index size mismatch");
            return reasons;
        }

        internal static SceneLookMaterial ParseMaterial(SceneLookFile f, long pathId)
        {
            SceneLookCursor c = new SceneLookCursor(f.ReadObject(pathId), 0);
            SceneLookMaterial material = new SceneLookMaterial();
            material.PathId = pathId;
            material.FileName = f.Name;
            material.Name = c.Text();
            material.Shader = c.Pptr();
            int valid = c.Count();
            for (int i = 0; i < valid; i++) c.Text();
            int invalid = c.Count();
            for (int i = 0; i < invalid; i++) c.Text();
            c.U32();
            c.U8();
            c.Align(4);
            material.RenderQueue = c.I32();
            int tags = c.Count();
            for (int i = 0; i < tags; i++)
            {
                c.Text();
                c.Text();
            }
            int passes = c.Count();
            for (int i = 0; i < passes; i++) c.Text();
            int texEnvs = c.Count();
            material.TexEnvs = new List<SceneLookTexEnv>(Math.Min(texEnvs, 64));
            for (int i = 0; i < texEnvs; i++)
            {
                SceneLookTexEnv env = new SceneLookTexEnv();
                env.Name = c.Text();
                env.Texture = c.Pptr();
                env.Scale = c.Floats(2);
                env.Offset = c.Floats(2);
                material.TexEnvs.Add(env);
            }
            int ints = c.Count();
            material.Ints = new List<SceneLookNamed<int>>(Math.Min(ints, 64));
            for (int i = 0; i < ints; i++)
            {
                SceneLookNamed<int> named = new SceneLookNamed<int>();
                named.Name = c.Text();
                named.Value = c.I32();
                material.Ints.Add(named);
            }
            int floats = c.Count();
            material.Floats = new List<SceneLookNamed<float>>(Math.Min(floats, 64));
            for (int i = 0; i < floats; i++)
            {
                SceneLookNamed<float> named = new SceneLookNamed<float>();
                named.Name = c.Text();
                named.Value = c.F32();
                material.Floats.Add(named);
            }
            int colors = c.Count();
            material.Colors = new List<SceneLookNamed<float[]>>(Math.Min(colors, 64));
            for (int i = 0; i < colors; i++)
            {
                SceneLookNamed<float[]> named = new SceneLookNamed<float[]>();
                named.Name = c.Text();
                named.Value = c.Floats(4);
                material.Colors.Add(named);
            }
            return material;
        }

        internal static SceneLookTexture ParseTexture(SceneLookFile f, long pathId, byte[] bytes)
        {
            SceneLookCursor c = new SceneLookCursor(bytes, 0);
            SceneLookTexture texture = new SceneLookTexture();
            texture.PathId = pathId;
            texture.FileName = f.Name;
            texture.Name = c.Text();
            c.I32();
            texture.Width = c.I32();
            texture.Height = c.I32();
            texture.CompleteSize = c.U32();
            c.I32();
            texture.Format = c.I32();
            texture.Mips = c.I32();
            string path;
            long offset;
            long size;
            if (FindStream(bytes, out path, out offset, out size))
            {
                texture.StreamPath = path;
                texture.StreamOffset = offset;
                texture.StreamSize = size;
            }
            else
            {
                texture.Inline = true;
                texture.InlineValid = InlineOk(bytes, texture.CompleteSize);
            }
            return texture;
        }

        internal static List<string> TextureReasons(SceneLookFiles files, SceneLookTexture texture)
        {
            List<string> reasons = new List<string>();
            if (!IsSupported(texture.Format)) reasons.Add("format " + FormatLabel(texture.Format) + " not supported");
            else if (ExpectedBytes(texture.Format, texture.Width, texture.Height, texture.Mips) != texture.CompleteSize) reasons.Add("texture size rule failed");
            if (texture.Inline)
            {
                if (!texture.InlineValid) reasons.Add("inline texture data not validated");
                return reasons;
            }
            if (!files.Installed(texture.StreamPath, texture.StreamOffset + texture.StreamSize))
            {
                reasons.Add("texture stream not installed");
            }
            else if (texture.StreamSize != texture.CompleteSize)
            {
                reasons.Add("texture stream size mismatch");
            }
            return reasons;
        }

        internal static byte[] TexturePixels(SceneLookFiles files, SceneLookTexture texture)
        {
            if (!texture.Inline) return files.ReadStream(texture.StreamPath, texture.StreamOffset, texture.StreamSize);
            string reason;
            SceneLookFile owner = files.Get(texture.FileName, out reason);
            if (owner == null) throw new InvalidDataException(reason);
            byte[] bytes = owner.ReadObject(texture.PathId);
            int size = (int)texture.CompleteSize;
            if (bytes.Length - 16 - size < 4) throw new InvalidDataException("inline texture " + texture.Name + " is too short");
            byte[] pixels = new byte[size];
            Buffer.BlockCopy(bytes, bytes.Length - 16 - size, pixels, 0, size);
            return pixels;
        }

        internal static int HeadFormat(SceneLookFile f, long pathId)
        {
            SceneLookCursor c = new SceneLookCursor(f.ReadObjectHead(pathId, 512), 0);
            c.Text();
            c.I32();
            c.I32();
            c.I32();
            c.U32();
            c.I32();
            return c.I32();
        }

        internal static string ShaderLabel(SceneLookFile f, long pathId)
        {
            string cached;
            if (f.Memo.ShaderLabels.TryGetValue(pathId, out cached)) return cached;
            string label = ShaderLabelOf(f.ReadObject(pathId));
            f.Memo.ShaderLabels[pathId] = label;
            return label;
        }

        private static string ShaderLabelOf(byte[] bytes)
        {
            foreach (string name in ShaderNames)
            {
                if (SceneLookReader.Contains(bytes, SceneLookReader.LengthPrefixed(name))) return name;
            }
            try
            {
                List<string> props = ShaderProperties(bytes);
                return "unnamed [" + string.Join(",", props.GetRange(0, Math.Min(3, props.Count))) + "]";
            }
            catch (Exception)
            {
                return "unnamed";
            }
        }

        private static List<string> ShaderProperties(byte[] bytes)
        {
            SceneLookCursor c = new SceneLookCursor(bytes, 0);
            c.Text();
            int count = c.Count();
            List<string> names = new List<string>();
            for (int i = 0; i < count; i++)
            {
                names.Add(c.Text());
                c.Text();
                int attributes = c.Count();
                for (int j = 0; j < attributes; j++) c.Text();
                c.I32();
                c.U32();
                c.Skip(16);
                c.Text();
                c.I32();
            }
            return names;
        }

        private static long ExpectedVertexBytes(SceneLookMesh mesh)
        {
            List<int> streams = new List<int>();
            Dictionary<int, long> strides = new Dictionary<int, long>();
            foreach (SceneLookChannel channel in mesh.Channels)
            {
                if (channel.Dimension == 0) continue;
                if (channel.Format < 0 || channel.Format >= VertexFormatSizes.Length) throw new InvalidDataException("vertex format " + channel.Format + " is not handled");
                long stride;
                if (!strides.TryGetValue(channel.Stream, out stride))
                {
                    streams.Add(channel.Stream);
                    stride = 0;
                }
                strides[channel.Stream] = stride + channel.Dimension * VertexFormatSizes[channel.Format];
            }
            long expected = 0;
            for (int i = 0; i < streams.Count; i++)
            {
                long size = mesh.VertexCount * strides[streams[i]];
                expected += i < streams.Count - 1 ? (size + 15) / 16 * 16 : size;
            }
            return expected;
        }

        private static bool FindStream(byte[] bytes, out string path, out long offset, out long size)
        {
            path = null;
            offset = 0;
            size = 0;
            foreach (string extension in new[] { ".resS", ".resource" })
            {
                byte[] ext = Encoding.ASCII.GetBytes(extension);
                int e = LastIndexOf(bytes, ext);
                if (e < 0) continue;
                int end = e + ext.Length;
                for (int n = ext.Length + 1; n <= 200; n++)
                {
                    int s = end - n;
                    if (s < 16) break;
                    if (BitConverter.ToInt32(bytes, s - 4) != n || !IsPrintable(bytes, s, end)) continue;
                    path = Encoding.ASCII.GetString(bytes, s, end - s);
                    offset = BitConverter.ToInt64(bytes, s - 16);
                    size = BitConverter.ToUInt32(bytes, s - 8);
                    return true;
                }
            }
            return false;
        }

        private static bool InlineOk(byte[] bytes, long complete)
        {
            if (bytes.Length < complete + 20) return false;
            int at = (int)(bytes.Length - 16 - complete - 4);
            return BitConverter.ToUInt32(bytes, at) == complete;
        }

        private static bool IsPrintable(byte[] bytes, int start, int end)
        {
            for (int i = start; i < end; i++)
            {
                if (bytes[i] < 0x20 || bytes[i] > 0x7E) return false;
            }
            return true;
        }

        private static int LastIndexOf(byte[] haystack, byte[] needle)
        {
            for (int i = haystack.Length - needle.Length; i >= 0; i--)
            {
                int j = 0;
                while (j < needle.Length && haystack[i + j] == needle[j]) j++;
                if (j == needle.Length) return i;
            }
            return -1;
        }

    }
}
