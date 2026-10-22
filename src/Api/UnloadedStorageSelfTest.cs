using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace ObenseuerCore
{
    internal static class UnloadedStorageSelfTest
    {
        private const string Save = "{\"SaveName\":\"Slot1\",\"Data\":["
            + "{\"GUID\":\"g1\",\"Data\":{\"$type\":\"Storage\",\"storageSlots\":["
            + "{\"$id\":\"1\",\"itemId\":5,\"itemAmount\":3,\"ownerId\":-1},"
            + "{\"$id\":\"2\",\"itemId\":-1,\"itemAmount\":0,\"ownerId\":-1},"
            + "null,"
            + "{\"$id\":\"3\",\"itemId\":7,\"itemAmount\":0,\"ownerId\":-1}]}},"
            + "{\"GUID\":\"g2\",\"Data\":{\"$type\":\"Other\",\"x\":1}}]}";

        internal static string Run()
        {
            try
            {
                string merge = CheckMerge();
                if (merge != null) return merge;
                string apply = CheckApply();
                if (apply != null) return apply;
                string retry = CheckRetry();
                if (retry != null) return retry;
                string read = CheckRead();
                if (read != null) return read;
                string clean = CheckClean();
                if (clean != null) return clean;
                string snapshot = CheckSnapshot();
                if (snapshot != null) return snapshot;
            }
            catch (Exception e)
            {
                return "unloaded storage self-check exception: " + e.Message;
            }
            return "unloaded storage self-check ok";
        }

        private static string CheckMerge()
        {
            List<UnloadedStorageEntry> list = new List<UnloadedStorageEntry>();
            HashSet<UnloadedStorageEntry> settled = new HashSet<UnloadedStorageEntry>();
            if (!UnloadedStorageMath.Append(list, Entry("a", 5, 2, false), settled)) return "first queue entry was refused";
            if (!UnloadedStorageMath.Append(list, Entry("a", 5, 3, false), settled) || list.Count != 1 || list[0].Count != 5) return "same container and item did not merge into one entry";
            if (!UnloadedStorageMath.Append(list, Entry("a", 5, 1, true), settled) || list.Count != 2) return "a give merged with a take";
            if (!UnloadedStorageMath.Append(list, Entry("a", 6, 1, true), settled) || list.Count != 3) return "a different item merged";
            settled.Add(list[2]);
            if (!UnloadedStorageMath.Append(list, Entry("a", 6, 1, true), settled) || list.Count != 4) return "a settled entry was merged into";
            list[3].Count = UnloadedStorageMath.MaxCount;
            if (!UnloadedStorageMath.Append(list, Entry("a", 6, 1, true), settled) || list.Count != 5) return "a merge past the count cap was not split off";
            for (int i = 0; i < UnloadedStorageMath.MaxEntries; i++) list.Add(Entry("b", 10 + i, 1, false));
            if (UnloadedStorageMath.Append(list, Entry("b", 9, 1, false), settled)) return "a mod passed the entry cap";
            return null;
        }

        private static string CheckApply()
        {
            if (UnloadedStorageMath.Decide(false, false) != UnloadedApply.Gone) return "a missing container is not Gone";
            if (UnloadedStorageMath.Decide(false, true) != UnloadedApply.Gone) return "a missing container with done is not Gone";
            if (UnloadedStorageMath.Decide(true, true) != UnloadedApply.Applied) return "a done change is not Applied";
            if (UnloadedStorageMath.Decide(true, false) != UnloadedApply.Refused) return "a refused change is not Refused";
            UnloadedStorageEntry entry = Entry("a", 5, 1, false);
            if (!UnloadedStorageMath.Durable(entry, true, "Bazaar")) return "a settled entry of the active scene is not durable";
            entry.Scene = "Other";
            if (UnloadedStorageMath.Durable(entry, true, "Bazaar")) return "a settled entry of another scene is durable";
            if (UnloadedStorageMath.Durable(entry, false, "Other")) return "an unsettled entry is durable";
            return null;
        }

        private static string CheckRetry()
        {
            if (UnloadedStorageMath.Expired(0f, UnloadedStorageMath.RetrySeconds - 1)) return "a retry expired before its window";
            if (!UnloadedStorageMath.Expired(0f, UnloadedStorageMath.RetrySeconds)) return "a retry did not expire at its window";
            if (UnloadedStorageMath.Expired(5f, 14f)) return "a retry expired before its window from a later start";
            return null;
        }

        private static string CheckRead()
        {
            string refusal;
            UnloadedContainer container = UnloadedStorageReader.Parse(Save, "g1", out refusal);
            if (container == null || refusal != null) return "a storage of the scene file did not read: " + refusal;
            if (container.Stacks.Count != 1 || container.Stacks[0].Slot != 0 || container.Stacks[0].ItemId != 5 || container.Stacks[0].Amount != 3) return "stacks differ from the scene file";
            if (container.Amount(5) != 3 || container.Amount(7) != 0) return "Amount does not total the stacks";
            if (UnloadedStorageReader.Parse(Save, "g2", out refusal) != null || refusal == null) return "a non-storage entry was read";
            if (UnloadedStorageReader.Parse(Save, "zz", out refusal) != null || refusal == null) return "an unknown id was read";
            string twice = Save.Replace("\"GUID\":\"g2\"", "\"GUID\":\"g1\"").Replace("\"$type\":\"Other\",\"x\":1", "\"$type\":\"Storage\",\"storageSlots\":[]");
            if (UnloadedStorageReader.Parse(twice, "g1", out refusal) != null || refusal == null) return "an ambiguous id was read";
            string reference = Save.Replace("{\"$id\":\"2\",\"itemId\":-1,\"itemAmount\":0,\"ownerId\":-1}", "{\"$ref\":\"1\"}");
            if (UnloadedStorageReader.Parse(reference, "g1", out refusal) != null || refusal == null) return "a reference slot was read";
            if (UnloadedStorageReader.Parse("{not json", "g1", out refusal) != null || refusal == null) return "invalid JSON was read";
            List<string> guids = UnloadedStorageReader.ParseGuids(Save, out refusal);
            if (guids.Count != 1 || guids[0] != "g1") return "the storage ids of the scene file differ";
            return null;
        }

        private static string CheckClean()
        {
            UnloadedStorageFile file = new UnloadedStorageFile();
            file.Entries.Add(Entry("a", 5, 1, false));
            file.Entries.Add(Entry("a", 1, 1, false));
            file.Entries.Add(Entry("a", 5, 0, false));
            file.Entries.Add(Entry("a", 5, UnloadedStorageMath.MaxCount + 1, false));
            file.Entries.Add(null);
            if (UnloadedStorageMath.Clean(file).Entries.Count != 1) return "clean kept an invalid entry";
            if (UnloadedStorageMath.Clean(null).Entries.Count != 0) return "clean of null is not empty";
            return null;
        }

        private static string CheckSnapshot()
        {
            UnloadedStorageEntry entry = Entry("a", 5, 1, false);
            entry.Scene = SceneManager.GetActiveScene().name;
            UnloadedStorage.Settle(entry);
            UnloadedStorage.Entries.Add(entry);
            try
            {
                UnloadedStorage.Flushing = true;
                bool during = UnloadedStorage.Snapshot().Entries.Contains(entry);
                UnloadedStorage.Flushing = false;
                bool after = UnloadedStorage.Snapshot().Entries.Contains(entry);
                if (!during) return "an applied change of the loaded scene was left out of the queue file during a flush";
                if (after) return "an applied change of the loaded scene was written by the game save path";
                return null;
            }
            finally
            {
                UnloadedStorage.Flushing = false;
                UnloadedStorage.Entries.Remove(entry);
                UnloadedStorage.Unsettle(entry);
            }
        }

        private static UnloadedStorageEntry Entry(string mod, int itemId, int count, bool give)
        {
            return new UnloadedStorageEntry { Mod = mod, Scene = "Bazaar", Guid = "g1", ItemId = itemId, Count = count, Give = give };
        }
    }
}
