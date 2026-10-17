using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ObenseuerCore
{
    internal sealed class WorldPropBuilt
    {
        internal GameObject Go;
        internal string Reason;
    }

    internal sealed class WorldPropsModelAssets : MonoBehaviour
    {
        private Mesh mesh;
        private Material[] materials;

        internal void Hold(Mesh value, Material[] values)
        {
            mesh = value;
            materials = values;
        }

        private void OnDestroy()
        {
            Mesh ownedMesh = mesh;
            Material[] ownedMaterials = materials;
            mesh = null;
            materials = null;
            try
            {
                if (ownedMesh != null) UnityEngine.Object.DestroyImmediate(ownedMesh);
                if (ownedMaterials != null)
                {
                    for (int i = 0; i < ownedMaterials.Length; i++)
                    {
                        if (ownedMaterials[i] != null) UnityEngine.Object.DestroyImmediate(ownedMaterials[i]);
                    }
                }
            }
            catch (Exception e)
            {
                CoreLog.For(WorldProps.Owner).Error("the mesh or the materials of a model prop could not be freed: " + e.ToString());
            }
        }
    }

    internal static class WorldPropsBuild
    {
        internal static string Check(WorldPropSource source)
        {
            if (source.Kind == WorldPropKind.Model)
            {
                ModelHandle handle = Handle(source.Key);
                if (handle == null) return "the model " + source.Key + " is not loaded";
                return handle.IsReady ? null : "the model " + source.Key + " is not ready: " + handle.Error;
            }
            if (source.Kind == WorldPropKind.Furniture)
            {
                return Building.PrefabOf(source.Key, source.Alt) == null ? "the furniture " + source.Key + " has no prefab" : null;
            }
            if (source.Kind == WorldPropKind.Item)
            {
                return ItemRegistry.FindInDatabase(source.Alt) == null ? "there is no item with id " + source.Alt : null;
            }
            return null;
        }

        internal static WorldPropBuilt Build(SavedPropEntry entry)
        {
            WorldPropBuilt built = new WorldPropBuilt();
            try
            {
                WorldPropKind kind = (WorldPropKind)entry.Kind;
                if (kind == WorldPropKind.Model) Model(entry, built);
                else if (kind == WorldPropKind.Primitive) Primitive(entry, built);
                else if (kind == WorldPropKind.Furniture) Prefab(entry, Building.PrefabOf(entry.Key, entry.Alt), built);
                else if (kind == WorldPropKind.Item) Item(entry, built);
                else if (kind == WorldPropKind.Light) Light(entry, built);
                else if (kind == WorldPropKind.Anchor) Anchor(entry, built);
                else built.Reason = "the saved prop kind " + entry.Kind + " is unknown";
                if (built.Go == null) return built;
                Finish(entry, built.Go);
                return built;
            }
            catch (Exception e)
            {
                if (built.Go != null)
                {
                    UnityEngine.Object.Destroy(built.Go);
                    built.Go = null;
                }
                built.Reason = "the prop could not be built: " + e.ToString();
                return built;
            }
        }

        internal static bool Strip(GameObject go)
        {
            int before = MonoCount(go);
            while (before > 0)
            {
                MonoBehaviour[] parts = go.GetComponentsInChildren<MonoBehaviour>(true);
                for (int i = parts.Length - 1; i >= 0; i--)
                {
                    MonoBehaviour part = parts[i];
                    if (part == null) continue;
                    string name = part.GetType().Name;
                    try
                    {
                        UnityEngine.Object.DestroyImmediate(part);
                    }
                    catch (Exception e)
                    {
                        ApiLog.Once(WorldProps.Owner, "WorldProps strip " + name + " " + go.name, "the component " + name + " could not be removed from " + go.name + ": " + e.Message);
                    }
                }
                int after = MonoCount(go);
                if (after == 0 || after >= before) break;
                before = after;
            }
            foreach (Animator animator in go.GetComponentsInChildren<Animator>(true)) animator.fireEvents = false;
            if (MonoCount(go) == 0) return true;
            foreach (MonoBehaviour part in go.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (part == null) continue;
                string name = part.GetType().Name;
                ApiLog.Once(WorldProps.Owner, "WorldProps strip " + name + " " + go.name, "the component " + name + " survives on " + go.name + ", so the prop is refused");
            }
            return false;
        }

        internal static void EnsureCollider(GameObject go)
        {
            if (go.GetComponent<Collider>() != null) return;
            BoxCollider box = go.AddComponent<BoxCollider>();
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer == null)
            {
                box.size = Vector3.one;
                return;
            }
            box.center = renderer.localBounds.center;
            box.size = renderer.localBounds.size;
        }

        private static void Model(SavedPropEntry entry, WorldPropBuilt built)
        {
            ModelHandle handle = Handle(entry.Key);
            if (handle == null || !handle.IsReady)
            {
                built.Reason = "the model " + entry.Key + " is not loaded or not ready";
                return;
            }
            Mesh mesh = handle.CreateMesh();
            if (mesh == null)
            {
                built.Reason = "the mesh of the model " + entry.Key + " could not be built";
                return;
            }
            Material source = Source();
            if (source == null)
            {
                UnityEngine.Object.DestroyImmediate(mesh);
                built.Reason = "there is no shader to build a model material from";
                return;
            }
            Material[] materials;
            try
            {
                materials = handle.CreateMaterials(source);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(source);
            }
            if (materials == null || materials.Length == 0)
            {
                UnityEngine.Object.DestroyImmediate(mesh);
                built.Reason = "the materials of the model " + entry.Key + " could not be built";
                return;
            }
            GameObject go = new GameObject(Name(entry));
            MeshFilter filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = materials;
            go.AddComponent<WorldPropsModelAssets>().Hold(mesh, materials);
            if (entry.Collider)
            {
                BoxCollider box = go.AddComponent<BoxCollider>();
                box.center = mesh.bounds.center;
                box.size = mesh.bounds.size;
            }
            built.Go = go;
        }

        private static void Primitive(SavedPropEntry entry, WorldPropBuilt built)
        {
            GameObject go = GameObject.CreatePrimitive(Shape(entry.Alt));
            go.name = Name(entry);
            if (!entry.Collider) NoColliders(go);
            built.Go = go;
        }

        private static void Prefab(SavedPropEntry entry, GameObject prefab, WorldPropBuilt built)
        {
            if (prefab == null)
            {
                built.Reason = "the prop " + Name(entry) + " has no prefab";
                return;
            }
            GameObject go = UnityEngine.Object.Instantiate(prefab, ModelBuild.Holder().transform);
            go.name = Name(entry);
            if (!Strip(go))
            {
                UnityEngine.Object.Destroy(go);
                built.Reason = "the prefab of " + Name(entry) + " keeps a component the framework cannot remove, so it is refused";
                return;
            }
            if (!entry.Collider) NoColliders(go);
            built.Go = go;
        }

        private static void Item(SavedPropEntry entry, WorldPropBuilt built)
        {
            Item item = ItemRegistry.FindInDatabase(entry.Alt);
            if (item == null)
            {
                built.Reason = "there is no item with id " + entry.Alt;
                return;
            }
            GameObject prefab = item.Prefab;
            if (prefab == null)
            {
                built.Reason = "item " + entry.Alt + " has no world prefab";
                return;
            }
            Prefab(entry, prefab, built);
        }

        private static void Light(SavedPropEntry entry, WorldPropBuilt built)
        {
            GameObject go = new GameObject(Name(entry));
            UnityEngine.Light light = go.AddComponent<UnityEngine.Light>();
            light.type = (LightType)entry.LightKind;
            light.color = entry.LightColor;
            light.range = entry.LightRange;
            light.intensity = entry.LightIntensity;
            if (light.type == LightType.Spot) light.spotAngle = entry.LightSpotAngle;
            built.Go = go;
        }

        private static void Anchor(SavedPropEntry entry, WorldPropBuilt built)
        {
            GameObject go = new GameObject(Name(entry));
            if (entry.Collider) EnsureCollider(go);
            built.Go = go;
        }

        internal static void Settle(GameObject go)
        {
            foreach (Rigidbody body in go.GetComponentsInChildren<Rigidbody>(true))
            {
                if (body == null) continue;
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.isKinematic = true;
                body.useGravity = false;
            }
            foreach (AudioSource source in go.GetComponentsInChildren<AudioSource>(true))
            {
                if (source == null) continue;
                source.playOnAwake = false;
                source.Stop();
            }
            foreach (ParticleSystem system in go.GetComponentsInChildren<ParticleSystem>(true))
            {
                if (system == null) continue;
                ParticleSystem.MainModule main = system.main;
                main.playOnAwake = false;
                system.Stop();
            }
        }

        private static void Finish(SavedPropEntry entry, GameObject go)
        {
            Settle(go);
            go.transform.SetParent(null, false);
            go.transform.localPosition = entry.Position;
            go.transform.localRotation = entry.Rotation;
            go.transform.localScale = entry.Scale;
            go.layer = entry.Layer;
            if (entry.Static) go.isStatic = true;
            SceneManager.MoveGameObjectToScene(go, SceneManager.GetSceneByName(entry.Scene));
            go.SetActive(true);
        }

        private static void NoColliders(GameObject go)
        {
            foreach (Collider collider in go.GetComponentsInChildren<Collider>(true))
            {
                if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
            }
        }

        private static int MonoCount(GameObject go)
        {
            int count = 0;
            foreach (MonoBehaviour part in go.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (part != null) count++;
            }
            return count;
        }

        private static Material Source()
        {
            Shader shader = Shader.Find("Standard");
            if (shader == null) return null;
            Material material = new Material(shader);
            material.name = "world prop source";
            return material;
        }

        private static ModelHandle Handle(string key)
        {
            string modGuid;
            string shortKey;
            if (!SavedPropFile.SplitKey(key, out modGuid, out shortKey)) return null;
            return ModelRegistry.Get(modGuid, shortKey);
        }

        private static string Name(SavedPropEntry entry)
        {
            return entry.Mod + "/" + entry.PropId;
        }

        private static PrimitiveType Shape(int primitive)
        {
            if (primitive == (int)WorldPrimitive.Sphere) return PrimitiveType.Sphere;
            if (primitive == (int)WorldPrimitive.Cylinder) return PrimitiveType.Cylinder;
            if (primitive == (int)WorldPrimitive.Plane) return PrimitiveType.Plane;
            return PrimitiveType.Cube;
        }
    }
}
