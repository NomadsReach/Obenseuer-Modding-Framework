using System;
using System.Collections.Generic;
using ObenseuerCore;

namespace SceneLookHarness
{
    internal static class SceneLookHarnessBlendShapes
    {
        private const int RefFrames = 83;
        private const int RefChannels = 83;
        private const long RefShapeVertices = 203931;
        private const int RefFrameVertices = 2457;
        private const float RefFrameWeight = 100f;
        private static readonly int[] RefHashes = { 1203594905, 141274217, -614799853 };
        private static readonly string[] RefNames = { "_02UpperLidRaise", "_03LowerLidLower", "_04LowerLidRaise" };

        internal static int Run(string dataDir, string meshArg, string fileArg)
        {
            string file = string.IsNullOrEmpty(fileArg) ? "resources.assets" : fileArg;
            string wanted = string.IsNullOrEmpty(meshArg) ? "head" : meshArg;
            SceneLookFiles files = new SceneLookFiles(dataDir);
            try
            {
                string reason;
                SceneLookFile source = files.Get(file, out reason);
                if (source == null)
                {
                    Console.WriteLine(file + " is not readable: " + reason);
                    return 1;
                }
                long pid = 0;
                if (!long.TryParse(wanted, out pid)) pid = 0;
                string name = pid == 0 ? wanted : null;
                List<SceneLookMesh> found = new List<SceneLookMesh>();
                foreach (long candidate in source.Order)
                {
                    if (source.ClassId(candidate) != SceneLookClass.Mesh) continue;
                    if (pid != 0)
                    {
                        if (candidate != pid) continue;
                    }
                    else if (HeadName(source, candidate) != name)
                    {
                        continue;
                    }
                    SceneLookMesh mesh = SceneLookReaderMesh.ParseMesh(source, candidate, false);
                    found.Add(mesh);
                }
                if (found.Count == 0)
                {
                    Console.WriteLine("no mesh named " + wanted + " in " + file);
                    return 1;
                }
                SceneLookMesh best = null;
                SceneLookMesh reference = null;
                foreach (SceneLookMesh mesh in found)
                {
                    if (reference == null && mesh.BlendShapes.Count == RefFrames && mesh.BlendShapeNames.Count == RefChannels && mesh.BlendShapeVertexCount == RefShapeVertices) reference = mesh;
                    if (best == null || mesh.BlendShapes.Count > best.BlendShapes.Count) best = mesh;
                }
                SceneLookMesh chosen = reference != null ? reference : best;
                Console.WriteLine("{0} meshes matched {1} in {2}; reporting {3} (pid {4})", found.Count, wanted, file, reference != null ? "the reference mesh" : "the mesh with the most frames", chosen.PathId);
                return Report(chosen);
            }
            finally
            {
                files.Dispose();
            }
        }

