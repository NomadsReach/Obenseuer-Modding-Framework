using System;
using UnityEngine;

namespace ObenseuerCore
{
    public enum WorldPropKind
    {
        Model = 0,
        Primitive = 1,
        Furniture = 2,
        Item = 3,
        Light = 4,
        Anchor = 5
    }

    public enum WorldPrimitive
    {
        Cube = 0,
        Sphere = 1,
        Cylinder = 2,
        Plane = 3
    }

    public sealed class WorldPropOptions
    {
        public bool Collider = true;
        public bool IsStatic;
        public int Layer;
        public string InteractKey;
        public string Data;

        internal WorldPropOptions Copy()
        {
            return (WorldPropOptions)MemberwiseClone();
        }
    }

    public sealed class WorldPropSource
    {
        internal WorldPropKind Kind;
        internal string Key;
        internal int Alt;
        internal LightType LightKind;
        internal Color LightColor;
        internal float LightRange;
        internal float LightIntensity;
        internal float LightSpotAngle;

        private WorldPropSource(WorldPropKind kind)
        {
            Kind = kind;
            Key = "";
        }

        public static WorldPropSource Model(string modGuid, string key)
        {
            ModIds.Require(modGuid, "modGuid");
            if (!ModIds.IsValid(key)) throw new ArgumentException("key must be 1 to 64 characters of letters, digits, '.', '_' or '-'", "key");
            WorldPropSource source = new WorldPropSource(WorldPropKind.Model);
            source.Key = modGuid + "/" + key;
            return source;
        }

        public static WorldPropSource Primitive(WorldPrimitive primitive)
        {
            if (primitive != WorldPrimitive.Cube && primitive != WorldPrimitive.Sphere && primitive != WorldPrimitive.Cylinder && primitive != WorldPrimitive.Plane)
            {
                throw new ArgumentException("primitive must be Cube, Sphere, Cylinder or Plane", "primitive");
            }
            WorldPropSource source = new WorldPropSource(WorldPropKind.Primitive);
            source.Alt = (int)primitive;
            return source;
        }

        public static WorldPropSource Furniture(string furnitureId, int alt)
        {
            if (string.IsNullOrEmpty(furnitureId)) throw new ArgumentException("furnitureId must be a non-empty furniture id", "furnitureId");
            if (furnitureId.Length > 128) throw new ArgumentException("furnitureId must be at most 128 characters", "furnitureId");
            if (alt < 0) throw new ArgumentException("alt must not be negative", "alt");
            WorldPropSource source = new WorldPropSource(WorldPropKind.Furniture);
            source.Key = furnitureId;
            source.Alt = alt;
            return source;
        }

        public static WorldPropSource Item(int itemId)
        {
            if (itemId <= 0) throw new ArgumentException("itemId must be a positive item id", "itemId");
            WorldPropSource source = new WorldPropSource(WorldPropKind.Item);
            source.Alt = itemId;
            return source;
        }

        public static WorldPropSource Light(LightType type, Color color, float range, float intensity, float spotAngle)
        {
            if (type != LightType.Point && type != LightType.Spot)
            {
                throw new ArgumentException("type must be LightType.Point or LightType.Spot", "type");
            }
            if (!Finite(range) || range <= 0f) throw new ArgumentException("range must be a finite number above 0", "range");
            if (!Finite(intensity) || intensity < 0f) throw new ArgumentException("intensity must be a finite number that is not negative", "intensity");
            if (!Finite(spotAngle) || spotAngle <= 0f || spotAngle >= 180f) throw new ArgumentException("spotAngle must be a finite number between 0 and 180", "spotAngle");
            if (type == LightType.Spot && spotAngle < 1f) throw new ArgumentException("spotAngle must be at least 1 for a spot light", "spotAngle");
            WorldPropSource source = new WorldPropSource(WorldPropKind.Light);
            source.LightKind = type;
            source.LightColor = color;
            source.LightRange = range;
            source.LightIntensity = intensity;
            source.LightSpotAngle = spotAngle;
            return source;
        }

        public static WorldPropSource Anchor()
        {
            return new WorldPropSource(WorldPropKind.Anchor);
        }

        internal static bool Finite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }

    public sealed class WorldPropInfo
    {
        public string ModId { get; private set; }

        public string PropId { get; private set; }

        public string Scene { get; private set; }

        public WorldPropKind Kind { get; private set; }

        public Vector3 Position { get; private set; }

        public Quaternion Rotation { get; private set; }

        public Vector3 Scale { get; private set; }

        public bool Live { get; private set; }

        public bool Collider { get; private set; }

        public bool IsStatic { get; private set; }

        public int Layer { get; private set; }

        public string InteractKey { get; private set; }

        public string Data { get; private set; }

        internal WorldPropInfo(SavedPropEntry entry, bool live)
        {
            ModId = entry.Mod;
            PropId = entry.PropId;
            Scene = entry.Scene;
            Kind = (WorldPropKind)entry.Kind;
            Position = entry.Position;
            Rotation = entry.Rotation;
            Scale = entry.Scale;
            Live = live;
            Collider = entry.Collider;
            IsStatic = entry.Static;
            Layer = entry.Layer;
            InteractKey = entry.InteractKey;
            Data = entry.Data;
        }
    }

    public sealed class WorldPropSpawned
    {
        public GameObject Object { get; private set; }

        public WorldPropInfo Info { get; private set; }

        public string ModId
        {
            get { return Info == null ? null : Info.ModId; }
        }

        public string PropId
        {
            get { return Info == null ? null : Info.PropId; }
        }

        public string Scene
        {
            get { return Info == null ? null : Info.Scene; }
        }

        internal WorldPropSpawned(GameObject live, WorldPropInfo info)
        {
            Object = live;
            Info = info;
        }
    }

    public sealed class WorldPropUse
    {
        public string ModId { get; private set; }

        public string Key { get; private set; }

        public string PropId { get; private set; }

        public string Scene { get; private set; }

        public string Data { get; private set; }

        public GameObject Object { get; private set; }

        internal WorldPropUse(SavedPropEntry entry, GameObject live)
        {
            ModId = entry.Mod;
            Key = entry.InteractKey;
            PropId = entry.PropId;
            Scene = entry.Scene;
            Data = entry.Data;
            Object = live;
        }
    }
}
