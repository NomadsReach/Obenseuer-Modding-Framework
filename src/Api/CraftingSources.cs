using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine.SceneManagement;

namespace ObenseuerCore
{
    public static class CraftingSources
    {
        internal const string Owner = "obenseuer.moddingframework.craftingsources";
        private static readonly List<Entry> providers = new List<Entry>();
        private static readonly List<Scope> open = new List<Scope>();
        private static readonly List<CraftingBase> awaitingConsume = new List<CraftingBase>();
        private static readonly MethodInfo compareRecipe = AccessTools.Method(typeof(CraftingBase), "CompareRecipeToItems", new[] { typeof(Recipe), typeof(ItemStack[]), typeof(bool), typeof(bool) });
        private static bool installed;

        public static bool Register(string modId, Func<CraftingStationInfo, IEnumerable<Storage>> provider)
        {
            ModIds.Require(modId, "modId");
            if (provider == null) throw new ArgumentNullException("provider");
            if (!installed || CorePatcher.IsDisabled(Owner))
            {
                CoreLog.For(modId).Warn("CraftingSources.Register ignored, the crafting hooks are not installed");
                return false;
            }
            Entry entry = new Entry(modId, provider);
            for (int i = 0; i < providers.Count; i++)
            {
                if (providers[i].ModId != modId) continue;
                providers[i] = entry;
                return true;
            }
            providers.Add(entry);
            return true;
        }

        public static bool Unregister(string modId)
        {
            ModIds.Require(modId, "modId");
            for (int i = 0; i < providers.Count; i++)
            {
                if (providers[i].ModId != modId) continue;
                providers.RemoveAt(i);
                return true;
            }
            return false;
        }

        internal static void Init()
        {
            Events.SceneLoadedWithMode += OnSceneLoaded;
            installed = CraftingSourcesPatches.Install();
            if (installed) return;
            CorePatcher.DisableOwner(Owner);
            CoreLog.For(Owner).Warn("crafting sources hooks incomplete, extra sources are disabled");
        }

        internal static Scope Open(CraftingBase station, bool consume)
        {
            if (providers.Count == 0 || station == null || station.InputOutputStorage == null || SaveGate.Busy || IsOpen(station) || station is Dishwasher) return null;
            bool paired = consume && awaitingConsume.Remove(station);
            Scope scope = new Scope(station, consume);
            open.Add(scope);
            try
            {
                scope.Extras = Collect(station);
                if (scope.Extras.Count == 0)
                {
                    if (paired) return scope;
                    open.Remove(scope);
                    return null;
                }
                List<StorageSlot[]> lists = new List<StorageSlot[]>();
                foreach (Storage extra in scope.Extras) lists.Add(extra.storageSlots);
                scope.Own = station.InputOutputStorage.storageSlots;
                scope.Combined = Combine(scope.Own, lists);
                station.InputOutputStorage.storageSlots = scope.Combined;
                return scope;
            }
            catch (Exception)
            {
                open.Remove(scope);
                throw;
            }
        }

        internal static void Close(Scope scope)
        {
            if (scope == null || scope.Closed) return;
            scope.Closed = true;
            open.Remove(scope);
            if (scope.Combined == null) return;
            Storage storage = scope.Station.InputOutputStorage;
            if (storage != null && storage.storageSlots == scope.Combined) storage.storageSlots = scope.Own;
            else ApiLog.Once(Owner, "restore station slots", "the station slots changed during a call that used extra sources, the new slots are kept");
            if (!scope.Consume) return;
            foreach (Storage extra in scope.Extras)
            {
                try
                {
                    if (extra.Slots != null) extra.UpdateSlots();
                }
                catch (Exception e)
                {
                    ApiLog.Once(Owner, "refresh extra source", e);
                }
            }
        }

        internal static void CloseConsume(CraftingBase station)
        {
            foreach (Scope scope in open.ToArray())
            {
                if (scope.Consume && ReferenceEquals(scope.Station, station)) Close(scope);
            }
        }

        internal static bool Allows(CraftingBase station, Recipe recipe, int multiples)
        {
            if (recipe == null) return true;
            awaitingConsume.Remove(station);
            Scope scope = Open(station, false);
            if (scope == null) return true;
            try
            {
                bool fits = Fits(station, recipe, multiples);
                if (fits) awaitingConsume.Add(station);
                return fits;
            }
            finally
            {
                Close(scope);
            }
        }

        internal static void Expire(CraftingBase station)
        {
            awaitingConsume.RemoveAll(s => s == null || ReferenceEquals(s, station));
        }

        private static void OnSceneLoaded(string scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single) return;
            awaitingConsume.Clear();
        }

        internal static bool Fits(CraftingBase station, Recipe recipe, int multiples)
        {
            if (!(bool)compareRecipe.Invoke(station, new object[] { recipe, null, false, true })) return false;
            return multiples <= 1 || station.GetRecipeMultiplier(recipe) >= multiples;
        }

        internal static T[] Combine<T>(T[] own, IList<T[]> extras) where T : class
        {
            List<T> result = new List<T>(own);
            List<T[]> seen = new List<T[]> { own };
            foreach (T[] extra in extras)
            {
                if (extra == null || extra.Length == 0 || seen.Exists(s => ReferenceEquals(s, extra))) continue;
                seen.Add(extra);
                result.AddRange(extra);
            }
            return result.ToArray();
        }

        private static bool IsOpen(CraftingBase station)
        {
            foreach (Scope scope in open)
            {
                if (ReferenceEquals(scope.Station, station)) return true;
            }
            return false;
        }

        private static List<Storage> Collect(CraftingBase station)
        {
            List<Storage> found = new List<Storage>();
            CraftingStationInfo info = Crafting.ToStation(station);
            foreach (Entry entry in providers.ToArray())
            {
                List<Storage> given = Ask(entry, info);
                if (given == null) continue;
                foreach (Storage storage in given)
                {
                    if (storage == null || ReferenceEquals(storage, station.InputOutputStorage) || ReferenceEquals(storage, station.OutputStorage) || storage.storageSlots == null || storage.storageSlots.Length == 0 || found.Contains(storage)) continue;
                    found.Add(storage);
                }
            }
            return found;
        }

        private static List<Storage> Ask(Entry entry, CraftingStationInfo info)
        {
            try
            {
                IEnumerable<Storage> given = entry.Provider(info);
                if (given == null)
                {
                    ApiLog.Once(entry.ModId, "CraftingSources provider", "returned null, its containers are ignored");
                    return null;
                }
                return new List<Storage>(given);
            }
            catch (Exception e)
            {
                ApiLog.Once(entry.ModId, "CraftingSources provider", e);
                return null;
            }
        }

        internal sealed class Entry
        {
            internal readonly string ModId;
            internal readonly Func<CraftingStationInfo, IEnumerable<Storage>> Provider;

            internal Entry(string modId, Func<CraftingStationInfo, IEnumerable<Storage>> provider)
            {
                ModId = modId;
                Provider = provider;
            }
        }

        internal sealed class Scope
        {
            internal readonly CraftingBase Station;
            internal readonly bool Consume;
            internal StorageSlot[] Own;
            internal StorageSlot[] Combined;
            internal List<Storage> Extras;
            internal bool Closed;

            internal Scope(CraftingBase station, bool consume)
            {
                Station = station;
                Consume = consume;
            }
        }
    }
}
