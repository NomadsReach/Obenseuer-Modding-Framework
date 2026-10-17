using System;
using System.Collections.Generic;
using UnityEngine;

namespace ObenseuerCore
{
    [Serializable]
    internal sealed class SavedPropEntry
    {
        public string Mod;
        public string PropId;
        public string Scene;
        public int Kind;
        public string Key;
        public int Alt;
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;
        public bool Collider;
        public bool Static;
        public int Layer;
        public string InteractKey;
        public string Data;
        public int LightKind;
        public Color LightColor;
        public float LightRange;
        public float LightIntensity;
        public float LightSpotAngle;
    }

    [Serializable]
    internal sealed class SavedPropFile
    {
        internal const int MaxEntries = 256;
        internal const int MaxIdLength = 64;
        internal const int MaxKeyLength = 128;
        internal const int MaxSceneLength = 128;
        internal const int MaxInteractKeyLength = 64;
        internal const int MaxDataLength = 256;
        internal const float MinScale = 0.01f;
        internal const float MaxScale = 100f;
        private const int MaxLightKeyLength = 64;

        public List<SavedPropEntry> Entries = new List<SavedPropEntry>();

        internal SavedPropEntry Find(string mod, string propId)
        {
            foreach (SavedPropEntry entry in Entries)
            {
                if (entry.Mod == mod && entry.PropId == propId) return entry;
            }
            return null;
        }

        internal bool Room(string mod, string propId)
        {
            return Find(mod, propId) != null || Count(mod) < MaxEntries;
        }

        internal void Put(SavedPropEntry entry)
        {
            Entries.RemoveAll(e => e.Mod == entry.Mod && e.PropId == entry.PropId);
            Entries.Add(entry);
        }

        internal int Drop(string mod, string propId)
        {
            return Entries.RemoveAll(e => e.Mod == mod && (propId == null || e.PropId == propId));
        }

        internal int Count(string mod)
        {
            int count = 0;
            foreach (SavedPropEntry entry in Entries)
            {
                if (entry.Mod == mod) count++;
            }
            return count;
        }

        internal List<SavedPropEntry> Restorable(string scene)
        {
            return Entries.FindAll(e => e.Scene == scene);
        }

        internal static SavedPropFile Clean(SavedPropFile file)
        {
            SavedPropFile clean = new SavedPropFile();
            if (file == null || file.Entries == null) return clean;
            foreach (SavedPropEntry entry in file.Entries)
            {
                if (entry == null || !Valid(entry) || !clean.Room(entry.Mod, entry.PropId)) continue;
                Text(entry);
                clean.Put(entry);
            }
            return clean;
        }

        internal static bool Valid(SavedPropEntry entry)
        {
            if (!ModIds.IsValid(entry.Mod)) return false;
            if (string.IsNullOrEmpty(entry.PropId) || entry.PropId.Length > MaxIdLength) return false;
            if (string.IsNullOrEmpty(entry.Scene) || entry.Scene.Length > MaxSceneLength) return false;
            if (!Finite(entry.Position) || !Finite(entry.Rotation) || !Finite(entry.Scale)) return false;
            if (!ScaleAllowed(entry.Scale)) return false;
            if (entry.Layer != 0) return false;
            if (entry.InteractKey != null && entry.InteractKey.Length > MaxInteractKeyLength) return false;
            if (entry.Data != null && entry.Data.Length > MaxDataLength) return false;
            return KindAllowed(entry);
        }

        internal static bool KindAllowed(SavedPropEntry entry)
        {
            WorldPropKind kind = (WorldPropKind)entry.Kind;
            if (kind == WorldPropKind.Model)
            {
                string modGuid;
                string key;
                return SplitKey(entry.Key, out modGuid, out key);
            }
            if (kind == WorldPropKind.Primitive)
            {
                return entry.Alt >= (int)WorldPrimitive.Cube && entry.Alt <= (int)WorldPrimitive.Plane && string.IsNullOrEmpty(entry.Key);
            }
            if (kind == WorldPropKind.Furniture)
            {
                return !string.IsNullOrEmpty(entry.Key) && entry.Key.Length <= MaxKeyLength && entry.Alt >= 0;
            }
            if (kind == WorldPropKind.Item)
            {
                return entry.Alt > 0 && string.IsNullOrEmpty(entry.Key);
            }
            if (kind == WorldPropKind.Light)
            {
                if (entry.LightKind != (int)LightType.Point && entry.LightKind != (int)LightType.Spot) return false;
                if (!WorldPropSource.Finite(entry.LightRange) || entry.LightRange <= 0f) return false;
                if (!WorldPropSource.Finite(entry.LightIntensity) || entry.LightIntensity < 0f) return false;
                if (!WorldPropSource.Finite(entry.LightSpotAngle) || entry.LightSpotAngle <= 0f || entry.LightSpotAngle >= 180f) return false;
                return string.IsNullOrEmpty(entry.Key) || entry.Key.Length <= MaxLightKeyLength;
            }
            if (kind == WorldPropKind.Anchor)
            {
                return string.IsNullOrEmpty(entry.Key);
            }
            return false;
        }

        internal static bool SplitKey(string key, out string modGuid, out string shortKey)
        {
            modGuid = null;
            shortKey = null;
            if (string.IsNullOrEmpty(key) || key.Length > MaxKeyLength) return false;
            int at = key.IndexOf('/');
            if (at <= 0 || at == key.Length - 1) return false;
            modGuid = key.Substring(0, at);
            shortKey = key.Substring(at + 1);
            return ModIds.IsValid(modGuid) && ModIds.IsValid(shortKey);
        }

        internal static bool PoseAllowed(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            return Finite(position) && Finite(rotation) && ScaleAllowed(scale);
        }

        internal static bool ScaleAllowed(Vector3 scale)
        {
            return Finite(scale) && Axis(scale.x) && Axis(scale.y) && Axis(scale.z);
        }

        internal static bool Finite(Vector3 value)
        {
            return WorldPropSource.Finite(value.x) && WorldPropSource.Finite(value.y) && WorldPropSource.Finite(value.z);
        }

        internal static bool Finite(Quaternion value)
        {
            return WorldPropSource.Finite(value.x) && WorldPropSource.Finite(value.y) && WorldPropSource.Finite(value.z) && WorldPropSource.Finite(value.w);
        }

        private static void Text(SavedPropEntry entry)
        {
            entry.Key = entry.Key == null ? "" : entry.Key;
            entry.InteractKey = entry.InteractKey == null ? "" : entry.InteractKey;
            entry.Data = entry.Data == null ? "" : entry.Data;
        }

        private static bool Axis(float value)
        {
            return WorldPropSource.Finite(value) && value >= MinScale && value <= MaxScale;
        }
    }
}
