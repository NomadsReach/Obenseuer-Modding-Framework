using System;
using System.Collections.Generic;

namespace ObenseuerCore
{
    internal enum FurnitureIdKind { Vanilla, Furniture, Blueprint, Malformed }

    internal static class FurnitureIds
    {
        internal const string Prefix = "obx:";
        internal const string BlueprintSuffix = "/blueprint";
        private const int MaxInputs = 16;
        private const int MinInputKey = 2;
        private const int MaxInputAmount = 1000;
        private const int MaxFurnitureInputs = 8;
        private const float MaxProcessTime = 10000000f;
        private const int MaxCraftAmount = 100;
        internal const int MaxPools = 8;
        internal const int MaxPoolName = 64;
        internal const int MaxLootWeight = 100;

        internal static bool IsModId(string id)
        {
            return id != null && id.StartsWith(Prefix, StringComparison.Ordinal);
        }

        internal static bool TryParse(string id, out string modGuid, out string key)
        {
            modGuid = null;
            key = null;
            return Classify(id) == FurnitureIdKind.Furniture && Split(id, out modGuid, out key);
        }

        internal static FurnitureIdKind Classify(string id)
        {
            if (!IsModId(id)) return FurnitureIdKind.Vanilla;
            string modGuid;
            string key;
            if (id.EndsWith(BlueprintSuffix, StringComparison.Ordinal) && Split(id.Substring(0, id.Length - BlueprintSuffix.Length), out modGuid, out key)) return FurnitureIdKind.Blueprint;
            return Split(id, out modGuid, out key) ? FurnitureIdKind.Furniture : FurnitureIdKind.Malformed;
        }

        internal static string BlueprintIdFor(string furnitureId)
        {
            return Classify(furnitureId) == FurnitureIdKind.Furniture ? furnitureId + BlueprintSuffix : null;
        }

        internal static bool TryParseBlueprint(string id, out string furnitureId)
        {
            furnitureId = Classify(id) == FurnitureIdKind.Blueprint ? id.Substring(0, id.Length - BlueprintSuffix.Length) : null;
            return furnitureId != null;
        }

        internal static string CheckBlueprint(float processTime, int amount, bool hasSkill, int minimumSkill, int maxSkill)
        {
            if (float.IsNaN(processTime) || processTime <= 0f || processTime > MaxProcessTime) return "ProcessTime must be above 0 and at most 10000000 seconds";
            if (amount < 1 || amount > MaxCraftAmount) return "Amount must be 1 to " + MaxCraftAmount;
            if (minimumSkill < 0 || minimumSkill > maxSkill) return "MinimumSkill must be 0 to " + maxSkill;
            if (minimumSkill > 0 && !hasSkill) return "MinimumSkill needs a Skill";
            return null;
        }

        internal static string CheckInputs(ICollection<KeyValuePair<int, int>> items, IList<string> furniture)
        {
            if (items == null || furniture == null) return "Inputs and FurnitureInputs must not be null";
            if (items.Count > MaxInputs) return "Inputs has more than " + MaxInputs + " entries";
            foreach (KeyValuePair<int, int> item in items)
            {
                if (item.Key < MinInputKey) return "Inputs key " + item.Key + " is below " + MinInputKey;
                if (item.Value < 1 || item.Value > MaxInputAmount) return "Inputs amount " + item.Value + " is outside 1 to " + MaxInputAmount;
            }
            if (furniture.Count > MaxFurnitureInputs) return "FurnitureInputs has more than " + MaxFurnitureInputs + " entries";
            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (string piece in furniture)
            {
                if (string.IsNullOrEmpty(piece)) return "FurnitureInputs has a null or empty entry";
                if (!seen.Add(piece)) return "FurnitureInputs repeats " + piece;
                FurnitureIdKind kind = Classify(piece);
                if (kind == FurnitureIdKind.Blueprint) return "FurnitureInputs entry " + piece + " is a blueprint id";
                if (kind == FurnitureIdKind.Malformed) return "FurnitureInputs entry " + piece + " is a malformed obx: id";
            }
            return null;
        }

        internal static string CheckPools(IList<string> pools)
        {
            if (pools == null) return "Pools must not be null";
            if (pools.Count > MaxPools) return "Pools has more than " + MaxPools + " entries";
            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (string pool in pools)
            {
                if (pool == null) return "Pools has a null entry";
                if (pool.Length > MaxPoolName) return "Pools entry is longer than " + MaxPoolName + " characters";
                if (!seen.Add(pool)) return "Pools repeats " + pool;
            }
            return null;
        }

        internal static string CheckLoot(string category, int weight)
        {
            if (string.IsNullOrEmpty(category)) return "loot category must not be empty";
            if (weight < 1 || weight > MaxLootWeight) return "loot weight must be 1 to " + MaxLootWeight;
            return null;
        }

        internal static int LootWeight(ICollection<int> weights)
        {
            long total = 0;
            foreach (int weight in weights)
            {
                total += weight;
            }
            return (int)Math.Min(MaxLootWeight, total);
        }

        internal static int PickWeighted(IList<int> weights, int roll)
        {
            if (weights == null) return -1;
            long total = 0;
            foreach (int weight in weights)
            {
                if (weight > 0) total += weight;
            }
            if (roll < 0 || roll >= total) return -1;
            long edge = 0;
            for (int i = 0; i < weights.Count; i++)
            {
                if (weights[i] <= 0) continue;
                edge += weights[i];
                if (roll < edge) return i;
            }
            return -1;
        }

        private static bool Split(string id, out string modGuid, out string key)
        {
            modGuid = null;
            key = null;
            if (!IsModId(id)) return false;
            int slash = id.IndexOf('/', Prefix.Length);
            if (slash < 0) return false;
            string mod = id.Substring(Prefix.Length, slash - Prefix.Length);
            string name = id.Substring(slash + 1);
            if (!ModIds.IsValid(mod) || !ModIds.IsValid(name)) return false;
            modGuid = mod;
            key = name;
            return true;
        }
    }
}
