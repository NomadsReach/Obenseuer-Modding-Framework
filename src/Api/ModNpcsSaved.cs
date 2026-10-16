using System;
using System.Collections.Generic;

namespace ObenseuerCore
{
    [Serializable]
    internal sealed class SavedNpcShape
    {
        public string Mesh;
        public string Shape;
        public float Weight;
    }

    [Serializable]
    internal sealed class SavedNpcLook
    {
        internal const int MaxShapes = 256;

        public bool Sliders;
        public List<string> DialNames = new List<string>();
        public List<float> DialValues = new List<float>();
        public List<string> SlotNames = new List<string>();
        public List<string> Wearables = new List<string>();
        public List<SavedNpcShape> Shapes = new List<SavedNpcShape>();

        public SavedNpcLook()
        {
        }
    }

    [Serializable]
    internal sealed class SavedNpcEntry
    {
        public string Mod;
        public string NpcId;
        public int Source;
        public int Body;
        public string Name;
        public string LastName;
        public float X;
        public float Y;
        public float Z;
        public float Yaw;
        public string Scene;
        public bool KeepDialogue;
        public SavedNpcLook Look;
    }

    [Serializable]
    internal sealed class SavedNpcFile
    {
        internal const int MaxEntries = 256;
        private const int MaxIdLength = 64;

        public List<SavedNpcEntry> Entries = new List<SavedNpcEntry>();

        internal bool Room(string mod, string npcId)
        {
            return Find(mod, npcId) != null || Count(mod, null) < MaxEntries;
        }

        internal void Put(SavedNpcEntry entry)
        {
            Entries.RemoveAll(e => e.Mod == entry.Mod && e.NpcId == entry.NpcId);
            Entries.Add(entry);
        }

        internal bool SetLook(string mod, string npcId, SavedNpcLook look)
        {
            SavedNpcEntry entry = Find(mod, npcId);
            if (entry == null) return false;
            bool had = entry.Look != null;
            entry.Look = look;
            return had;
        }

        internal SavedNpcLook LookOf(string mod, string npcId)
        {
            SavedNpcEntry entry = Find(mod, npcId);
            return entry == null ? null : entry.Look;
        }

        internal int Count(string mod, string npcId)
        {
            return Entries.FindAll(e => Matches(e, mod, npcId)).Count;
        }

        internal int Drop(string mod, string npcId)
        {
            return Entries.RemoveAll(e => Matches(e, mod, npcId));
        }

        internal List<SavedNpcEntry> Restorable(string scene, Func<string, bool> present)
        {
            return Entries.FindAll(e => e.Scene == scene && present(e.Mod));
        }

        internal static SavedNpcFile Clean(SavedNpcFile file)
        {
            SavedNpcFile clean = new SavedNpcFile();
            if (file == null || file.Entries == null) return clean;
            foreach (SavedNpcEntry entry in file.Entries)
            {
                if (Valid(entry) && clean.Room(entry.Mod, entry.NpcId))
                {
                    entry.Look = NonEmptyLook(CleanLook(entry.Look));
                    clean.Put(entry);
                }
            }
            return clean;
        }

        internal static SavedNpcLook CleanLook(SavedNpcLook look)
        {
            SavedNpcLook clean = new SavedNpcLook();
            if (look == null) return clean;
            clean.Sliders = look.Sliders;
            int dials = Math.Min(LengthOf(look.DialNames), LengthOf(look.DialValues));
            for (int i = 0; i < dials; i++)
            {
                if (!string.IsNullOrEmpty(look.DialNames[i]) && Finite(look.DialValues[i]))
                {
                    clean.DialNames.Add(look.DialNames[i]);
                    clean.DialValues.Add(look.DialValues[i]);
                }
            }
            int slots = Math.Min(LengthOf(look.SlotNames), LengthOf(look.Wearables));
            for (int i = 0; i < slots; i++)
            {
                if (!string.IsNullOrEmpty(look.SlotNames[i]))
                {
                    clean.SlotNames.Add(look.SlotNames[i]);
                    clean.Wearables.Add(look.Wearables[i] ?? "");
                }
            }
            if (look.Shapes == null) return clean;
            foreach (SavedNpcShape shape in look.Shapes)
            {
                if (clean.Shapes.Count == SavedNpcLook.MaxShapes) break;
                if (shape != null && !string.IsNullOrEmpty(shape.Mesh) && !string.IsNullOrEmpty(shape.Shape) && Finite(shape.Weight))
                {
                    clean.Shapes.Add(new SavedNpcShape { Mesh = shape.Mesh, Shape = shape.Shape, Weight = shape.Weight });
                }
            }
            return clean;
        }

        internal static SavedNpcLook NonEmptyLook(SavedNpcLook look)
        {
            return look.DialNames.Count + look.SlotNames.Count + look.Shapes.Count == 0 ? null : look;
        }

        internal static List<SavedNpcShape> DiffShapes(IList<string> names, IList<float> values, IList<string> sourceNames, IList<float> sourceValues, string mesh)
        {
            List<SavedNpcShape> changed = new List<SavedNpcShape>();
            if (names == null || values == null) return changed;
            for (int i = 0; i < Math.Min(names.Count, values.Count); i++)
            {
                if (!Finite(values[i]) || string.IsNullOrEmpty(names[i])) continue;
                int index = sourceNames == null || sourceValues == null ? -1 : sourceNames.IndexOf(names[i]);
                bool same = index >= 0 && index < sourceValues.Count && sourceValues[index] == values[i];
                if (!same) changed.Add(new SavedNpcShape { Mesh = mesh, Shape = names[i], Weight = values[i] });
            }
            return changed;
        }

        private static bool Matches(SavedNpcEntry entry, string mod, string npcId)
        {
            return entry.Mod == mod && (npcId == null || entry.NpcId == npcId);
        }

        private static bool Valid(SavedNpcEntry entry)
        {
            return entry != null && ModIds.IsValid(entry.Mod) && !string.IsNullOrEmpty(entry.NpcId) && entry.NpcId.Length <= MaxIdLength && entry.Body >= 0 && !string.IsNullOrEmpty(entry.Name) && !string.IsNullOrEmpty(entry.Scene) && Finite(entry.X) && Finite(entry.Y) && Finite(entry.Z) && Finite(entry.Yaw);
        }

        private static int LengthOf<T>(List<T> list)
        {
            return list == null ? 0 : list.Count;
        }

        private static bool Finite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private SavedNpcEntry Find(string mod, string npcId)
        {
            foreach (SavedNpcEntry entry in Entries)
            {
                if (entry.Mod == mod && entry.NpcId == npcId) return entry;
            }
            return null;
        }
    }
}
