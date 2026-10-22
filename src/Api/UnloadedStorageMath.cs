using System;
using System.Collections.Generic;
using System.IO;

namespace ObenseuerCore
{
    internal enum UnloadedApply
    {
        Applied,
        Gone,
        Refused
    }

    internal static class UnloadedStorageMath
    {
        internal const int MaxEntries = 256;
        internal const int MaxCount = 100000;
        internal const int MaxModLength = 64;
        internal const int MaxSceneLength = 128;
        internal const int MaxGuidLength = 128;
        internal const int FirstItemId = 2;
        internal const long MaxFileBytes = 64L * 1024 * 1024;
        internal const int RetrySeconds = 10;

        internal static bool ValidMod(string modId)
        {
            return !string.IsNullOrEmpty(modId) && modId.Length <= MaxModLength;
        }

        internal static bool ValidScene(string scene)
        {
            return !string.IsNullOrEmpty(scene) && scene.Length <= MaxSceneLength && scene.IndexOfAny(Path.GetInvalidFileNameChars()) < 0 && !scene.Contains("..");
        }

        internal static bool ValidGuid(string guid)
        {
            return !string.IsNullOrEmpty(guid) && guid.Length <= MaxGuidLength;
        }

        internal static bool ValidItem(int itemId)
        {
            return itemId >= FirstItemId;
        }

        internal static bool ValidCount(int count)
        {
            return count >= 1 && count <= MaxCount;
        }

        internal static bool ValidEntry(UnloadedStorageEntry entry)
        {
            return entry != null && ValidMod(entry.Mod) && ValidScene(entry.Scene) && ValidGuid(entry.Guid) && ValidItem(entry.ItemId) && ValidCount(entry.Count);
        }

        internal static UnloadedStorageFile Clean(UnloadedStorageFile file)
        {
            UnloadedStorageFile clean = new UnloadedStorageFile();
            if (file == null || file.Entries == null) return clean;
            foreach (UnloadedStorageEntry entry in file.Entries)
            {
                if (ValidEntry(entry) && CountOf(clean.Entries, entry.Mod) < MaxEntries) clean.Entries.Add(entry);
            }
            return clean;
        }

        internal static int CountOf(List<UnloadedStorageEntry> entries, string modId)
        {
            int count = 0;
            foreach (UnloadedStorageEntry entry in entries)
            {
                if (entry.Mod == modId) count++;
            }
            return count;
        }

        internal static bool CanMerge(UnloadedStorageEntry last, UnloadedStorageEntry next)
        {
            return last.Mod == next.Mod && last.Scene == next.Scene && last.Guid == next.Guid && last.ItemId == next.ItemId && last.Give == next.Give && (long)last.Count + next.Count <= MaxCount;
        }

        internal static bool Append(List<UnloadedStorageEntry> entries, UnloadedStorageEntry entry, HashSet<UnloadedStorageEntry> settled)
        {
            UnloadedStorageEntry last = entries.Count == 0 ? null : entries[entries.Count - 1];
            if (last != null && !settled.Contains(last) && CanMerge(last, entry))
            {
                last.Count += entry.Count;
                return true;
            }
            if (CountOf(entries, entry.Mod) >= MaxEntries) return false;
            entries.Add(entry);
            return true;
        }

        internal static UnloadedApply Decide(bool found, bool done)
        {
            if (!found) return UnloadedApply.Gone;
            return done ? UnloadedApply.Applied : UnloadedApply.Refused;
        }

        internal static bool Expired(float since, float now)
        {
            return now - since >= RetrySeconds;
        }

        internal static bool Durable(UnloadedStorageEntry entry, bool settled, string activeScene)
        {
            return settled && entry.Scene == activeScene;
        }
    }
}
