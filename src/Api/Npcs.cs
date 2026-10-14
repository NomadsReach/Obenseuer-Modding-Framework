using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
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

    public sealed class SceneNpcInfo
    {
        public string Scene { get; private set; }
        public string Path { get; private set; }
        public string Handle { get; private set; }
        public bool IsDirector { get; private set; }
        public int Id { get; private set; }
        public string Name { get; private set; }
        public string DisplayName { get; private set; }
        public Vector3 Position { get; private set; }
        public bool Active { get; private set; }
        public bool Present { get; private set; }

        internal SceneNpcInfo(string scene, string path, string handle, bool isDirector, int id, string name, string displayName, Vector3 position, bool active, bool present)
        {
            Scene = scene;
            Path = path;
            Handle = handle;
            IsDirector = isDirector;
            Id = id;
            Name = name;
            DisplayName = displayName;
            Position = position;
            Active = active;
            Present = present;
        }
    }

    public static class Npcs
    {
        internal const string Owner = "obenseuer.moddingframework.npcs";
        internal const string DiedOwner = Owner + ".died";
        internal const string RelationshipOwner = Owner + ".relationship";
        private static bool installed;
        private static bool diedInstalled;
        private static bool relationshipInstalled;
        private static readonly FieldInfo culledField = AccessTools.Field(typeof(FadeGameObject), "culled");
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

        public static List<SceneNpcInfo> InScene()
        {
            try
            {
                List<SceneNpcInfo> result = new List<SceneNpcInfo>();
                foreach (NPCInfo npc in UnityEngine.Object.FindObjectsByType<NPCInfo>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (npc != null && npc.gameObject.scene.isLoaded) result.Add(SceneSnapshot(npc));
                }
                return result;
            }
            catch (Exception e)
            {
                ApiLog.Once(Npcs.Owner, "Npcs.InScene", e);
                return new List<SceneNpcInfo>();
            }
        }

        public static bool IsAvailable
        {
            get
            {
                return installed && diedInstalled && !CorePatcher.IsDisabled(Npcs.Owner) && !CorePatcher.IsDisabled(DiedOwner);
            }
        }

        internal static bool RelationshipAvailable
        {
            get
            {
                return relationshipInstalled && !CorePatcher.IsDisabled(RelationshipOwner);
            }
        }

        internal static void Init()
        {
            installed = NpcsPatches.InstallSpawn();
            diedInstalled = NpcsPatches.InstallDied();
            relationshipInstalled = NpcsPatches.InstallRelationship();
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

        private static SceneNpcInfo SceneSnapshot(NPCInfo npc)
        {
            GameObject go = npc.gameObject;
            Character character = npc.character;
            int id = character == null ? -1 : character.ID;
            NPCData data = character == null ? null : FindData(id);
            return new SceneNpcInfo(go.scene.name, WorldObjects.PathOf(go), SceneIds.Of(npc), data != null, id, character == null ? go.name : character.Name, character == null ? null : character.FullName, go.transform.position, go.activeInHierarchy, PresentIn(npc, data));
        }

        private static bool PresentIn(NPCInfo npc, NPCData data)
        {
            if (npc.gameObject.activeInHierarchy) return true;
            FadeGameObject fade = npc.GetComponent<FadeGameObject>();
            if (fade == null || fade.hideAlways || !(fade.isHidden || Culled(fade))) return false;
            Transform parent = npc.transform.parent;
            if (parent != null && !parent.gameObject.activeInHierarchy) return false;
            return data == null || (data.state != null && data.state.currentScene == npc.gameObject.scene.name);
        }

        private static bool Culled(FadeGameObject fade)
        {
            if (culledField == null)
            {
                ApiLog.Once(Owner, "Npcs.Present", "FadeGameObject.culled was not found, so a culled NPC reads as not present");
                return false;
            }
            return (bool)culledField.GetValue(fade);
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
