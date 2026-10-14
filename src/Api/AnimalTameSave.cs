using System;
using System.Collections.Generic;

namespace ObenseuerCore
{
    [Serializable]
    internal sealed class AnimalTameEntry
    {
        public string Mod;
        public string Scene;
        public string Guid;
        public bool Tame;
    }

    [Serializable]
    internal sealed class AnimalTameFile
    {
        internal const int MaxEntries = 1024;
        internal const int MaxTotalEntries = 16384;

        public List<AnimalTameEntry> Entries = new List<AnimalTameEntry>();

        internal bool Set(string mod, string scene, string guid, bool tame)
        {
            AnimalTameEntry entry = Find(mod, scene, guid);
            if (entry == null)
            {
                if (Entries.Count >= MaxTotalEntries || Count(mod) >= MaxEntries) return false;
                entry = new AnimalTameEntry { Mod = mod, Scene = scene, Guid = guid };
                Entries.Add(entry);
            }
            entry.Tame = tame;
            return true;
        }

        internal int Count(string mod)
        {
            return Entries.FindAll(e => e.Mod == mod).Count;
        }

        internal int Forget(string mod)
        {
            return Entries.RemoveAll(e => e.Mod == mod);
        }

        internal List<AnimalTameEntry> For(string scene, string guid, Func<string, bool> present)
        {
            return Entries.FindAll(e => e.Scene == scene && e.Guid == guid && present(e.Mod));
        }

        internal int DropStale(Func<string, bool> installed, Func<AnimalTameEntry, bool?> liveTame)
        {
            return Entries.RemoveAll(entry =>
            {
                if (!installed(entry.Mod)) return false;
                bool? tame = liveTame(entry);
                return tame.HasValue && tame.Value != entry.Tame;
            });
        }

        internal static AnimalTameFile Clean(AnimalTameFile file)
        {
            AnimalTameFile clean = new AnimalTameFile();
            if (file == null || file.Entries == null) return clean;
            Dictionary<string, AnimalTameEntry> byKey = new Dictionary<string, AnimalTameEntry>();
            Dictionary<string, int> perMod = new Dictionary<string, int>();
            foreach (AnimalTameEntry entry in file.Entries)
            {
                if (!Valid(entry)) continue;
                string key = Key(entry.Mod, entry.Scene, entry.Guid);
                AnimalTameEntry kept;
                if (byKey.TryGetValue(key, out kept))
                {
                    kept.Tame = entry.Tame;
                    continue;
                }
                int count;
                perMod.TryGetValue(entry.Mod, out count);
                if (clean.Entries.Count >= MaxTotalEntries || count >= MaxEntries) continue;
                perMod[entry.Mod] = count + 1;
                kept = new AnimalTameEntry { Mod = entry.Mod, Scene = entry.Scene, Guid = entry.Guid, Tame = entry.Tame };
                byKey.Add(key, kept);
                clean.Entries.Add(kept);
            }
            return clean;
        }

        private static bool Valid(AnimalTameEntry entry)
        {
            return entry != null && ModIds.IsValid(entry.Mod) && !string.IsNullOrEmpty(entry.Scene) && !string.IsNullOrEmpty(entry.Guid);
        }

        private static string Key(string mod, string scene, string guid)
        {
            return mod.Length + ":" + mod + scene.Length + ":" + scene + guid;
        }

        private AnimalTameEntry Find(string mod, string scene, string guid)
        {
            foreach (AnimalTameEntry entry in Entries)
            {
                if (entry.Mod == mod && entry.Scene == scene && entry.Guid == guid) return entry;
            }
            return null;
        }
    }
}
