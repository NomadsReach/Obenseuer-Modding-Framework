using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace ObenseuerCore
{
    internal static class FurnitureBuild
    {
        private static readonly Dictionary<string, Furniture> placeholders = new Dictionary<string, Furniture>();
        private static readonly Dictionary<ModelHandle, Mesh> meshes = new Dictionary<ModelHandle, Mesh>();
        private static GameObject holder;

        internal static bool Started;

        internal static Furniture Resolve(Furniture saved)
        {
            Started = true;
            FurnitureEntry entry = FurnitureRegistry.Find(saved.id);
            Furniture built = entry == null ? null : Registered(entry);
            return built != null ? built : Placeholder(saved);
        }

        internal static Furniture Registered(FurnitureEntry entry)
        {
            Started = true;
            if (entry.Built != null || entry.BuildFailed) return entry.Built;
            try
            {
                entry.Built = Build(entry);
            }
            catch (Exception e)
            {
                ApiLog.Once(FurnitureRegistry.Owner, "build " + entry.Id, e);
            }
            entry.BuildFailed = entry.Built == null;
            return entry.Built;
        }

        internal static void ForgetFailures()
        {
            foreach (FurnitureEntry entry in FurnitureRegistry.All()) entry.BuildFailed = false;
        }

        internal static Vector3 AlignOffset(Bounds model, Bounds template)
        {
            return new Vector3(template.center.x - model.center.x, template.min.y - model.min.y, template.center.z - model.center.z);
        }

        internal static bool CompensatedScale(Vector3 root, Vector3 child, out Vector3 scale)
        {
            scale = Vector3.one;
            if (IsZero(root) || IsZero(child)) return false;
            scale = new Vector3(root.x / child.x, root.y / child.y, root.z / child.z);
            return true;
        }

        internal static Bounds ScaledBounds(Bounds bounds, Vector3 scale)
        {
            return new Bounds(Vector3.Scale(bounds.center, scale), Vector3.Scale(bounds.size, new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z))));
        }

        internal static void ApplyModel(GameObject prefabRoot, ModelHandle model, Bounds[] colliders)
        {
            if (prefabRoot == null || model == null) return;
            GameObject part = null;
            Material[] materials = null;
            List<BoxCollider> boxes = new List<BoxCollider>();
            List<Renderer> hidden = new List<Renderer>();
            List<Collider> disabled = new List<Collider>();
            try
            {
                if (prefabRoot.transform.childCount == 0)
                {
                    CoreLog.For(FurnitureRegistry.Owner).Warn("prefab " + prefabRoot.name + " has no child 0, its template look is kept");
                    return;
                }
                Transform target = prefabRoot.transform.GetChild(0);
                Bounds template;
                if (!VisibleBounds(target, out template))
                {
                    CoreLog.For(FurnitureRegistry.Owner).Warn("prefab " + prefabRoot.name + " has no enabled mesh under child 0, its template look is kept");
                    return;
                }
                Mesh mesh = MeshOf(model);
                materials = ModelBuild.BuildMaterials(model, ModelBuild.Primary(target.gameObject).sharedMaterial);
                Vector3 scale;
                if (!CompensatedScale(prefabRoot.transform.lossyScale, target.lossyScale, out scale))
                {
                    ApiLog.Once(FurnitureRegistry.Owner, "scale " + model.Key, new InvalidOperationException("prefab " + prefabRoot.name + " has a zero scale on the root or child 0, the model keeps scale one"));
                }
                List<Collider> solids = new List<Collider>();
                List<Renderer> renderers = new List<Renderer>();
                int layer = TemplateParts(prefabRoot, target, solids, renderers);
                List<Transform> owners = new List<Transform>();
                List<Collider> sources = new List<Collider>();
                Owners(solids, target, owners, sources);
                if (colliders != null && owners.Count > 1) ApiLog.Once(FurnitureRegistry.Owner, "colliders " + model.Key, "prefab " + prefabRoot.name + " has " + owners.Count + " solid owners: Colliders go on one of them and the others get no collider");
                CustomBoxes(colliders, target, owners, sources);
                part = new GameObject("obx_model");
                part.layer = layer;
                part.transform.SetParent(target, false);
                part.transform.localScale = scale;
                part.transform.localPosition = AlignOffset(ScaledBounds(mesh.bounds, scale), template);
                part.AddComponent<MeshFilter>().sharedMesh = mesh;
                part.AddComponent<MeshRenderer>().sharedMaterials = materials;
                List<Bounds> fits = new List<Bounds>();
                for (int i = 0; i < owners.Count; i++) fits.Add(BoxIn(part.transform, colliders == null ? mesh.bounds : colliders[i], owners[i]));
                foreach (Renderer renderer in renderers)
                {
                    hidden.Add(renderer);
                    renderer.enabled = false;
                }
                foreach (Collider collider in solids)
                {
                    disabled.Add(collider);
                    collider.enabled = false;
                }
                for (int i = 0; i < owners.Count; i++)
                {
                    BoxCollider box = owners[i].gameObject.AddComponent<BoxCollider>();
                    box.center = fits[i].center;
                    box.size = fits[i].size;
                    if (sources[i] != null) box.sharedMaterial = sources[i].sharedMaterial;
                    boxes.Add(box);
                }
            }
            catch (Exception e)
            {
                Undo(part, materials, boxes, hidden, disabled);
                ApiLog.Once(FurnitureRegistry.Owner, "model " + model.Key, e);
            }
        }

        private static bool VisibleBounds(Transform target, out Bounds bounds)
        {
            bounds = new Bounds();
            bool found = false;
            foreach (MeshRenderer renderer in target.GetComponentsInChildren<MeshRenderer>(true))
            {
                MeshFilter filter = renderer.GetComponent<MeshFilter>();
                if (!renderer.enabled || filter == null || filter.sharedMesh == null) continue;
                Bounds part = ModelBuild.RootBounds(target, renderer);
                if (found) bounds.Encapsulate(part);
                else bounds = part;
                found = true;
            }
            return found;
        }

        private static int TemplateParts(GameObject prefabRoot, Transform target, List<Collider> solids, List<Renderer> renderers)
        {
            int layer = target.gameObject.layer;
            bool layerFound = false;
            int outsideColliders = 0;
            foreach (Collider collider in prefabRoot.GetComponentsInChildren<Collider>(true))
            {
                if (collider.isTrigger || !collider.enabled) continue;
                solids.Add(collider);
                if (collider.transform.IsChildOf(target))
                {
                    if (!layerFound) layer = collider.gameObject.layer;
                    layerFound = true;
                }
                else
                {
                    outsideColliders++;
                }
            }
            int outsideRenderers = 0;
            foreach (Renderer renderer in prefabRoot.GetComponentsInChildren<Renderer>(true))
            {
                if (!renderer.enabled) continue;
                if (renderer.transform.IsChildOf(target)) renderers.Add(renderer);
                else outsideRenderers++;
            }
            if (outsideColliders > 0) CoreLog.For(FurnitureRegistry.Owner).Warn("prefab " + prefabRoot.name + ": " + outsideColliders + " non-trigger collider(s) outside child 0 are disabled");
            if (outsideRenderers > 0) CoreLog.For(FurnitureRegistry.Owner).Warn("prefab " + prefabRoot.name + ": " + outsideRenderers + " renderer(s) outside child 0 stay visible");
            return layer;
        }

        private static void Owners(List<Collider> solids, Transform target, List<Transform> owners, List<Collider> sources)
        {
            foreach (Collider collider in solids)
            {
                if (owners.Contains(collider.transform)) continue;
                owners.Add(collider.transform);
                sources.Add(collider);
            }
            if (owners.Count == 0)
            {
                owners.Add(target);
                sources.Add(null);
            }
        }

        private static void CustomBoxes(Bounds[] colliders, Transform target, List<Transform> owners, List<Collider> sources)
        {
            if (colliders == null) return;
            int index = -1;
            for (int i = 0; i < owners.Count && index < 0; i++)
            {
                if (owners[i] == target || owners[i].IsChildOf(target)) index = i;
            }
            Transform owner = index < 0 ? target : owners[index];
            Collider source = index < 0 ? null : sources[index];
            owners.Clear();
            sources.Clear();
            for (int i = 0; i < colliders.Length; i++)
            {
                owners.Add(owner);
                sources.Add(source);
            }
        }

        private static Bounds BoxIn(Transform part, Bounds model, Transform owner)
        {
            Matrix4x4 move = owner.worldToLocalMatrix * part.localToWorldMatrix;
            Vector3 min = model.min;
            Vector3 max = model.max;
            Bounds result = new Bounds(move.MultiplyPoint3x4(min), Vector3.zero);
            for (int i = 0; i < 8; i++)
            {
                result.Encapsulate(move.MultiplyPoint3x4(new Vector3((i & 1) == 0 ? min.x : max.x, (i & 2) == 0 ? min.y : max.y, (i & 4) == 0 ? min.z : max.z)));
            }
            result.size = Vector3.Max(result.size, Vector3.one * ModelBuild.MinColliderEdge);
            return result;
        }

        private static void Undo(GameObject part, Material[] materials, List<BoxCollider> boxes, List<Renderer> hidden, List<Collider> disabled)
        {
            foreach (BoxCollider box in boxes) UnityEngine.Object.DestroyImmediate(box);
            if (part != null) UnityEngine.Object.DestroyImmediate(part);
            if (materials != null)
            {
                foreach (Material material in materials) UnityEngine.Object.DestroyImmediate(material);
            }
            foreach (Renderer renderer in hidden) renderer.enabled = true;
            foreach (Collider collider in disabled) collider.enabled = true;
        }

        private static bool IsZero(Vector3 v)
        {
            return v.x == 0f || v.y == 0f || v.z == 0f;
        }

        private static Mesh MeshOf(ModelHandle model)
        {
            Mesh mesh;
            if (!meshes.TryGetValue(model, out mesh))
            {
                mesh = ModelBuild.BuildMesh(model.Data, model.Options, 1f, model.Key);
                meshes[model] = mesh;
            }
            return mesh;
        }

        private static ModelHandle UsableModel(FurnitureEntry entry)
        {
            ModelHandle model = entry.Definition.Model;
            if (model == null || model.IsReady) return model;
            CoreLog.For(FurnitureRegistry.Owner).Error("furniture " + entry.Id + " keeps the template mesh, model " + model.Key + " is not ready: " + model.Error);
            return null;
        }

        private static Furniture Build(FurnitureEntry entry)
        {
            FurnitureDefinition definition = entry.Definition;
            Furniture template = Template(definition);
            string path = string.IsNullOrEmpty(definition.TemplatePath) ? template.addressableAssetPath : definition.TemplatePath;
            if (string.IsNullOrEmpty(path))
            {
                ApiLog.Once(FurnitureRegistry.Owner, "path " + entry.Id, new InvalidOperationException("template " + definition.TemplateName + " has no addressable path"));
                return null;
            }
            string title = definition.Title ?? template.title;
            Sprite image = definition.Icon ?? template.image;
            if (string.IsNullOrEmpty(title) || image == null) throw Refuse(definition, template, "has no title or image");
            Furniture furniture = UnityEngine.Object.Instantiate(template);
            try
            {
                furniture.id = entry.Id;
                furniture.addressableAssetPath = path;
                furniture.title = title;
                furniture.name = title;
                furniture.details = definition.Details ?? template.details;
                furniture.priceOC = definition.PriceOC < 0 ? template.priceOC : definition.PriceOC;
                furniture.priceRM = definition.PriceRM < 0 ? template.priceRM : definition.PriceRM;
                furniture.image = image;
                furniture.dismantleItems = template.dismantleItems == null ? new List<Furniture.ReseourceItem>() : new List<Furniture.ReseourceItem>(template.dismantleItems);
                furniture.skins = new Furniture.Skin[0];
                furniture.alternativePrefabs = new Furniture.AlternativePrefab[0];
                furniture.hideFlags = HideFlags.HideAndDontSave;
                furniture.category = FurnitureCategories.CategoryFor(definition.CategoryName, template.category);
                furniture.prefab = Clone(template.prefab);
                furniture.previewPrefab = template.previewPrefab == template.prefab ? furniture.prefab : Clone(template.previewPrefab);
                furniture.customDisplayPrefab = Clone(template.customDisplayPrefab);
                if (definition.Placement != FurniturePlacement.Template) SetPlacement(furniture.previewPrefab, definition.Placement, entry.Id);
                if (definition.Placement == FurniturePlacement.Ceiling) furniture.displayStyle = Furniture.DisplayStyle.Ceiling;
                furniture.prefab.GetComponent<FurniturePlaceable>().furniture = furniture;
                ModelHandle model = UsableModel(entry);
                ApplyModel(furniture.prefab, model, definition.Colliders);
                if (furniture.previewPrefab != furniture.prefab) ApplyModel(furniture.previewPrefab, model, definition.Colliders);
                ApplyModel(furniture.customDisplayPrefab, model, definition.Colliders);
                return furniture;
            }
            catch (Exception)
            {
                Discard(furniture, template);
                ReleaseTemplate(definition, template);
                throw;
            }
        }

        private static void Discard(Furniture furniture, Furniture template)
        {
            GameObject[] clones = { furniture.prefab, furniture.previewPrefab, furniture.customDisplayPrefab };
            foreach (GameObject clone in clones)
            {
                if (clone != null && clone != template.prefab && clone != template.previewPrefab && clone != template.customDisplayPrefab) UnityEngine.Object.DestroyImmediate(clone);
            }
            UnityEngine.Object.DestroyImmediate(furniture);
        }

        private static void SetPlacement(GameObject preview, FurniturePlacement placement, string id)
        {
            ObjectPreview objectPreview = preview == null ? null : preview.GetComponentInChildren<ObjectPreview>(true);
            if (objectPreview == null)
            {
                ApiLog.Once(FurnitureRegistry.Owner, "placement " + id, "the preview prefab has no ObjectPreview, Placement is ignored and the template's orientation is kept");
                return;
            }
            objectPreview.SetOrientationType(PlaceTypeOf(placement));
        }

        private static PlaceType PlaceTypeOf(FurniturePlacement placement)
        {
            switch (placement)
            {
                case FurniturePlacement.Floor: return PlaceType.floor;
                case FurniturePlacement.Wall: return PlaceType.wall;
                case FurniturePlacement.Ceiling: return PlaceType.ceiling;
                case FurniturePlacement.FloorAndWall: return PlaceType.floorAndWall;
                default: return PlaceType.all;
            }
        }

        internal static Furniture Template(FurnitureDefinition definition)
        {
            ScriptableObject asset = string.IsNullOrEmpty(definition.TemplatePath) ? FindTemplate(definition.TemplateName) : SavableScriptableObject.LoadFromPath(definition.TemplatePath);
            Furniture template = asset as Furniture;
            if (template == null || template is FurnitureSurface || template is Train) throw Refuse(definition, asset, "is missing, or is a surface or train");
            if (template.prefab == null || template.prefab.transform.childCount == 0 || template.prefab.GetComponent<FurniturePlaceable>() == null) throw Refuse(definition, asset, "has no placeable prefab with a model child");
            return template;
        }

        internal static void ReleaseTemplate(FurnitureDefinition definition, ScriptableObject asset)
        {
            if (asset != null && !string.IsNullOrEmpty(definition.TemplatePath)) Addressables.Release(asset);
        }

        private static InvalidOperationException Refuse(FurnitureDefinition definition, ScriptableObject asset, string reason)
        {
            ReleaseTemplate(definition, asset);
            return new InvalidOperationException("template " + (definition.TemplatePath ?? definition.TemplateName) + " " + reason);
        }

        private static ScriptableObject FindTemplate(string name)
        {
            DeveloperConsole console = DeveloperConsole.Instance;
            if (console == null || console.furniture == null) return null;
            foreach (Furniture furniture in console.furniture.furnitures)
            {
                if (furniture != null && furniture.name == name && !FurnitureIds.IsModId(furniture.id)) return furniture;
            }
            return null;
        }

        private static GameObject Clone(GameObject original)
        {
            return original == null ? null : UnityEngine.Object.Instantiate(original, Holder());
        }

        private static Transform Holder()
        {
            if (holder == null)
            {
                holder = new GameObject("ObenseuerCore furniture");
                holder.SetActive(false);
                UnityEngine.Object.DontDestroyOnLoad(holder);
            }
            return holder.transform;
        }

        private static Furniture Placeholder(Furniture saved)
        {
            Furniture placeholder;
            if (placeholders.TryGetValue(saved.id, out placeholder)) return placeholder;
            Furniture template = SavableScriptableObject.LoadFromPath(saved.addressableAssetPath) as Furniture;
            if (template == null || template.prefab == null) return null;
            placeholder = UnityEngine.Object.Instantiate(template);
            placeholder.id = saved.id;
            placeholder.title = string.IsNullOrEmpty(saved.title) ? template.title : saved.title;
            placeholder.name = placeholder.title;
            placeholder.details = saved.details;
            placeholder.priceOC = saved.priceOC;
            placeholder.priceRM = saved.priceRM;
            placeholder.hideFlags = HideFlags.HideAndDontSave;
            placeholders[saved.id] = placeholder;
            string modGuid;
            string key;
            FurnitureRegistry.TryParse(saved.id, out modGuid, out key);
            if (FurnitureRegistry.Find(saved.id) != null) CoreLog.For(FurnitureRegistry.Owner).Warn("\"" + placeholder.title + "\" (" + saved.id + ") did not build; using its template");
            else CoreLog.For(FurnitureRegistry.Owner).Warn("\"" + placeholder.title + "\" (" + saved.id + ") needs mod " + (modGuid ?? "?") + "; using its template");
            return placeholder;
        }
    }
}
