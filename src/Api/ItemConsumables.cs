using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using OS.Items;
using UnityEngine.SceneManagement;

namespace ObenseuerCore
{
    public sealed class ConsumableInfo
    {
        public bool DestroyAfterUse { get; set; }

        public float Food { get; set; }

        public float Drink { get; set; }

        public float Nicotine { get; set; }

        public float Intoxication { get; set; }

        public float Smv { get; set; }

        public float Depression { get; set; }

        public float Health { get; set; }

        public float Bleeding { get; set; }

        public float High { get; set; }

        public float Tiredness { get; set; }

        public float Hygiene { get; set; }

        public float AlcoholAddiction { get; set; }

        public float MushroomAddiction { get; set; }

        public float SmokingAddiction { get; set; }
    }

    [Serializable]
    internal sealed class ConsumableSaveEntry
    {
        public string ModId;
        public int ItemId;
        public bool DestroyAfterUse;
        public float[] Values;
    }

    [Serializable]
    internal sealed class ConsumableSave
    {
        public List<ConsumableSaveEntry> Entries = new List<ConsumableSaveEntry>();
    }

    public static class ItemConsumables
    {
        internal const string Owner = "obenseuer.moddingframework.consumables";
        private const int ValueCount = 14;
        private static readonly Dictionary<int, Entry> entries = new Dictionary<int, Entry>();
        private static readonly Dictionary<int, ConsumableInfo> vanilla = new Dictionary<int, ConsumableInfo>();
        private static readonly HashSet<string> setters = new HashSet<string>();
        private static bool inGame;
        private static Func<int, ItemConsumable> lookup;

        public static ConsumableInfo Get(int itemId)
        {
            try
            {
                ItemConsumable consumable = Find(itemId);
                return consumable == null ? null : ToInfo(consumable);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "ItemConsumables.Get", e);
                return null;
            }
        }

