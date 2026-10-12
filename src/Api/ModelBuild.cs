using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace ObenseuerCore
{
    internal sealed class ObxModelMark : MonoBehaviour
    {
    }

    internal static class ModelBuild
    {
        private const string HolderName = "ObxModels";
        private const string ErrorPath = "Prefabs/Items/ERROR";
        private const int MaxTextureSide = 2048;
        private const int MaxConvexTriangles = 255;
        private const int MaxUInt16Vertices = 65535;
        internal const float MinColliderEdge = 0.01f;
        private static readonly Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);
        private static readonly string[] ClearedMaps = { "_BumpMap", "_MetallicGlossMap", "_OcclusionMap", "_EmissionMap", "_DetailAlbedoMap" };
        private static readonly string[] DisabledKeywords = { "_NORMALMAP", "_METALLICGLOSSMAP", "_DETAIL_MULX2" };
        private static readonly HashSet<string> diagnosed = new HashSet<string>();
        private static GameObject holder;

        internal static Bounds BoundsOf(float[] xyz)
        {
            Bounds bounds = new Bounds(new Vector3(xyz[0], xyz[1], xyz[2]), Vector3.zero);
            for (int i = 3; i + 2 < xyz.Length; i += 3)
            {
                bounds.Encapsulate(new Vector3(xyz[i], xyz[i + 1], xyz[i + 2]));
            }
            return bounds;
        }

        internal static void Prepare(WorldModel world)
        {
            if (world.Prepared || FindItem(world.TemplateId) == null) return;
            world.Prepared = true;
            string reason;
            try
            {
                reason = Load(world);
            }
            catch (Exception e)
            {
                reason = "exception: " + e.ToString();
            }
            if (reason != null) Refuse(world, reason);
        }

        internal static GameObject CloneFor(WorldModel world, Item item)
        {
            if (world.Clone != null) return world.Clone;
            if (!Ready(world)) return null;
            try
            {
                world.Clone = Instance(world, item);
            }
            catch (Exception e)
            {
                Refuse(world, "clone failed: " + e.ToString());
                return null;
            }
            return world.Clone;
        }

        internal static void SwapInstance(CollectibleItem instance, WorldModel world)
        {
            if (Ready(world)) Swap(instance.gameObject, world, false);
        }

        private static bool Ready(WorldModel world)
        {
            Prepare(world);
            return world.Refusal == null && world.Mesh != null;
        }

        private static string Load(WorldModel world)
        {
            Item template = FindItem(world.TemplateId);
            if (template == null || template.Appearance == null) return "template item " + world.TemplateId + " has no appearance";
            string path;
            GameObject prefab = PrefabOf(template, out path);
            if (prefab == null || path == ErrorPath) return "template prefab " + path + " does not resolve";
            string unsupported = Unsupported(prefab);
            if (unsupported != null) return unsupported;
            MeshRenderer primary = Primary(prefab);
            ModelHandle handle = world.Handle;
            float templateEdge = Largest(RootBounds(prefab.transform, primary).size);
            float modelEdge = Largest(handle.Bounds.size);
            if (templateEdge <= 0f || modelEdge <= 0f) return "the template mesh or the model has no size";
            int triangles = 0;
            foreach (ObjSubMesh sub in handle.Data.SubMeshes)
            {
                triangles += sub.Triangles.Count / 3;
            }
            ModelCollider collider = handle.Options.Collider;
            if (collider == ModelCollider.ConvexMesh && triangles > MaxConvexTriangles)
            {
                CoreLog.For(ModelPatches.Owner).Warn("model " + handle.Key + " has " + triangles + " triangles, a convex collider takes " + MaxConvexTriangles + " or fewer, a box is used");
                collider = ModelCollider.Box;
            }
            float fit = handle.Options.FitToTemplate ? templateEdge / modelEdge : 1f;
            float size = modelEdge * fit * handle.Options.Scale;
            if (float.IsNaN(size) || float.IsInfinity(size)) return "the scaled model size is not finite";
            world.TemplatePath = path;
            world.TemplatePrefab = prefab;
            world.Collider = collider;
            world.Mesh = BuildMesh(handle.Data, handle.Options, fit, handle.Key);
            world.Materials = BuildMaterials(handle, primary.sharedMaterial);
            return null;
        }

        private static Item FindItem(int id)
        {
            List<Item> list = ItemDatabase.database;
            if (list == null) return null;
            foreach (Item item in list)
            {
                if (item.ID == id) return item;
            }
            return null;
        }

        private static GameObject PrefabOf(Item template, out string path)
        {
            path = template.Appearance.PrefabPath;
            if (string.IsNullOrEmpty(path)) return Item.ItemAppearance.GetPrefabByTitle(template.Title, out path);
            GameObject prefab = Resources.Load<GameObject>(path);
            if (prefab == null)
            {
                path = Item.StripPath(path);
                prefab = Resources.Load<GameObject>(path);
            }
            return prefab;
        }

        private static string Unsupported(GameObject prefab)
        {
            if (prefab.GetComponent<CollectibleItem>() == null) return "the prefab root has no CollectibleItem";
            if (prefab.GetComponentInChildren<Storage>(true) != null) return "the prefab has Storage";
            if (prefab.GetComponentInChildren<Animal>(true) != null) return "the prefab has Animal";
            if (prefab.GetComponentInChildren<AnimalStats>(true) != null) return "the prefab has AnimalStats";
            if (prefab.GetComponentInChildren<Pathfinding.AIBase>(true) != null) return "the prefab has AIBase";
            if (Primary(prefab) == null) return "the prefab has no MeshRenderer with a mesh (skinned-only)";
            return null;
        }

        internal static MeshRenderer Primary(GameObject root)
        {
            MeshRenderer best = null;
            float bestSize = -1f;
            foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                MeshFilter filter = renderer.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null) continue;
                float size = filter.sharedMesh.bounds.size.sqrMagnitude;
                if (size > bestSize)
                {
                    best = renderer;
                    bestSize = size;
                }
            }
            return best;
        }

        private static float Largest(Vector3 size)
        {
            return Mathf.Max(size.x, Mathf.Max(size.y, size.z));
        }

        internal static Bounds RootBounds(Transform root, MeshRenderer primary)
        {
            Matrix4x4 chain = Matrix4x4.identity;
            for (Transform node = primary.transform; node != root; node = node.parent)
            {
                chain = Matrix4x4.TRS(node.localPosition, node.localRotation, node.localScale) * chain;
            }
            Bounds local = primary.GetComponent<MeshFilter>().sharedMesh.bounds;
            Vector3 min = local.min;
            Vector3 max = local.max;
            Bounds result = new Bounds(chain.MultiplyPoint3x4(min), Vector3.zero);
            for (int i = 0; i < 8; i++)
            {
                result.Encapsulate(chain.MultiplyPoint3x4(new Vector3((i & 1) == 0 ? min.x : max.x, (i & 2) == 0 ? min.y : max.y, (i & 4) == 0 ? min.z : max.z)));
            }
            return result;
        }

        internal static Mesh BuildMesh(ObjData data, ModelOptions options, float fit, string name)
        {
            int count = data.Positions.Length / 3;
            Quaternion turn = Quaternion.Euler(options.EulerOffset);
            float scale = fit * options.Scale;
            float[] xyz = new float[count * 3];
            for (int i = 0; i < count; i++)
            {
                Vector3 p = turn * new Vector3(data.Positions[i * 3], data.Positions[i * 3 + 1], data.Positions[i * 3 + 2]) * scale;
                xyz[i * 3] = p.x;
                xyz[i * 3 + 1] = p.y;
                xyz[i * 3 + 2] = p.z;
            }
            Vector3 shift = PivotShift(options.Pivot, BoundsOf(xyz)) + options.PositionOffset;
            Vector3[] vertices = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                vertices[i] = new Vector3(xyz[i * 3], xyz[i * 3 + 1], xyz[i * 3 + 2]) + shift;
            }
            Vector3[] normals = null;
            if (data.Normals.Length == count * 3)
            {
                normals = new Vector3[count];
                for (int i = 0; i < count; i++)
                {
                    normals[i] = turn * new Vector3(data.Normals[i * 3], data.Normals[i * 3 + 1], data.Normals[i * 3 + 2]);
                }
            }
            Vector2[] uvs = null;
            if (data.Uvs.Length == count * 2)
            {
                uvs = new Vector2[count];
                for (int i = 0; i < count; i++)
                {
                    uvs[i] = new Vector2(data.Uvs[i * 2], data.Uvs[i * 2 + 1]);
                }
            }
            Mesh mesh = new Mesh();
            mesh.name = name;
            mesh.indexFormat = count > MaxUInt16Vertices ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.vertices = vertices;
            if (normals != null) mesh.normals = normals;
            if (uvs != null) mesh.uv = uvs;
            mesh.subMeshCount = data.SubMeshes.Count;
            for (int i = 0; i < data.SubMeshes.Count; i++)
            {
                mesh.SetTriangles(data.SubMeshes[i].Triangles, i);
            }
            if (normals == null) mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return mesh;
        }

        private static Vector3 PivotShift(ModelPivot pivot, Bounds bounds)
        {
            if (pivot == ModelPivot.Center) return -bounds.center;
            if (pivot == ModelPivot.BottomCenter) return new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z);
            return Vector3.zero;
        }

        internal static Material[] BuildMaterials(ModelHandle handle, Material source)
        {
            List<ObjSubMesh> subs = handle.Data.SubMeshes;
            Material[] materials = new Material[subs.Count];
            for (int i = 0; i < subs.Count; i++)
            {
                materials[i] = MaterialFor(source, Named(handle.Mtl, subs[i].Material), handle.TexturePath);
            }
            return materials;
        }

        private static MtlMaterial Named(MtlData mtl, string name)
        {
            if (mtl == null || name == null) return null;
            foreach (MtlMaterial material in mtl.Materials)
            {
                if (material.Name == name) return material;
            }
            return null;
        }

        private static Material MaterialFor(Material source, MtlMaterial mtl, string texturePath)
        {
            Material material = source != null ? new Material(source) : new Material(Shader.Find("Standard"));
            material.hideFlags = HideFlags.DontUnloadUnusedAsset;
            string path = texturePath ?? (mtl == null ? null : mtl.MapKd);
            Texture2D texture = path == null ? null : Texture(path);
            Texture2D albedo = texture == null ? Texture2D.whiteTexture : texture;
            Color color = mtl == null ? Color.white : new Color(Channel(mtl.Kd[0]), Channel(mtl.Kd[1]), Channel(mtl.Kd[2]), 1f);
            Color emission = mtl == null ? Color.black : new Color(mtl.Ke[0], mtl.Ke[1], mtl.Ke[2], 1f);
            bool emissive = emission.r > 0f || emission.g > 0f || emission.b > 0f;
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", albedo);
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", albedo);
            foreach (string map in ClearedMaps)
            {
                if (material.HasProperty(map)) material.SetTexture(map, null);
            }
            if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", emission);
            foreach (string keyword in DisabledKeywords)
            {
                material.DisableKeyword(keyword);
            }
            if (emissive) material.EnableKeyword("_EMISSION");
            else material.DisableKeyword("_EMISSION");
            return material;
        }

        private static float Channel(float linear)
        {
            return Mathf.LinearToGammaSpace(linear);
        }

        private static Texture2D Texture(string absPath)
        {
            Texture2D texture;
            if (textures.TryGetValue(absPath, out texture)) return texture;
            texture = Decode(absPath);
            textures[absPath] = texture;
            return texture;
        }

        private static Texture2D Decode(string absPath)
        {
            try
            {
                byte[] bytes = File.ReadAllBytes(absPath);
                if (PngOverSize(bytes))
                {
                    CoreLog.For(ModelPatches.Owner).Warn("texture " + absPath + " is over " + MaxTextureSide + " px a side, it is not decoded, the material keeps its colour");
                    return null;
                }
                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, true);
                if (!ImageConversion.LoadImage(texture, bytes, true))
                {
                    UnityEngine.Object.Destroy(texture);
                    CoreLog.For(ModelPatches.Owner).Warn("texture " + absPath + " is not a readable image, the material keeps its colour");
                    return null;
                }
                if (texture.width > MaxTextureSide || texture.height > MaxTextureSide)
                {
                    CoreLog.For(ModelPatches.Owner).Warn("texture " + absPath + " is " + texture.width + "x" + texture.height + ", the limit is " + MaxTextureSide + " px per side, the material keeps its colour");
                    UnityEngine.Object.Destroy(texture);
                    return null;
                }
                texture.filterMode = FilterMode.Bilinear;
                texture.wrapMode = TextureWrapMode.Repeat;
                texture.hideFlags = HideFlags.DontUnloadUnusedAsset;
                return texture;
            }
            catch (Exception e)
            {
                CoreLog.For(ModelPatches.Owner).Warn("texture " + absPath + " failed to load, the material keeps its colour: " + e.ToString());
                return null;
            }
        }

        private static bool PngOverSize(byte[] bytes)
        {
            if (bytes.Length < 24 || bytes[0] != 0x89 || bytes[1] != 0x50 || bytes[2] != 0x4E || bytes[3] != 0x47) return false;
            for (int at = 16; at <= 20; at += 4)
            {
                long side = ((long)bytes[at] << 24) | ((long)bytes[at + 1] << 16) | ((long)bytes[at + 2] << 8) | bytes[at + 3];
                if (side > MaxTextureSide) return true;
            }
            return false;
        }

        private static GameObject Instance(WorldModel world, Item item)
        {
            GameObject clone = UnityEngine.Object.Instantiate(world.TemplatePrefab, Holder().transform);
            try
            {
                clone.name = world.TemplateId == world.ItemId ? world.TemplatePrefab.name : "obxm_" + world.ItemId;
                clone.GetComponent<CollectibleItem>().Item = new ItemReference((Item)null) { ID = world.ItemId, name = item.Title };
                string before = Snapshot(clone);
                MeshRenderer primary = Swap(clone, world, true);
                try
                {
                    Diagnose(clone, primary, world, before);
                }
                catch (Exception e)
                {
                    CoreLog.For(ModelPatches.Owner).Warn("template diagnostic for item " + world.TemplateId + " failed, the model is kept: " + e.ToString());
                }
            }
            catch (Exception)
            {
                UnityEngine.Object.DestroyImmediate(clone);
                throw;
            }
            return clone;
        }

        private static string Snapshot(GameObject clone)
        {
            Transform root = clone.transform;
            List<string> colliders = new List<string>();
            foreach (Collider collider in clone.GetComponentsInChildren<Collider>(true))
            {
                colliders.Add(PathUnder(root, collider.transform));
            }
            return "root euler " + Vec(root.eulerAngles) + ", root scale " + Vec(root.localScale) + ", colliders before swap [" + string.Join(", ", colliders.ToArray()) + "], body before swap [" + BodyOf(clone) + "]";
        }

        private static void Diagnose(GameObject clone, MeshRenderer primary, WorldModel world, string before)
        {
            if (!diagnosed.Add(world.TemplatePath)) return;
            Transform root = clone.transform;
            Bounds bounds = RootBounds(root, primary);
            CoreLog.For(ModelPatches.Owner).Info("template " + world.TemplatePath + " for item " + world.TemplateId + ": " + before + ", after swap: primary " + PathUnder(root, primary.transform) + ", kept " + Kept(clone) + ", mesh bounds in root space centre " + Vec(bounds.center) + " size " + Vec(bounds.size) + ", body [" + BodyOf(clone) + "], colour space " + QualitySettings.activeColorSpace);
        }

        private static string Kept(GameObject root)
        {
            Collider[] colliders = root.GetComponents<Collider>();
            if (colliders.Length != 1) return colliders.Length + " colliders on the root";
            BoxCollider box = colliders[0] as BoxCollider;
            if (box == null) return colliders[0].GetType().Name;
            return "BoxCollider centre " + Vec(box.center) + " size " + Vec(box.size);
        }

        private static string BodyOf(GameObject root)
        {
            Rigidbody body = root.GetComponent<Rigidbody>();
            int others = root.GetComponentsInChildren<Rigidbody>(true).Length - (body == null ? 0 : 1);
            string state = body == null ? "none" : "isKinematic " + body.isKinematic + ", useGravity " + body.useGravity;
            return state + ", child bodies " + others;
        }

        private static string Vec(Vector3 v)
        {
            return string.Format(CultureInfo.InvariantCulture, "({0:F4}, {1:F4}, {2:F4})", v.x, v.y, v.z);
        }

        private static string PathUnder(Transform root, Transform node)
        {
            if (node == root) return ".";
            string path = node.name;
            for (Transform t = node.parent; t != root; t = t.parent) path = t.name + "/" + path;
            return path;
        }

        private static GameObject Holder()
        {
            if (holder == null)
            {
                holder = new GameObject(HolderName);
                holder.SetActive(false);
                UnityEngine.Object.DontDestroyOnLoad(holder);
            }
            return holder;
        }

        private static MeshRenderer Swap(GameObject root, WorldModel world, bool clone)
        {
            MeshRenderer primary = Primary(root);
            Transform node = primary.transform;
            if (clone) root.transform.localRotation = Quaternion.identity;
            if (node != root.transform)
            {
                node.localPosition = Vector3.zero;
                node.localRotation = Quaternion.identity;
                node.localScale = Vector3.one;
            }
            foreach (ItemSkin skin in root.GetComponentsInChildren<ItemSkin>(true))
            {
                UnityEngine.Object.DestroyImmediate(skin);
            }
            foreach (CollectibleItemColors colors in root.GetComponentsInChildren<CollectibleItemColors>(true))
            {
                UnityEngine.Object.DestroyImmediate(colors);
            }
            primary.GetComponent<MeshFilter>().sharedMesh = world.Mesh;
            primary.sharedMaterials = world.Materials;
            foreach (MeshRenderer other in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (other != primary) other.enabled = false;
            }
            foreach (LODGroup group in root.GetComponentsInChildren<LODGroup>(true))
            {
                UnityEngine.Object.DestroyImmediate(group);
            }
            Collide(root, primary, world);
            if (root.GetComponent<ObxModelMark>() == null) root.AddComponent<ObxModelMark>();
            return primary;
        }

        private static void Collide(GameObject root, MeshRenderer primary, WorldModel world)
        {
            if (world.Collider == ModelCollider.Template) return;
            Collider keep;
            if (world.Collider == ModelCollider.ConvexMesh)
            {
                MeshCollider convex = root.GetComponent<MeshCollider>();
                if (convex == null) convex = root.AddComponent<MeshCollider>();
                convex.sharedMesh = world.Mesh;
                convex.convex = true;
                convex.isTrigger = false;
                keep = convex;
            }
            else
            {
                Bounds bounds = RootBounds(root.transform, primary);
                BoxCollider box = root.GetComponent<BoxCollider>();
                if (box == null) box = root.AddComponent<BoxCollider>();
                box.center = bounds.center;
                box.size = Vector3.Max(bounds.size, Vector3.one * MinColliderEdge);
                box.isTrigger = false;
                keep = box;
            }
            foreach (Collider collider in root.GetComponents<Collider>())
            {
                if (collider != keep) UnityEngine.Object.DestroyImmediate(collider);
            }
            foreach (Collider collider in primary.GetComponents<Collider>())
            {
                if (collider != keep) UnityEngine.Object.DestroyImmediate(collider);
            }
        }

        private static void Refuse(WorldModel world, string reason)
        {
            world.Refusal = reason;
            CoreLog.For(ModelPatches.Owner).Warn("model " + world.Handle.Key + " for item " + world.ItemId + " refused, the template look is kept: " + reason);
        }
    }
}
