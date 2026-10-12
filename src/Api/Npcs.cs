using System;
using System.Collections.Generic;
using NPC;
using UnityEngine;

namespace ObenseuerCore
{
    public sealed class NpcInfo
    {
        public int Id { get; private set; }
        public string Name { get; private set; }
        public string DisplayName { get; private set; }
        public Vector3 Position { get; private set; }
        public bool Alive { get; private set; }
        public string CurrentScene { get; private set; }

        internal NpcInfo(int id, string name, string displayName, Vector3 position, bool alive, string currentScene)
        {
            Id = id;
            Name = name;
            DisplayName = displayName;
            Position = position;
            Alive = alive;
            CurrentScene = currentScene;
        }
    }

    public static class Npcs
    {
        internal const string Owner = "obenseuer.moddingframework.npcs";
        private static bool installed;
        public static event Action<NpcInfo> Spawned;
        public static event Action<NpcInfo> Despawned;
        public static event Action<NpcInfo> Died;

        public static List<NpcInfo> All()
        {
            try
            {
                List<NpcInfo> result = new List<NpcInfo>();
                List<NPCData> database = NPCDirector.globalDatabase;
                if (database == null) return result;
                foreach (NPCData data in database)
                {
                    if (data != null) result.Add(Snapshot(data, LiveFor(data.ID)));
                }
                return result;
            }
            catch (Exception e)
            {
                ApiLog.Once(Npcs.Owner, "Npcs.All", e);
                return new List<NpcInfo>();
            }
        }

        public static NpcInfo Get(int id)
        {
            try
            {
                NPCData data = FindData(id);
                return data == null ? null : Snapshot(data, LiveFor(id));
            }
            catch (Exception e)
            {
                ApiLog.Once(Npcs.Owner, "Npcs.Get", e);
                return null;
            }
        }

        public static NpcInfo Find(string name)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("name must be a non-empty string", "name");
            try
            {
                foreach (NpcInfo info in All())
                {
                    if (string.Equals(info.Name, name, StringComparison.OrdinalIgnoreCase) || string.Equals(info.DisplayName, name, StringComparison.OrdinalIgnoreCase)) return info;
                }
                return null;
            }
            catch (Exception e)
            {
                ApiLog.Once(Npcs.Owner, "Npcs.Find", e);
                return null;
            }
        }

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Npcs.Owner);
            }
        }

        internal static void Init()
        {
            installed = NpcsPatches.Install();
        }

        internal static void RaiseSpawned(NPCInfo npc)
        {
            NpcInfo info = FromScene(npc);
            if (info != null) Events.Raise(Spawned, "Npcs.Spawned", h => h(info));
        }

        internal static void RaiseDespawned(NPCInfo npc)
        {
            NpcInfo info = FromScene(npc);
            if (info != null) Events.Raise(Despawned, "Npcs.Despawned", h => h(info));
        }

        internal static void RaiseDied(NPCInfo npc)
        {
            NpcInfo info = FromScene(npc);
            if (info != null) Events.Raise(Died, "Npcs.Died", h => h(info));
        }

        private static NpcInfo FromScene(NPCInfo npc)
        {
            if (npc == null || npc.character == null) return null;
            NPCData data = FindData(npc.character.ID);
            return data == null ? null : Snapshot(data, npc);
        }

        private static NPCData FindData(int id)
        {
            return NPCDirector.instance == null ? null : NPCDirector.instance.GetNPCDataByID(id);
        }

        private static NPCInfo LiveFor(int id)
        {
            return NPCManager.instance == null ? null : NPCManager.instance.FindNpcById(id);
        }

        private static NpcInfo Snapshot(NPCData data, NPCInfo live)
        {
            Character character = data.character;
            bool inScene = live != null && live.gameObject.activeInHierarchy;
            Vector3 position = inScene ? live.transform.position : (data.state == null ? Vector3.zero : data.state.currentPosition);
            string scene = data.state == null ? null : data.state.currentScene;
            bool alive = live == null || !live.IsDead();
            return new NpcInfo(data.ID, character == null ? null : character.Name, character == null ? null : character.FullName, position, alive, scene);
        }
    }
}