        public static bool Set(string modId, int itemId, ConsumableInfo info)
        {
            ModIds.Require(modId, "modId");
            if (info == null || !Finite(info) || SaveGate.Busy) return false;
            try
            {
                ItemConsumable consumable = Find(itemId);
                if (consumable == null) return false;
                Entry previous;
                entries.TryGetValue(itemId, out previous);
                if (previous != null && previous.ModId != modId) ApiLog.Once(Owner, "ItemConsumables.Set writer " + itemId, "consumable values of item " + itemId + " were set by " + previous.ModId + " and are now set by " + modId + ", the values are shared by every copy of the item");
                Remember(itemId, consumable);
                bool bound = inGame || SaveData.Folder != null;
                Entry under = previous == null ? null : (previous.SaveBound ? previous.Under : previous);
                entries[itemId] = new Entry(modId, Copy(info), bound, bound ? under : null);
                setters.Add(modId);
                Write(consumable, info);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "ItemConsumables.Set", e);
                return false;
            }
        }

        public static bool Clear(string modId)
        {
            ModIds.Require(modId, "modId");
            if (SaveGate.Busy) return false;
            try
            {
                bool any = false;
                foreach (KeyValuePair<int, Entry> pair in new List<KeyValuePair<int, Entry>>(entries))
                {
                    Entry kept = Without(pair.Value, modId);
                    if (kept == pair.Value) continue;
                    any = true;
                    if (kept != null) entries[pair.Key] = kept;
                    else entries.Remove(pair.Key);
                    if (pair.Value.ModId != modId) continue;
                    ConsumableInfo original;
                    ConsumableInfo target = kept != null ? kept.Info : (vanilla.TryGetValue(pair.Key, out original) ? original : null);
                    ItemConsumable consumable = Find(pair.Key);
                    if (consumable != null && target != null) Write(consumable, target);
                }
                return any;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "ItemConsumables.Clear", e);
                return false;
            }
        }

        internal static string SelfCheck()
        {
            const int id = -7001;
            const string a = "selfcheck.a";
            const string b = "selfcheck.b";
            bool savedInGame = inGame;
            Func<int, ItemConsumable> savedLookup = lookup;
            try
            {
                ItemConsumable stub = (ItemConsumable)FormatterServices.GetUninitializedObject(typeof(ItemConsumable));
                stub.Food = 1f;
                lookup = itemId => itemId == id ? stub : null;
                inGame = false;
                if (!Set(a, id, new ConsumableInfo { Food = 2f })) return "Set was refused";
                inGame = true;
                Set(a, id, new ConsumableInfo { Food = 3f });
                if (stub.Food != 3f) return "an in-game value was not written";
                if (!Clear(a)) return "Clear found nothing for a mod with entries";
                if (stub.Food != 1f || entries.ContainsKey(id)) return "Clear left the mod's session value under its in-game value";
                inGame = false;
                Set(a, id, new ConsumableInfo { Food = 2f });
                inGame = true;
                Set(b, id, new ConsumableInfo { Food = 4f });
                Clear(a);
                if (stub.Food != 4f) return "Clear changed a value another mod set over it";
                Revert(id);
                if (stub.Food != 1f || entries.ContainsKey(id)) return "a cleared mod's value came back after the in-game value was reverted";
                return "item consumables self-check ok";
            }
            catch (Exception e)
            {
                return "exception: " + e.Message;
            }
            finally
            {
                entries.Remove(id);
                vanilla.Remove(id);
                setters.Remove(a);
                setters.Remove(b);
                lookup = savedLookup;
                inGame = savedInGame;
            }
        }

        internal static void Init()
        {
            Events.SceneLoadedWithMode += (scene, mode) =>
            {
                if (mode == LoadSceneMode.Single) inGame = scene != "Main Menu";
            };
            try
            {
                SaveData.Register<ConsumableSave>(Owner, Build, Load);
            }
            catch (Exception e)
            {
                CoreLog.For(Owner).Error("save data not registered, consumable values last for the session only: " + e);
            }
        }

        private static ConsumableSave Build()
        {
            ConsumableSave save = new ConsumableSave();
            foreach (KeyValuePair<int, Entry> pair in entries)
            {
                if (pair.Value.SaveBound) save.Entries.Add(new ConsumableSaveEntry { ModId = pair.Value.ModId, ItemId = pair.Key, DestroyAfterUse = pair.Value.Info.DestroyAfterUse, Values = Values(pair.Value.Info) });
            }
            return save;
        }

        private static void Load(ConsumableSave save)
        {
            foreach (KeyValuePair<int, Entry> pair in new List<KeyValuePair<int, Entry>>(entries))
            {
                if (pair.Value.SaveBound) Revert(pair.Key);
            }
            if (save == null || save.Entries == null) return;
            foreach (ConsumableSaveEntry saved in save.Entries)
            {
                try
                {
                    Restore(saved);
                }
                catch (Exception e)
                {
                    ApiLog.Once(Owner, "ItemConsumables.Load", e);
                }
            }
        }

        private static void Restore(ConsumableSaveEntry saved)
        {
            if (saved == null || !ModIds.IsValid(saved.ModId) || saved.Values == null || saved.Values.Length != ValueCount) return;
            ConsumableInfo info = FromValues(saved.DestroyAfterUse, saved.Values);
            if (!Finite(info)) return;
            Entry existing;
            if (entries.TryGetValue(saved.ItemId, out existing) && !existing.SaveBound)
            {
                ApiLog.Once(Owner, "ItemConsumables.Load live " + saved.ItemId, "saved consumable values of item " + saved.ItemId + " were not applied: " + existing.ModId + " has set them this session");
                return;
            }
            if (!ModIds.IsInstalled(saved.ModId, setters))
            {
                ApiLog.Once(Owner, "ItemConsumables.Load absent " + saved.ModId, "saved consumable values of " + saved.ModId + " were not applied, the mod is not installed, and the next save drops them");
                return;
            }
            ItemConsumable consumable = Find(saved.ItemId);
            if (consumable == null) return;
            Remember(saved.ItemId, consumable);
            entries[saved.ItemId] = new Entry(saved.ModId, info, true, null);
            Write(consumable, info);
        }

        private static Entry Without(Entry entry, string modId)
        {
            if (entry == null) return null;
            Entry under = Without(entry.Under, modId);
            if (entry.ModId == modId) return under;
            return under == entry.Under ? entry : new Entry(entry.ModId, entry.Info, entry.SaveBound, under);
        }

        private static void Revert(int itemId)
        {
            Entry entry;
            if (!entries.TryGetValue(itemId, out entry)) return;
            ConsumableInfo original;
            ConsumableInfo target = entry.Under != null ? entry.Under.Info : (vanilla.TryGetValue(itemId, out original) ? original : null);
            if (entry.Under != null) entries[itemId] = entry.Under;
            else entries.Remove(itemId);
            ItemConsumable consumable = Find(itemId);
            if (consumable != null && target != null) Write(consumable, target);
        }

        private static void Remember(int itemId, ItemConsumable consumable)
        {
            if (!vanilla.ContainsKey(itemId)) vanilla[itemId] = ToInfo(consumable);
        }

        private static ItemConsumable Find(int itemId)
        {
            if (lookup != null) return lookup(itemId);
            Item item = ItemRegistry.FindInDatabase(itemId);
            if (item == null || item.Actions == null) return null;
            foreach (Item.ItemAction action in item.Actions)
            {
                ItemConsumable consumable = action as ItemConsumable;
                if (consumable != null) return consumable;
            }
            return null;
        }

        private static ConsumableInfo ToInfo(ItemConsumable c)
        {
            return new ConsumableInfo
            {
                DestroyAfterUse = c.DestroyAfterUse,
                Food = c.Food,
                Drink = c.Drink,
                Nicotine = c.Nicotine,
                Intoxication = c.Intoxication,
                Smv = c.SMV,
                Depression = c.Depression,
                Health = c.Health,
                Bleeding = c.Bleeding,
                High = c.High,
                Tiredness = c.Tiredness,
                Hygiene = c.Hygiene,
                AlcoholAddiction = c.AlcoholAddiction,
                MushroomAddiction = c.MushroomAddiction,
                SmokingAddiction = c.SmokingAddiction
            };
        }

        private static void Write(ItemConsumable c, ConsumableInfo info)
        {
            c.DestroyAfterUse = info.DestroyAfterUse;
            c.Food = info.Food;
            c.Drink = info.Drink;
            c.Nicotine = info.Nicotine;
            c.Intoxication = info.Intoxication;
            c.SMV = info.Smv;
            c.Depression = info.Depression;
            c.Health = info.Health;
            c.Bleeding = info.Bleeding;
            c.High = info.High;
            c.Tiredness = info.Tiredness;
            c.Hygiene = info.Hygiene;
            c.AlcoholAddiction = info.AlcoholAddiction;
            c.MushroomAddiction = info.MushroomAddiction;
            c.SmokingAddiction = info.SmokingAddiction;
        }

        private static float[] Values(ConsumableInfo i)
        {
            return new[] { i.Food, i.Drink, i.Nicotine, i.Intoxication, i.Smv, i.Depression, i.Health, i.Bleeding, i.High, i.Tiredness, i.Hygiene, i.AlcoholAddiction, i.MushroomAddiction, i.SmokingAddiction };
        }

        private static ConsumableInfo FromValues(bool destroyAfterUse, float[] v)
        {
            return new ConsumableInfo
            {
                DestroyAfterUse = destroyAfterUse,
                Food = v[0],
                Drink = v[1],
                Nicotine = v[2],
                Intoxication = v[3],
                Smv = v[4],
                Depression = v[5],
                Health = v[6],
                Bleeding = v[7],
                High = v[8],
                Tiredness = v[9],
                Hygiene = v[10],
                AlcoholAddiction = v[11],
                MushroomAddiction = v[12],
                SmokingAddiction = v[13]
            };
        }

        private static ConsumableInfo Copy(ConsumableInfo i)
        {
            return FromValues(i.DestroyAfterUse, Values(i));
        }

        private static bool Finite(ConsumableInfo i)
        {
            foreach (float value in Values(i))
            {
                if (float.IsNaN(value) || float.IsInfinity(value)) return false;
            }
            return true;
        }

        private sealed class Entry
        {
            internal readonly string ModId;
            internal readonly ConsumableInfo Info;
            internal readonly bool SaveBound;
            internal readonly Entry Under;

            internal Entry(string modId, ConsumableInfo info, bool saveBound, Entry under)
            {
                ModId = modId;
                Info = info;
                SaveBound = saveBound;
                Under = under;
            }
        }
    }
}
