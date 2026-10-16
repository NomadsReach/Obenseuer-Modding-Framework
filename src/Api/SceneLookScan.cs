using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ObenseuerCore
{
    internal static class SceneLookScan
    {
        private const string KeptFile = "resources.assets";

        internal sealed class RendererCheck
        {
            public long GameObject;
            public SceneLookRenderer Source;
            public readonly List<string> Bones = new List<string>();
            public readonly List<string> Reasons = new List<string>();
            public readonly List<string> Shaders = new List<string>();
            public readonly List<int> Formats = new List<int>();
            public long Vertices;
            public long Triangles;
        }

        internal sealed class CheckState
        {
            internal readonly Dictionary<string, MeshCheck> Meshes = new Dictionary<string, MeshCheck>();
            internal readonly Dictionary<string, MaterialCheck> Materials = new Dictionary<string, MaterialCheck>();
            internal readonly Dictionary<string, TextureCheck> Textures = new Dictionary<string, TextureCheck>();
        }

        internal sealed class MeshCheck
        {
            public readonly List<string> Reasons = new List<string>();
            public long Vertices;
            public long Triangles;
        }

        internal sealed class MaterialCheck
        {
            public readonly List<string> Reasons = new List<string>();
            public string Shader;
            public readonly List<TextureCheck> Textures = new List<TextureCheck>();
        }

        internal sealed class TextureCheck
        {
            public readonly List<string> Reasons = new List<string>();
            public int Format = -1;
        }

        internal static SceneLookFileScan ScanFile(SceneLookFiles files, string fileName)
        {
            SceneLookFileScan result = new SceneLookFileScan();
            result.FileName = fileName;
            Stopwatch watch = Stopwatch.StartNew();
            try
            {
                string reason;
                SceneLookFile file = files.Get(fileName, out reason);
                if (file == null) result.Error = "unreadable or not installed: " + reason;
                else ScanOpened(files, file, result);
            }
            catch (Exception e)
            {
                result.Error = e.Message;
            }
            finally
            {
                files.CloseAllExcept(KeptFile);
                result.Seconds = watch.Elapsed.TotalSeconds;
            }
            return result;
        }

        internal static List<long> RenderersUnder(SceneLookFile file, long root)
        {
            List<long> found = new List<long>();
            foreach (long pathId in file.Order)
            {
                if (file.ClassId(pathId) == SceneLookClass.SkinnedMeshRenderer && BodyOf(file, pathId) == root) found.Add(pathId);
            }
            return found;
        }

        internal static RendererCheck CheckRenderer(SceneLookFiles files, SceneLookFile file, long pathId, CheckState state)
        {
            RendererCheck check = new RendererCheck();
            SceneLookRenderer renderer;
            try
            {
                renderer = SceneLookReaderMesh.ParseRenderer(pathId, file.ReadObject(pathId));
            }
            catch (Exception)
            {
                check.Reasons.Add("smr layout error");
                return check;
            }
            check.Source = renderer;
            if (file.Contains(renderer.GameObject.PathId)) check.GameObject = renderer.GameObject.PathId;
            foreach (SceneLookRef bone in renderer.Bones)
            {
                SceneLookTarget target = file.Target(bone);
                if (target.Error != null)
                {
                    check.Reasons.Add("bone " + target.Error);
                    continue;
                }
                try
                {
                    check.Bones.Add(SceneLookReader.BoneName(target));
                }
                catch (Exception)
                {
                    check.Reasons.Add("bone layout error");
                }
            }
            SceneLookTarget mesh = file.Target(renderer.Mesh);
            if (mesh.Error != null)
            {
                check.Reasons.Add("mesh " + mesh.Error);
            }
            else
            {
                MeshCheck meshCheck = MeshCheckOf(state, mesh.File, mesh.PathId);
                check.Reasons.AddRange(meshCheck.Reasons);
                check.Vertices = meshCheck.Vertices;
                check.Triangles = meshCheck.Triangles;
            }
            foreach (SceneLookRef material in renderer.Materials)
            {
                SceneLookTarget target = file.Target(material);
                if (target.Error == "null") continue;
                if (target.Error != null)
                {
                    check.Reasons.Add("material " + target.Error);
                    continue;
                }
                MaterialCheck materialCheck = MaterialCheckOf(files, state, target.File, target.PathId);
                check.Reasons.AddRange(materialCheck.Reasons);
                if (materialCheck.Shader != null) check.Shaders.Add(materialCheck.Shader);
                foreach (TextureCheck texture in materialCheck.Textures)
                {
                    check.Reasons.AddRange(texture.Reasons);
                    if (texture.Format >= 0) check.Formats.Add(texture.Format);
                }
            }
            return check;
        }

        private static void ScanOpened(SceneLookFiles files, SceneLookFile file, SceneLookFileScan result)
        {
            CheckState state = new CheckState();
            List<RendererCheck> renderers = new List<RendererCheck>();
            foreach (long pathId in file.Order)
            {
                if (file.ClassId(pathId) == SceneLookClass.SkinnedMeshRenderer) renderers.Add(CheckRenderer(files, file, pathId, state));
            }
            Dictionary<long, List<RendererCheck>> groups = new Dictionary<long, List<RendererCheck>>();
            List<long> bodies = new List<long>();
            List<RendererCheck> unrooted = new List<RendererCheck>();
            foreach (RendererCheck renderer in renderers)
            {
                long body = renderer.GameObject == 0 ? 0 : SceneLookReader.BodyRoot(file, renderer.GameObject);
                if (body == 0)
                {
                    unrooted.Add(renderer);
                    continue;
                }
                if (!groups.ContainsKey(body))
                {
                    groups[body] = new List<RendererCheck>();
                    bodies.Add(body);
                }
                groups[body].Add(renderer);
            }
            foreach (long body in bodies) result.Roots.Add(GroupRoot(file, body, groups[body]));
            foreach (long pathId in SceneLookReader.NpcBehaviours(file)) result.Npcs.Add(ScanNpc(file, pathId));
            AttachNpcs(file, result.Roots, result.Npcs);
            foreach (long pathId in file.Order)
            {
                int classId = file.ClassId(pathId);
                if (classId == SceneLookClass.Texture2D) result.TextureFormats.Add(SceneLookReaderMesh.HeadFormat(file, pathId));
                else if (classId == SceneLookClass.Shader) result.ShaderObjects.Add(SceneLookReaderMesh.ShaderLabel(file, pathId));
            }
            result.Renderers = renderers.Count;
            foreach (RendererCheck renderer in renderers)
            {
                if (renderer.Reasons.Count == 0) result.CleanRenderers++;
                foreach (string reason in new HashSet<string>(renderer.Reasons)) result.ReasonCounts.Add(reason);
                foreach (int format in renderer.Formats) result.FormatsUsed.Add(format);
                foreach (string shader in renderer.Shaders) result.ShadersUsed.Add(shader);
            }
            result.Unrooted = unrooted.Count;
            foreach (RendererCheck renderer in unrooted) result.UnrootedRigs.Add(SceneLookReader.RigKind(new HashSet<string>(renderer.Bones)));
        }

        private static long BodyOf(SceneLookFile file, long pathId)
        {
            long go = new SceneLookCursor(file.ReadObjectHead(pathId, 12), 0).Pptr().PathId;
            return file.Contains(go) ? SceneLookReader.BodyRoot(file, go) : 0;
        }

        private static SceneLookRoot GroupRoot(SceneLookFile file, long body, List<RendererCheck> renderers)
        {
            HashSet<string> names = new HashSet<string>();
            HashSet<string> reasons = new HashSet<string>();
            HashSet<int> formats = new HashSet<int>();
            HashSet<string> shaders = new HashSet<string>();
            long vertices = 0;
            long triangles = 0;
            foreach (RendererCheck renderer in renderers)
            {
                names.UnionWith(renderer.Bones);
                reasons.UnionWith(renderer.Reasons);
                formats.UnionWith(renderer.Formats);
                shaders.UnionWith(renderer.Shaders);
                vertices += renderer.Vertices;
                triangles += renderer.Triangles;
            }
            HashSet<string> kinds = SceneLookReader.Kinds(file, body);
            HashSet<string> hierarchy = new HashSet<string>();
            foreach (long go in SceneLookReader.Subtree(file, body)) hierarchy.Add(SceneLookReader.GameObject(file, go).Name);
            SceneLookRoot root = new SceneLookRoot();
            root.GameObject = body;
            root.Name = SceneLookReader.GameObject(file, body).Name;
            root.Path = SceneLookReader.PathName(file, body);
            root.IsNpc = kinds.Contains("NPCInfo");
            root.HasAnimator = kinds.Contains("Animator");
            root.Renderers = renderers.Count;
            root.BoneNames = Sorted(names);
            root.Rig = SceneLookReader.RigKind(names);
            root.Avatars = new List<SceneLookAvatarInfo>();
            foreach (SceneLookAnimator animator in SceneLookReader.Animators(file, body)) root.Avatars.Add(SceneLookReader.AvatarInfo(file, animator));
            root.Avatar = AvatarSummary(root.Avatars);
            root.Vertices = vertices;
            root.Triangles = triangles;
            root.Reasons = Sorted(reasons);
            root.Clean = root.Reasons.Count == 0;
            root.Formats = new List<int>(formats);
            root.Formats.Sort();
            root.Shaders = Sorted(shaders);
            root.Hierarchy = Sorted(hierarchy);
            return root;
        }

        private static SceneLookNpc ScanNpc(SceneLookFile file, long pathId)
        {
            try
            {
                return SceneLookReader.ReadNpc(file, pathId);
            }
            catch (Exception e)
            {
                SceneLookNpc npc = new SceneLookNpc();
                npc.PathId = pathId;
                npc.Name = "?";
                npc.Path = e.Message;
                return npc;
            }
        }

        private static void AttachNpcs(SceneLookFile file, List<SceneLookRoot> roots, List<SceneLookNpc> npcs)
        {
            Dictionary<long, List<long>> chains = new Dictionary<long, List<long>>();
            foreach (SceneLookRoot root in roots) chains[root.GameObject] = SceneLookReader.Chain(file, root.GameObject);
            HashSet<long> linked = new HashSet<long>();
            foreach (SceneLookNpc npc in npcs)
            {
                foreach (SceneLookRoot root in roots)
                {
                    long body = root.GameObject;
                    bool match = npc.GameObject == body || (npc.ModelGameObject != 0 && (SceneLookReader.Chain(file, npc.ModelGameObject).Contains(body) || chains[body].Contains(npc.ModelGameObject)));
                    if (!match) continue;
                    if (linked.Add(body))
                    {
                        root.Character = npc.Character;
                        root.CharacterNote = npc.CharacterNote;
                    }
                    npc.Body = body;
                    break;
                }
            }
        }

        private static MeshCheck MeshCheckOf(CheckState state, SceneLookFile file, long pathId)
        {
            string key = file.Name + "|" + pathId;
            MeshCheck cached;
            if (state.Meshes.TryGetValue(key, out cached)) return cached;
            MeshCheck check = new MeshCheck();
            if (file.ClassId(pathId) != SceneLookClass.Mesh)
            {
                check.Reasons.Add("mesh pptr not a Mesh");
            }
            else
            {
                try
                {
                    SceneLookMesh mesh = SceneLookReaderMesh.ParseMesh(file, pathId, false);
                    check.Reasons.AddRange(SceneLookReaderMesh.MeshReasons(mesh));
                    check.Vertices = mesh.VertexCount;
                    check.Triangles = SceneLookReaderMesh.IndexTotal(mesh) / 3;
                }
                catch (Exception)
                {
                    check.Reasons.Add("mesh layout error");
                }
            }
            state.Meshes[key] = check;
            return check;
        }

        private static MaterialCheck MaterialCheckOf(SceneLookFiles files, CheckState state, SceneLookFile file, long pathId)
        {
            string key = file.Name + "|" + pathId;
            MaterialCheck cached;
            if (state.Materials.TryGetValue(key, out cached)) return cached;
            MaterialCheck check = new MaterialCheck();
            SceneLookMaterial material = null;
            try
            {
                material = SceneLookReaderMesh.ParseMaterial(file, pathId);
            }
            catch (Exception)
            {
                check.Reasons.Add("material layout error");
            }
            if (material != null)
            {
                SceneLookTarget shader = file.Target(material.Shader);
                if (shader.Error == "null") check.Shader = "null";
                else if (shader.Error != null) check.Reasons.Add("shader " + shader.Error);
                else if (shader.File.ClassId(shader.PathId) != SceneLookClass.Shader) check.Reasons.Add("shader pptr not a Shader");
                else check.Shader = SceneLookReaderMesh.ShaderLabel(shader.File, shader.PathId);
                foreach (SceneLookTexEnv env in material.TexEnvs)
                {
                    SceneLookTarget texture = file.Target(env.Texture);
                    if (texture.Error == "null") continue;
                    if (texture.Error != null)
                    {
                        check.Reasons.Add("texture " + texture.Error);
                        continue;
                    }
                    check.Textures.Add(TextureCheckOf(files, state, texture.File, texture.PathId));
                }
            }
            state.Materials[key] = check;
            return check;
        }

        private static TextureCheck TextureCheckOf(SceneLookFiles files, CheckState state, SceneLookFile file, long pathId)
        {
            string key = file.Name + "|" + pathId;
            TextureCheck cached;
            if (state.Textures.TryGetValue(key, out cached)) return cached;
            TextureCheck check = new TextureCheck();
            if (file.ClassId(pathId) != SceneLookClass.Texture2D)
            {
                check.Reasons.Add("texture pptr not Texture2D");
            }
            else
            {
                try
                {
                    SceneLookTexture texture = SceneLookReaderMesh.ParseTexture(file, pathId, file.ReadObject(pathId));
                    check.Format = texture.Format;
                    check.Reasons.AddRange(SceneLookReaderMesh.TextureReasons(files, texture));
                }
                catch (Exception)
                {
                    check.Reasons.Add("texture layout error");
                }
            }
            state.Textures[key] = check;
            return check;
        }

        private static string AvatarSummary(List<SceneLookAvatarInfo> infos)
        {
            bool humanoid = false;
            bool generic = false;
            foreach (SceneLookAvatarInfo info in infos)
            {
                if (info.Kind == "humanoid") humanoid = true;
                else if (info.Kind == "generic") generic = true;
            }
            return humanoid ? "humanoid" : generic ? "generic" : "none";
        }

        private static List<string> Sorted(ICollection<string> values)
        {
            List<string> list = new List<string>(values);
            list.Sort(StringComparer.Ordinal);
            return list;
        }
    }
}
