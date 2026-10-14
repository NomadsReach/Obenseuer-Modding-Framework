using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace ObenseuerCore
{
    public enum RespawnTime
    {
        Default,
        VeryShort,
        Short,
        Long,
        VeryLong,
        EveryDay
    }

    public sealed class SpawnerInfo
    {
        public string Id { get; private set; }

        public string TypeName { get; private set; }

        public bool RespawnEnabled { get; private set; }

        public float Chance { get; private set; }

        public int SpawnedCount { get; private set; }

        public RespawnTime SpawnTime { get; private set; }

        public bool UseCustomSpawnTime { get; private set; }

        public int MinRespawnTime { get; private set; }

        public int MaxRespawnTime { get; private set; }

        public int SecondsToSpawn { get; private set; }

        internal SpawnerInfo(string id, string typeName, bool respawnEnabled, float chance, int spawnedCount, RespawnTime spawnTime, bool useCustomSpawnTime, int minRespawnTime, int maxRespawnTime, int secondsToSpawn)
        {
            Id = id;
            TypeName = typeName;
            RespawnEnabled = respawnEnabled;
            Chance = chance;
            SpawnedCount = spawnedCount;
            SpawnTime = spawnTime;
            UseCustomSpawnTime = useCustomSpawnTime;
            MinRespawnTime = minRespawnTime;
            MaxRespawnTime = maxRespawnTime;
            SecondsToSpawn = secondsToSpawn;
        }
    }

    public sealed class SpawnerArgs
    {
        public string Id { get; private set; }

        public string TypeName { get; private set; }

        public int SpawnedCount { get; private set; }

        internal SpawnerArgs(string id, string typeName, int spawnedCount)
        {
            Id = id;
            TypeName = typeName;
            SpawnedCount = spawnedCount;
        }
    }

    public static class Spawners
    {
        internal const string Owner = "obenseuer.moddingframework.spawners";
        private static readonly FieldInfo objectsField = AccessTools.Field(typeof(Spawner), "objects");
        private static bool installed;

        public static event Action<SpawnerArgs> Spawned;

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        public static List<SpawnerInfo> All()
        {
            List<SpawnerInfo> list = new List<SpawnerInfo>();
            try
            {
                foreach (Spawner spawner in SceneIds.Active<Spawner>())
                {
                    list.Add(ToInfo(spawner));
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Spawners.All", e);
            }
            return list;
        }

        public static SpawnerInfo Get(string id)
        {
            try
            {
                Spawner spawner = SceneIds.Find<Spawner>(id);
                return spawner == null ? null : ToInfo(spawner);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Spawners.Get", e);
                return null;
            }
        }

        public static bool SetRespawn(string id, bool enabled, float chance)
        {
            try
            {
                Spawner spawner = SceneIds.Find<Spawner>(id);
                if (spawner == null || spawner.spawnSettings == null || SaveGate.Busy) return false;
                spawner.spawnSettings.respawnEnabled = enabled;
                spawner.spawnSettings.spawnChance = Mathf.Clamp01(chance);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Spawners.SetRespawn", e);
                return false;
            }
        }

        public static bool SetRespawnTiming(string id, RespawnTime time, bool useCustomSpawnTime, int minSeconds, int maxSeconds)
        {
            try
            {
                Spawner spawner = SceneIds.Find<Spawner>(id);
                if (spawner == null || spawner.spawnSettings == null || SaveGate.Busy) return false;
                if (!Enum.IsDefined(typeof(RespawnTime), time) || minSeconds < 0 || maxSeconds < minSeconds) return false;
                SpawnSettings settings = spawner.spawnSettings;
                settings.spawnTime = GameTime(time);
                settings.useCustomSpawnTime = useCustomSpawnTime;
                settings.minRespawnTime = minSeconds;
                settings.maxRespawnTime = maxSeconds;
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Spawners.SetRespawnTiming", e);
                return false;
            }
        }

        public static bool TriggerNow(string id)
        {
            try
            {
                Spawner spawner = SceneIds.Find<Spawner>(id);
                if (spawner == null || spawner.spawnSettings == null || SaveGate.Busy) return false;
                return spawner.TrySpawn();
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Spawners.TriggerNow", e);
                return false;
            }
        }

        internal static void Init()
        {
            installed = SpawnersPatches.Install();
            if (!installed) CorePatcher.DisableOwner(Owner);
        }

        internal static void RaiseSpawned(Spawner spawner)
        {
            if (Spawned == null) return;
            SpawnerArgs args = new SpawnerArgs(SceneIds.Of(spawner), spawner.GetType().Name, SpawnedCount(spawner));
            Events.Raise(Spawned, "Spawners.Spawned", h => h(args));
        }

        private static SpawnerInfo ToInfo(Spawner spawner)
        {
            SpawnSettings settings = spawner.spawnSettings;
            if (settings == null) return new SpawnerInfo(SceneIds.Of(spawner), spawner.GetType().Name, false, 0f, SpawnedCount(spawner), RespawnTime.Default, false, 0, 0, 0);
            return new SpawnerInfo(SceneIds.Of(spawner), spawner.GetType().Name, settings.respawnEnabled, settings.spawnChance, SpawnedCount(spawner), ModTime(settings.spawnTime), settings.useCustomSpawnTime, settings.minRespawnTime, settings.maxRespawnTime, settings.timetospawn);
        }

        private static RespawnTime ModTime(SpawnSettings.SpawnTime time)
        {
            switch (time)
            {
                case SpawnSettings.SpawnTime.VeryShort: return RespawnTime.VeryShort;
                case SpawnSettings.SpawnTime.Short: return RespawnTime.Short;
                case SpawnSettings.SpawnTime.Long: return RespawnTime.Long;
                case SpawnSettings.SpawnTime.VeryLong: return RespawnTime.VeryLong;
                case SpawnSettings.SpawnTime.EveryDay: return RespawnTime.EveryDay;
                default: return RespawnTime.Default;
            }
        }

        private static SpawnSettings.SpawnTime GameTime(RespawnTime time)
        {
            switch (time)
            {
                case RespawnTime.VeryShort: return SpawnSettings.SpawnTime.VeryShort;
                case RespawnTime.Short: return SpawnSettings.SpawnTime.Short;
                case RespawnTime.Long: return SpawnSettings.SpawnTime.Long;
                case RespawnTime.VeryLong: return SpawnSettings.SpawnTime.VeryLong;
                case RespawnTime.EveryDay: return SpawnSettings.SpawnTime.EveryDay;
                default: return SpawnSettings.SpawnTime.Default;
            }
        }

        private static int SpawnedCount(Spawner spawner)
        {
            if (objectsField == null) return 0;
            int count = 0;
            foreach (GameObject item in (List<GameObject>)objectsField.GetValue(spawner))
            {
                if (item != null) count++;
            }
            return count;
        }
    }
}
