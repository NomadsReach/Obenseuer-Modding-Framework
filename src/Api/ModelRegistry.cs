using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ObenseuerCore
{
    public enum ModelPivot
    {
        Keep = 0,
        BottomCenter = 1,
        Center = 2
    }

    public enum ModelCollider
    {
        Box = 0,
        ConvexMesh = 1,
        Template = 2
    }

    public sealed class ModelOptions
    {
        public float Scale = 1f;
        public bool FitToTemplate = true;
        public ModelPivot Pivot = ModelPivot.Keep;
        public Vector3 EulerOffset;
        public Vector3 PositionOffset;
        public bool FlipX = true;
        public ModelCollider Collider = ModelCollider.Box;

        internal ModelOptions Copy()
        {
            return (ModelOptions)MemberwiseClone();
        }
    }

    public sealed class ModelHandle
    {
        internal readonly ObjData Data;
        internal readonly MtlData Mtl;
        internal readonly string TexturePath;
        internal readonly ModelOptions Options;

        internal ModelHandle(string key, ObjData data, MtlData mtl, string texturePath, ModelOptions options, string error)
        {
            Key = key;
            Data = data;
            Mtl = mtl;
            TexturePath = texturePath;
            Options = options;
            Error = error;
            IsReady = error == null && data != null;
            Bounds = IsReady ? ModelBuild.BoundsOf(data.Positions) : new Bounds();
        }

        public string Key { get; private set; }
        public bool IsReady { get; private set; }
        public string Error { get; private set; }
        public Bounds Bounds { get; private set; }

        public Mesh CreateMesh()
        {
            if (!IsReady || Data == null) return null;
            if (!Core.OnMain())
            {
                ApiLog.Once(ModelPatches.Owner, "CreateMesh off main thread", "not on the Unity main thread or Core has not started, returns null");
                return null;
            }
            try
            {
                return ModelBuild.BuildMesh(Data, Options, 1f, Key);
            }
            catch (Exception e)
            {
                ApiLog.Once(ModelPatches.Owner, "CreateMesh", e);
                return null;
            }
        }

        public Material[] CreateMaterials(Material source)
        {
            if (!IsReady || source == null) return null;
            if (!Core.OnMain())
            {
                ApiLog.Once(ModelPatches.Owner, "CreateMaterials off main thread", "not on the Unity main thread or Core has not started, returns null");
                return null;
            }
            try
            {
                return ModelBuild.BuildMaterials(this, source);
            }
            catch (Exception e)
            {
                ApiLog.Once(ModelPatches.Owner, "CreateMaterials", e);
                return null;
            }
        }
    }

    public static class ModelRegistry
    {
        private static readonly Dictionary<string, ModelHandle> handles = new Dictionary<string, ModelHandle>();
        private static readonly Dictionary<int, WorldModel> worlds = new Dictionary<int, WorldModel>();

        public static bool IsAvailable
        {
            get { return ModelPatches.Installed && !CorePatcher.IsDisabled(ModelPatches.Owner); }
        }

        public static ModelHandle LoadObj(string modGuid, string key, string objPath, string texturePath, ModelOptions options)
        {
            ModIds.Require(modGuid, "modGuid");
            if (!ModIds.IsValid(key)) throw new ArgumentException("key must be 1 to 64 characters of letters, digits, '.', '_' or '-'", "key");
            string id = modGuid + "/" + key;
            ModelHandle existing;
            if (handles.TryGetValue(id, out existing) && existing.IsReady) return existing;
            ModelHandle handle = Parse(id, objPath, texturePath, options == null ? new ModelOptions() : options.Copy());
            handles[id] = handle;
            return handle;
        }

        public static ModelHandle Get(string modGuid, string key)
        {
            ModelHandle handle;
            if (modGuid == null || key == null) return null;
            return handles.TryGetValue(modGuid + "/" + key, out handle) ? handle : null;
        }

        public static bool OverrideVanilla(int itemId, ModelHandle world, ModelHandle held)
        {
            if (world == null) throw new ArgumentNullException("world");
            if (held != null) throw new ArgumentException("held models are not supported in this version", "held");
            if (!IsAvailable) return false;
            if (!world.IsReady)
            {
                CoreLog.For(ModelPatches.Owner).Error("override of item " + itemId + " refused: model " + world.Key + " is not ready: " + world.Error);
                return false;
            }
            List<Item> list = ItemDatabase.database;
            bool built = list != null && list.Count > 0;
            if (built && !HasAppearance(list, itemId))
            {
                CoreLog.For(ModelPatches.Owner).Error("override of item " + itemId + " refused: the catalogue has no item with that id and an appearance");
                return false;
            }
            WorldModel previous;
            if (worlds.TryGetValue(itemId, out previous))
            {
                if (previous.Handle == world) return true;
                CoreLog.For(ModelPatches.Owner).Warn("item " + itemId + " model replaced: " + previous.Handle.Key + " by " + world.Key);
            }
            worlds[itemId] = new WorldModel(itemId, itemId, world);
            if (built) ClearPrefabs(list, itemId);
            return true;
        }

        internal static bool Owns(ModelHandle handle)
        {
            ModelHandle known;
            return handle != null && handles.TryGetValue(handle.Key, out known) && known == handle;
        }

        internal static WorldModel WorldFor(int itemId)
        {
            WorldModel world;
            return worlds.TryGetValue(itemId, out world) ? world : null;
        }

        internal static void Attach(Item item, int templateId, ModelHandle model)
        {
            if (model == null || item == null || item.Appearance == null) return;
            try
            {
                if (!IsAvailable)
                {
                    CoreLog.For(ModelPatches.Owner).Warn("model " + model.Key + " not applied to item " + item.ID + ": the model patches are not installed");
                    return;
                }
                if (!model.IsReady)
                {
                    CoreLog.For(ModelPatches.Owner).Error("model " + model.Key + " not applied to item " + item.ID + ", the template look is kept: " + model.Error);
                    return;
                }
                WorldModel existing;
                if (!worlds.TryGetValue(item.ID, out existing) || existing.Handle != model || existing.TemplateId != templateId)
                {
                    worlds[item.ID] = new WorldModel(item.ID, templateId, model);
                }
                item.Appearance.Prefab = null;
                item.Appearance.PrefabMany = null;
            }
            catch (Exception e)
            {
                ApiLog.Once(ModelPatches.Owner, "attach model", e);
            }
        }

        private static ModelHandle Parse(string key, string objPath, string texturePath, ModelOptions options)
        {
            ObjData data = null;
            MtlData mtl = null;
            string error = null;
            try
            {
                if (string.IsNullOrEmpty(objPath) || !Path.IsPathRooted(objPath)) error = "objPath must be an absolute path";
                else if (texturePath != null && !Path.IsPathRooted(texturePath)) error = "texturePath must be an absolute path, or null to use the .mtl file";
                else if (!(options.Scale > 0f) || float.IsInfinity(options.Scale)) error = "Scale must be a finite number above 0";
                else if (options.Scale > 1000f) error = "Scale must be at most 1000";
                else if (!Finite(options.EulerOffset) || !Finite(options.PositionOffset)) error = "EulerOffset and PositionOffset must be finite";
                else
                {
                    data = ObjParser.ParseFile(objPath, options.FlipX, out error);
                    if (data != null) mtl = LoadMtl(data, objPath);
                }
                if (texturePath != null) texturePath = Path.GetFullPath(texturePath);
            }
            catch (Exception e)
            {
                data = null;
                mtl = null;
                error = e.Message;
            }
            ModelHandle handle = new ModelHandle(key, data, mtl, texturePath, options, error);
            if (error != null) CoreLog.For(ModelPatches.Owner).Error("model " + key + " not loaded: " + error);
            if (data != null) WarnAll(key, data.Warnings);
            if (mtl != null) WarnAll(key, mtl.Warnings);
            return handle;
        }

        private static bool Finite(Vector3 v)
        {
            float sum = v.x + v.y + v.z;
            return !float.IsNaN(sum) && !float.IsInfinity(sum);
        }

        private static MtlData LoadMtl(ObjData data, string objPath)
        {
            MtlData mtl = new MtlData();
            if (data.MtlLib == null) return mtl;
            string name = data.MtlLib.Replace('\\', '/');
            if (name.Length == 0 || Path.IsPathRooted(name) || name.IndexOf("..", StringComparison.Ordinal) >= 0)
            {
                mtl.Warnings.Add("mtllib " + data.MtlLib + " refused: it must be a file name inside the model folder");
                return mtl;
            }
            string folder = Path.GetDirectoryName(Path.GetFullPath(objPath));
            return MtlParser.Parse(Path.Combine(folder, name), folder);
        }

        private static void WarnAll(string key, List<string> warnings)
        {
            foreach (string warning in warnings)
            {
                CoreLog.For(ModelPatches.Owner).Warn("model " + key + ": " + warning);
            }
        }

        private static bool HasAppearance(List<Item> list, int itemId)
        {
            foreach (Item item in list)
            {
                if (item.ID == itemId) return item.Appearance != null;
            }
            return false;
        }

        private static void ClearPrefabs(List<Item> list, int itemId)
        {
            foreach (Item item in list)
            {
                if (item.ID == itemId && item.Appearance != null)
                {
                    item.Appearance.Prefab = null;
                    item.Appearance.PrefabMany = null;
                }
            }
        }
    }

    internal sealed class WorldModel
    {
        internal readonly int ItemId;
        internal readonly int TemplateId;
        internal readonly ModelHandle Handle;
        internal bool Prepared;
        internal string Refusal;
        internal string TemplatePath;
        internal GameObject TemplatePrefab;
        internal ModelCollider Collider;
        internal Mesh Mesh;
        internal Material[] Materials;
        internal GameObject Clone;

        internal WorldModel(int itemId, int templateId, ModelHandle handle)
        {
            ItemId = itemId;
            TemplateId = templateId;
            Handle = handle;
        }
    }
}