        private static int Report(SceneLookMesh mesh)
        {
            int frames = mesh.BlendShapes.Count;
            int channels = mesh.BlendShapeNames.Count;
            long total = 0;
            foreach (SceneLookBlendShape shape in mesh.BlendShapes) total += shape.VertexCount;
            long weights = mesh.BlendShapeFrameWeights == null ? 0 : mesh.BlendShapeFrameWeights.Length;
            long channelFrames = 0;
            foreach (SceneLookBlendShapeChannel channel in mesh.BlendShapeChannels) channelFrames += channel.Frames;
            bool chain = true;
            for (int i = 0; i + 1 < frames; i++)
            {
                if (mesh.BlendShapes[i].FirstVertex + mesh.BlendShapes[i].VertexCount != mesh.BlendShapes[i + 1].FirstVertex) chain = false;
            }
            bool spans = frames > 0 && mesh.BlendShapes[0].FirstVertex == 0 && mesh.BlendShapes[frames - 1].FirstVertex + mesh.BlendShapes[frames - 1].VertexCount == mesh.BlendShapeVertexCount;
            Console.WriteLine("mesh {0} ({1} pid {2}) | vertices {3} | frames {4} | channel names {5} | shape vertices {6} | frame weights {7} | index format {8}", mesh.Name, mesh.FileName, mesh.PathId, mesh.VertexCount, frames, channels, mesh.BlendShapeVertexCount, weights, mesh.IndexFormat);
            Console.WriteLine("channel records (hash, firstFrame, frames):");
            for (int i = 0; i < channels; i++)
            {
                if (channels > 12 && i >= 8 && i < channels - 2)
                {
                    if (i == 8) Console.WriteLine("  ...");
                    continue;
                }
                SceneLookBlendShapeChannel channel = mesh.BlendShapeChannels[i];
                Console.WriteLine("  [{0}] '{1}' hash {2} firstFrame {3} frames {4}", i, mesh.BlendShapeNames[i], channel.Hash, channel.FirstFrame, channel.Frames);
            }
            Console.WriteLine("frame records (firstVertex, vertexCount, hasNormals, hasTangents):");
            for (int i = 0; i < frames; i++)
            {
                if (frames > 12 && i >= 4 && i < frames - 2)
                {
                    if (i == 4) Console.WriteLine("  ...");
                    continue;
                }
                SceneLookBlendShape shape = mesh.BlendShapes[i];
                Console.WriteLine("  [{0}] {1} {2} {3} {4}", i, shape.FirstVertex, shape.VertexCount, shape.HasNormals, shape.HasTangents);
            }
            Console.WriteLine("frame weights first 4: {0}", FirstWeights(mesh.BlendShapeFrameWeights));
            Console.WriteLine("chain: firstVertex + vertexCount of every frame equals the next firstVertex: {0}", chain);
            Console.WriteLine("spans: firstVertex 0 and the last frame ends at the shape vertex total: {0}", spans);
            Console.WriteLine("sum of frame vertex counts {0} equals the shape vertex total {1}: {2}", total, mesh.BlendShapeVertexCount, total == mesh.BlendShapeVertexCount);
            Console.WriteLine("sum of channel frame counts {0} equals the frame count {1}: {2}", channelFrames, frames, channelFrames == frames);
            Console.WriteLine("frame weights {0} equal the frame count {1}: {2}", weights, frames, weights == frames);
            int multi = 0;
            foreach (SceneLookBlendShapeChannel channel in mesh.BlendShapeChannels)
            {
                if (channel.Frames > 1) multi++;
            }
            Console.WriteLine("channels with more than one frame: {0}", multi);
            bool ok = chain && spans && total == mesh.BlendShapeVertexCount && channelFrames == frames && weights == frames;
            bool reference = frames == RefFrames && channels == RefChannels && mesh.BlendShapeVertexCount == RefShapeVertices;
            if (!reference)
            {
                Console.WriteLine("reference numbers do not apply to this mesh (they are {0} frames, {1} channels and {2} shape vertices)", RefFrames, RefChannels, RefShapeVertices);
                Console.WriteLine("blendshape layout check: {0}", ok ? "PASS" : "FAIL");
                return ok ? 0 : 1;
            }
            bool matches = true;
            for (int i = 0; i < RefHashes.Length; i++)
            {
                SceneLookBlendShapeChannel channel = mesh.BlendShapeChannels[i];
                if (channel.Hash != RefHashes[i] || channel.FirstFrame != i || channel.Frames != 1) matches = false;
                if (mesh.BlendShapeNames[i] != RefNames[i]) matches = false;
            }
            foreach (SceneLookBlendShape shape in mesh.BlendShapes)
            {
                if (shape.VertexCount != RefFrameVertices) matches = false;
            }
            foreach (float weight in mesh.BlendShapeFrameWeights)
            {
                if (weight != RefFrameWeight) matches = false;
            }
            Console.WriteLine("reference: {0} frames, {1} channels, {2} shape vertices, {3} frame vertices, frame weight {4}, first hashes {5}, first names {6}: {7}", RefFrames, RefChannels, RefShapeVertices, RefFrameVertices, RefFrameWeight, string.Join(", ", RefHashesToString()), string.Join(", ", RefNames), matches ? "match" : "MISMATCH");
            bool pass = ok && matches;
            Console.WriteLine("blendshape layout check: {0}", pass ? "PASS" : "FAIL");
            return pass ? 0 : 1;
        }

        private static string[] RefHashesToString()
        {
            string[] text = new string[RefHashes.Length];
            for (int i = 0; i < RefHashes.Length; i++) text[i] = RefHashes[i].ToString();
            return text;
        }

        private static string FirstWeights(float[] weights)
        {
            if (weights == null || weights.Length == 0) return "(none)";
            List<string> values = new List<string>();
            for (int i = 0; i < weights.Length && i < 4; i++) values.Add(weights[i].ToString());
            return string.Join(", ", values.ToArray());
        }

        private static string HeadName(SceneLookFile file, long pathId)
        {
            try
            {
                return new SceneLookCursor(file.ReadObjectHead(pathId, 256), 0).Text();
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
