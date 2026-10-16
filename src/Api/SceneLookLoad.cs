using System;
using System.Collections.Generic;

namespace ObenseuerCore
{
    internal static class SceneLookLoad
    {
        internal static SceneLookLook Load(SceneLookFiles files, string fileName, long root, out string reason)
        {
            reason = null;
            SceneLookFile file = files.Get(fileName, out reason);
            if (file == null) return null;
            if (!file.Contains(root))
            {
                reason = "root not found";
                return null;
            }
            List<long> renderers = SceneLookScan.RenderersUnder(file, root);
            if (renderers.Count == 0)
            {
                reason = "no skinned renderers under the root";
                return null;
            }
            SceneLookScan.CheckState state = new SceneLookScan.CheckState();
            SceneLookLook look = new SceneLookLook();
            look.FileName = fileName;
            look.Root = root;
            look.Files = files;
            look.Nodes = Nodes(file, root);
            look.Renderers = new List<SceneLookLookRenderer>();
            look.Textures = new List<SceneLookTexture>();
            Dictionary<string, SceneLookTexture> textures = new Dictionary<string, SceneLookTexture>();
            foreach (long pathId in renderers)
            {
                SceneLookScan.RendererCheck check = SceneLookScan.CheckRenderer(files, file, pathId, state);
                if (check.Reasons.Count > 0)
                {
                    reason = "renderer " + pathId + " refused: " + string.Join("; ", check.Reasons);
                    return null;
                }
                SceneLookLookRenderer item = new SceneLookLookRenderer();
                item.Name = SceneLookReader.GameObject(file, check.GameObject).Name;
                item.Source = check.Source;
                SceneLookTarget meshTarget = file.Target(check.Source.Mesh);
                item.Mesh = SceneLookReaderMesh.ParseMesh(meshTarget.File, meshTarget.PathId, true);
                item.Materials = new List<SceneLookMaterial>();
                foreach (SceneLookRef materialRef in check.Source.Materials)
                {
                    SceneLookTarget materialTarget = file.Target(materialRef);
                    if (materialTarget.Error == "null")
                    {
                        item.Materials.Add(null);
                        continue;
                    }
                    SceneLookMaterial material = SceneLookReaderMesh.ParseMaterial(materialTarget.File, materialTarget.PathId);
                    item.Materials.Add(material);
                    foreach (SceneLookTexEnv env in material.TexEnvs)
                    {
                        SceneLookTarget texture = materialTarget.File.Target(env.Texture);
                        if (texture.Error == "null") continue;
                        string key = texture.File.Name + "|" + texture.PathId;
                        SceneLookTexture resolved;
                        if (!textures.TryGetValue(key, out resolved))
                        {
                            resolved = SceneLookReaderMesh.ParseTexture(texture.File, texture.PathId, texture.File.ReadObject(texture.PathId));
                            textures[key] = resolved;
                            look.Textures.Add(resolved);
                        }
                        env.Resolved = resolved;
                    }
                }
                item.Bones = new List<SceneLookBone>();
                foreach (SceneLookRef boneRef in check.Source.Bones)
                {
                    if (file.Target(boneRef).File != file)
                    {
                        reason = "bone outside the look file";
                        return null;
                    }
                    item.Bones.Add(Bone(file, boneRef));
                }
                SceneLookTarget rootBone = file.Target(check.Source.RootBone);
                if (rootBone.Error == "null")
                {
                    item.RootBone = null;
                }
                else if (rootBone.Error != null || rootBone.File != file)
                {
                    reason = "root bone " + (rootBone.Error ?? "outside the look file");
                    return null;
                }
                else
                {
                    item.RootBone = Bone(file, check.Source.RootBone);
                }
                look.Renderers.Add(item);
            }
            return look;
        }

        private static List<SceneLookNode> Nodes(SceneLookFile file, long root)
        {
            List<SceneLookNode> nodes = new List<SceneLookNode>();
            foreach (long transformPid in SceneLookReader.SubtreeTransforms(file, root))
            {
                SceneLookTransform transform = SceneLookReader.Transform(file, transformPid);
                SceneLookNode node = new SceneLookNode();
                node.GameObject = transform.GameObject.PathId;
                node.Transform = transformPid;
                node.Name = SceneLookReader.GameObject(file, node.GameObject).Name;
                node.Father = transform.Father;
                node.Children = transform.Children;
                node.Rotation = transform.Rotation;
                node.Position = transform.Position;
                node.Scale = transform.Scale;
                nodes.Add(node);
            }
            return nodes;
        }

        private static SceneLookBone Bone(SceneLookFile file, SceneLookRef transform)
        {
            SceneLookBone bone = new SceneLookBone();
            bone.Name = SceneLookReader.BoneName(file.Target(transform));
            bone.Transform = transform;
            return bone;
        }
    }
}
