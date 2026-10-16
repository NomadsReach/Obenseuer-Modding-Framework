using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ObenseuerCore;

namespace SceneLookHarness
{
    internal static class SceneLookHarnessProof
    {
        private sealed class MaterialLine
        {
            public string Name;
            public string Shader;
            public string Owner;
            public readonly List<KeyValuePair<string, SceneLookTexture>> Slots = new List<KeyValuePair<string, SceneLookTexture>>();
        }

        internal static int Run(string dataDir)
        {
            Stopwatch total = Stopwatch.StartNew();
            SceneLookFiles files = new SceneLookFiles(dataDir);
            try
            {
                return Prove(files, total);
            }
            finally
            {
                files.Dispose();
            }
        }

        private static int Prove(SceneLookFiles files, Stopwatch total)
        {
            string reason;
            SceneLookFile level = files.Get("level15", out reason);
            if (level == null)
            {
                Console.WriteLine("level15 is not readable: " + reason);
                return 1;
            }
            List<string> touched = new List<string>();
            Touch(touched, level);
            Console.WriteLine("Mr. Duck scene body in {0} (Unity {1})", level.Name, level.UnityString);
            long rootGo = 0;
            int rootSmrs = -1;
            int rootNodes = -1;
            foreach (long candidate in level.Order)
            {
                if (level.ClassId(candidate) != SceneLookClass.GameObject || SceneLookReader.GameObject(level, candidate).Name.ToLowerInvariant() != "mr_duck") continue;
                List<long> nodes = SceneLookReader.Subtree(level, candidate);
                int smrs = nodes.Count(n => ComponentCount(level, n, SceneLookClass.SkinnedMeshRenderer) > 0);
                if (smrs > rootSmrs || (smrs == rootSmrs && nodes.Count > rootNodes))
                {
                    rootGo = candidate;
                    rootSmrs = smrs;
                    rootNodes = nodes.Count;
                }
            }
            if (rootSmrs < 0)
            {
                Console.WriteLine("no mr_duck GameObject in level15");
                return 1;
            }
            List<long> duckNodes = SceneLookReader.Subtree(level, rootGo);
            Console.WriteLine("  renderer root: {0} (GameObject pid {1}), {2} SkinnedMeshRenderers", SceneLookReader.PathName(level, rootGo), rootGo, rootSmrs);
            long? npc = null;
            foreach (long candidate in level.Order)
            {
                if (level.ClassId(candidate) == SceneLookClass.GameObject && SceneLookReader.GameObject(level, candidate).Name == "Mr. Duck")
                {
                    npc = candidate;
                    break;
                }
            }
            if (npc != null) Console.WriteLine("  NPC object: {0} (pid {1}), {2} MonoBehaviours", SceneLookReader.PathName(level, npc.Value), npc.Value, ComponentCount(level, npc.Value, SceneLookClass.MonoBehaviour));
            Console.WriteLine("Renderers");
            Dictionary<string, SceneLookMesh> meshes = new Dictionary<string, SceneLookMesh>();
            Dictionary<string, bool> meshOk = new Dictionary<string, bool>();
            SceneLookCounter<string> rig = new SceneLookCounter<string>();
            List<MaterialLine> materials = new List<MaterialLine>();
            Dictionary<string, MaterialLine> materialByName = new Dictionary<string, MaterialLine>();
            List<SceneLookTexture> textures = new List<SceneLookTexture>();
            HashSet<string> textureKeys = new HashSet<string>();
            foreach (long go in duckNodes)
            {
                long renderer = FirstComponent(level, go, SceneLookClass.SkinnedMeshRenderer);
                if (renderer == 0) continue;
                SceneLookRenderer source = SceneLookReaderMesh.ParseRenderer(renderer, level.ReadObject(renderer));
                SceneLookTarget meshTarget = level.Target(source.Mesh);
                SceneLookMesh mesh = SceneLookReaderMesh.ParseMesh(meshTarget.File, meshTarget.PathId, false);
                Touch(touched, meshTarget.File);
                bool ok = SceneLookReaderMesh.MeshReasons(mesh).Count == 0;
                meshes[mesh.Name] = mesh;
                meshOk[mesh.Name] = ok;
                List<string> boneNames = new List<string>();
                foreach (SceneLookRef bone in source.Bones) boneNames.Add(SceneLookReader.BoneName(level.Target(bone)));
                foreach (string bone in boneNames) rig.Add(bone);
                Console.WriteLine("  {0} | mesh {1} ({2}) | vertices {3} | triangles {4} | bones {5} | buffers match {6}", SceneLookReader.GameObject(level, go).Name, mesh.Name, mesh.FileName, mesh.VertexCount, SceneLookReaderMesh.IndexTotal(mesh) / 3, boneNames.Count, ok);
                foreach (SceneLookRef materialRef in source.Materials)
                {
                    SceneLookTarget materialTarget = level.Target(materialRef);
                    if (materialTarget.Error != null) continue;
                    Touch(touched, materialTarget.File);
                    SceneLookMaterial material = SceneLookReaderMesh.ParseMaterial(materialTarget.File, materialTarget.PathId);
                    SceneLookTarget shader = materialTarget.File.Target(material.Shader);
                    MaterialLine line = new MaterialLine();
                    line.Name = material.Name;
                    line.Shader = shader.Error != null ? "unresolved" : SceneLookReaderMesh.ShaderLabel(shader.File, shader.PathId);
                    line.Owner = materialTarget.File.Name;
                    foreach (SceneLookTexEnv env in material.TexEnvs)
                    {
                        SceneLookTarget texture = materialTarget.File.Target(env.Texture);
                        if (texture.Error != null || texture.File.ClassId(texture.PathId) != SceneLookClass.Texture2D) continue;
                        Touch(touched, texture.File);
                        SceneLookTexture parsed = SceneLookReaderMesh.ParseTexture(texture.File, texture.PathId, texture.File.ReadObject(texture.PathId));
                        line.Slots.Add(new KeyValuePair<string, SceneLookTexture>(env.Name, parsed));
                        if (textureKeys.Add(texture.File.Name + "|" + texture.PathId)) textures.Add(parsed);
                    }
                    if (!materialByName.ContainsKey(line.Name))
                    {
                        materialByName[line.Name] = line;
                        materials.Add(line);
                    }
                }
            }
            Console.WriteLine("Materials");
            foreach (MaterialLine line in materials)
            {
                List<string> parts = new List<string>();
                foreach (KeyValuePair<string, SceneLookTexture> slot in line.Slots) parts.Add(slot.Key + "=" + slot.Value.Name);
                Console.WriteLine("  {0} | shader {1} | file {2} | {3}", line.Name, line.Shader, line.Owner, string.Join(", ", parts));
            }
            Console.WriteLine("Textures");
            long textureBytes = 0;
            long streamBytes = 0;
            foreach (SceneLookTexture texture in textures)
            {
                bool sizeOk = SceneLookReaderMesh.IsSupported(texture.Format) && SceneLookReaderMesh.ExpectedBytes(texture.Format, texture.Width, texture.Height, texture.Mips) == texture.CompleteSize;
                string stream = "inline";
                if (!texture.Inline)
                {
                    stream = string.Format("stream {0} @{1} ({2} bytes, fits file {3})", texture.StreamPath, texture.StreamOffset, texture.StreamSize, files.Installed(texture.StreamPath, texture.StreamOffset + texture.StreamSize));
                    streamBytes += texture.StreamSize;
                }
                textureBytes += texture.CompleteSize;
                Console.WriteLine("  {0} | {1} | {2}x{3} | {4} | {5} mips | {6} bytes | size rule {7} | {8}", texture.Name, texture.FileName, texture.Width, texture.Height, SceneLookReaderMesh.FormatLabel(texture.Format), texture.Mips, texture.CompleteSize, sizeOk, stream);
            }
            long? pentti = null;
            foreach (long candidate in level.Order)
            {
                if (level.ClassId(candidate) == SceneLookClass.GameObject && SceneLookReader.GameObject(level, candidate).Name.StartsWith("Pentti", StringComparison.Ordinal))
                {
                    pentti = candidate;
                    break;
                }
            }
            if (pentti != null)
            {
                HashSet<string> penttiBones = new HashSet<string>();
                foreach (long go in SceneLookReader.Subtree(level, pentti.Value))
                {
                    long renderer = FirstComponent(level, go, SceneLookClass.SkinnedMeshRenderer);
                    if (renderer == 0) continue;
                    foreach (SceneLookRef bone in SceneLookReaderMesh.ParseRenderer(renderer, level.ReadObject(renderer)).Bones) penttiBones.Add(SceneLookReader.BoneName(level.Target(bone)));
                }
                Console.WriteLine("Rig check: Pentti body ({0} bones) shares {1} bone names with Mr. Duck ({2} bones)", penttiBones.Count, rig.Keys.Count(k => penttiBones.Contains(k)), rig.Keys.Count);
            }
            Console.WriteLine("Rig bone names ({0}): {1}", rig.Keys.Count, string.Join(", ", rig.Keys));
            long vertices = 0;
            long triangles = 0;
            long vertexBytes = 0;
            long indexBytes = 0;
            foreach (SceneLookMesh mesh in meshes.Values)
            {
                vertices += mesh.VertexCount;
                triangles += SceneLookReaderMesh.IndexTotal(mesh) / 3;
                vertexBytes += mesh.VertexDataSize;
                indexBytes += mesh.IndexDataSize;
            }
            Console.WriteLine("Totals: {0} meshes, {1} vertices, {2} triangles, vertex bytes {3}, index bytes {4}, texture mip-chain bytes {5}, texture stream bytes {6}", meshes.Count, vertices, triangles, vertexBytes, indexBytes, textureBytes, streamBytes);
            Console.WriteLine("Meshes with buffers matching layout: {0} of {1}", meshOk.Values.Count(ok => ok), meshes.Count);
            List<string> byteParts = new List<string>();
            foreach (string name in touched) byteParts.Add("'" + name + "': " + files.Get(name, out reason).BytesRead);
            Console.WriteLine("Object bytes read: {" + string.Join(", ", byteParts) + "}");
            Stopwatch read = Stopwatch.StartNew();
            long readBytes = 0;
            foreach (SceneLookTexture texture in textures)
            {
                byte[] pixels = SceneLookReaderMesh.TexturePixels(files, texture);
                if (pixels.Length != texture.CompleteSize) throw new InvalidDataException(texture.Name + " has " + pixels.Length + " pixel bytes");
                readBytes += pixels.Length;
            }
            Console.WriteLine("Stream read test: {0} bytes read into memory in {1:F3} s, nothing written", readBytes, read.Elapsed.TotalSeconds);
            Console.WriteLine("Elapsed {0:F2} s", total.Elapsed.TotalSeconds);
            return 0;
        }

        private static void Touch(List<string> touched, SceneLookFile file)
        {
            if (!touched.Contains(file.Name)) touched.Add(file.Name);
        }

        private static int ComponentCount(SceneLookFile file, long go, int classId)
        {
            int count = 0;
            foreach (SceneLookRef component in SceneLookReader.GameObject(file, go).Components)
            {
                if (component.FileId == 0 && file.Contains(component.PathId) && file.ClassId(component.PathId) == classId) count++;
            }
            return count;
        }

        private static long FirstComponent(SceneLookFile file, long go, int classId)
        {
            foreach (SceneLookRef component in SceneLookReader.GameObject(file, go).Components)
            {
                if (component.FileId == 0 && file.Contains(component.PathId) && file.ClassId(component.PathId) == classId) return component.PathId;
            }
            return 0;
        }
    }
}
