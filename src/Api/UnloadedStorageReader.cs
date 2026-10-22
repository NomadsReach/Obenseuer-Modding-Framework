using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;

namespace ObenseuerCore
{
    internal static class UnloadedStorageReader
    {
        internal static UnloadedContainer Parse(string json, string guid, out string refusal)
        {
            refusal = null;
            try
            {
                JObject root = JObject.Parse(json);
                return FromRoot(root, guid, out refusal);
            }
            catch (Exception e)
            {
                refusal = "the scene file could not be read: " + e.Message;
                return null;
            }
        }

        internal static List<string> ParseGuids(string json, out string refusal)
        {
            refusal = null;
            try
            {
                JArray data = JObject.Parse(json)["Data"] as JArray;
                if (data == null)
                {
                    refusal = "the scene file has no Data list";
                    return new List<string>();
                }
                List<string> guids = new List<string>();
                foreach (JToken token in data)
                {
                    JObject entry = token as JObject;
                    if (entry == null || !IsStorage(entry)) continue;
                    string guid = (string)entry["GUID"];
                    if (!string.IsNullOrEmpty(guid)) guids.Add(guid);
                }
                return guids;
            }
            catch (Exception e)
            {
                refusal = "the scene file could not be read: " + e.Message;
                return new List<string>();
            }
        }

        internal static UnloadedContainer Read(string path, string guid, out string refusal)
        {
            refusal = null;
            FileInfo info = new FileInfo(path);
            if (!info.Exists)
            {
                refusal = "this save has no file for the scene yet";
                return null;
            }
            if (info.Length > UnloadedStorageMath.MaxFileBytes)
            {
                refusal = "the scene file is too large to read";
                return null;
            }
            return Parse(File.ReadAllText(path, Encoding.UTF8), guid, out refusal);
        }

        internal static List<string> Guids(string path, out string refusal)
        {
            refusal = null;
            FileInfo info = new FileInfo(path);
            if (!info.Exists)
            {
                refusal = "this save has no file for the scene yet";
                return new List<string>();
            }
            if (info.Length > UnloadedStorageMath.MaxFileBytes)
            {
                refusal = "the scene file is too large to read";
                return new List<string>();
            }
            return ParseGuids(File.ReadAllText(path, Encoding.UTF8), out refusal);
        }

        private static UnloadedContainer FromRoot(JObject root, string guid, out string refusal)
        {
            refusal = null;
            JArray data = root["Data"] as JArray;
            if (data == null)
            {
                refusal = "the scene file has no Data list";
                return null;
            }
            JObject storage = null;
            int matches = 0;
            foreach (JToken token in data)
            {
                JObject entry = token as JObject;
                if (entry == null || (string)entry["GUID"] != guid || !IsStorage(entry)) continue;
                storage = entry["Data"] as JObject;
                matches++;
            }
            if (matches == 0)
            {
                refusal = "no storage with this id is in the scene file";
                return null;
            }
            if (matches > 1)
            {
                refusal = "more than one storage has this id, the read is refused";
                return null;
            }
            return Stacks(storage, guid, out refusal);
        }

        private static UnloadedContainer Stacks(JObject storage, string guid, out string refusal)
        {
            refusal = null;
            JArray slots = (JArray)storage["storageSlots"];
            List<UnloadedStack> stacks = new List<UnloadedStack>();
            for (int i = 0; i < slots.Count; i++)
            {
                JToken token = slots[i];
                if (token.Type == JTokenType.Null) continue;
                JObject slot = token as JObject;
                if (slot == null)
                {
                    refusal = "slot " + i + " is not an object, the read is refused";
                    return null;
                }
                if (slot["$ref"] != null)
                {
                    refusal = "slot " + i + " is a reference, the read is refused";
                    return null;
                }
                int itemId = Integer(slot["itemId"], -1);
                int amount = Integer(slot["itemAmount"], 0);
                int owner = Integer(slot["ownerId"], -1);
                if (itemId != -1 && amount > 0) stacks.Add(new UnloadedStack(i, itemId, amount, owner));
            }
            return new UnloadedContainer(guid, stacks);
        }

        private static bool IsStorage(JObject entry)
        {
            JObject body = entry["Data"] as JObject;
            return body != null && body["storageSlots"] is JArray;
        }

        private static int Integer(JToken token, int fallback)
        {
            if (token == null || token.Type != JTokenType.Integer) return fallback;
            return (int)token;
        }
    }
}
