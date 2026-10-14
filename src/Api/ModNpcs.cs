using System;
using System.Collections.Generic;
using NPC;
using PixelCrushers.DialogueSystem;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ObenseuerCore
{
    public sealed class ModNpcInfo
    {
        public string ModId { get; private set; }
        public string Id { get; private set; }
        public int CharacterId { get; private set; }
        public int SourceCharacterId { get; private set; }
        public string Name { get; private set; }
        public string DisplayName { get; private set; }
        public Vector3 Position { get; private set; }
        public bool Alive { get; private set; }

        internal ModNpcInfo(string modId, string id, int characterId, int sourceCharacterId, string name, string displayName, Vector3 position, bool alive)
        {
            ModId = modId;
            Id = id;
            CharacterId = characterId;
            SourceCharacterId = sourceCharacterId;
            Name = name;
            DisplayName = displayName;
            Position = position;
            Alive = alive;
        }
    }

    public static class ModNpcs
    {
        internal const string Owner = "obenseuer.moddingframework.modnpcs";
        private const int MaxIdLength = 64;
        private const int BaseId = 1000000;
        private const int IdSpan = 1000000;
        private const float GroundLift = 1f;
        private const float GroundReach = 4f;
        private const float RemovalDelay = 0.5f;
        private static readonly List<Record> records = new List<Record>();
        private static readonly HashSet<string> used = new HashSet<string>();
        private static readonly Dictionary<string, SavedNpcLook> looks = new Dictionary<string, SavedNpcLook>();
        private static SavedNpcFile saved = new SavedNpcFile();

        public static bool IsAvailable
        {
            get
            {
                return !CorePatcher.IsDisabled(Owner);
            }
        }

        public static int Spawn(string modId, string npcId, int sourceCharacterId, string name, string lastName, Vector3 position, float yaw, bool keepSourceDialogue = false)
        {
            ModIds.Require(modId, "modId");
            RequireSpawn(npcId, name, position, yaw);
            if (!IsAvailable || SaveGate.Busy || NPCManager.instance == null) return 0;
            try
            {
                Prune();
                if (Find(modId, npcId) != null) return 0;
                return Build(modId, npcId, sourceCharacterId, name, lastName, position, yaw, keepSourceDialogue, Remembered(modId, npcId), false);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "ModNpcs.Spawn", e);
                return 0;
            }
        }

        public static int SpawnSaved(string modId, string npcId, int sourceCharacterId, string name, string lastName, Vector3 position, float yaw, string scene, bool keepSourceDialogue = false)
        {
            ModIds.Require(modId, "modId");
            RequireSpawn(npcId, name, position, yaw);
            if (string.IsNullOrEmpty(scene)) throw new ArgumentException("scene must be a non-empty string", "scene");
            if (!IsAvailable || SaveGate.Busy || NPCManager.instance == null || SceneManager.GetActiveScene().name != scene) return 0;
            try
            {
                Prune();
                if (Find(modId, npcId) != null || !saved.Room(modId, npcId)) return 0;
                SavedNpcLook look = saved.LookOf(modId, npcId);
                int id = Build(modId, npcId, sourceCharacterId, name, lastName, position, yaw, keepSourceDialogue, look, true);
                if (id == 0) return 0;
                used.Add(modId);
                saved.Put(new SavedNpcEntry { Mod = modId, NpcId = npcId, Source = sourceCharacterId, Name = name, LastName = lastName ?? "", X = position.x, Y = position.y, Z = position.z, Yaw = yaw, Scene = scene, KeepDialogue = keepSourceDialogue, Look = look });
                return id;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "ModNpcs.SpawnSaved", e);
                return 0;
            }
        }

        public static bool Despawn(string modId, string npcId)
        {
            ModIds.Require(modId, "modId");
            if (string.IsNullOrEmpty(npcId)) return false;
            try
            {
                Record record = Find(modId, npcId);
                int forgotten = saved.Count(modId, npcId);
                if (record == null && forgotten == 0) return false;
                if (forgotten > 0 && SaveGate.Busy) return false;
                if (record != null) Release(record, true);
                saved.Drop(modId, npcId);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "ModNpcs.Despawn", e);
                return false;
            }
        }

        public static bool Clear(string modId)
        {
            ModIds.Require(modId, "modId");
            try
            {
                if (SaveGate.Busy && saved.Count(modId, null) > 0) return false;
                List<Record> mine = records.FindAll(r => r.ModId == modId);
                foreach (Record record in mine) Release(record, true);
                int forgotten = saved.Drop(modId, null);
                ForgetLooks(modId);
                return mine.Count > 0 || forgotten > 0;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "ModNpcs.Clear", e);
                return false;
            }
        }

        public static int CharacterIdOf(string modId, string npcId)
        {
            ModIds.Require(modId, "modId");
            if (string.IsNullOrEmpty(npcId)) return 0;
            Record record = Find(modId, npcId);
            return record == null ? 0 : record.Clone.ID;
        }

        public static List<ModNpcInfo> List(string modId)
        {
            ModIds.Require(modId, "modId");
            List<ModNpcInfo> list = new List<ModNpcInfo>();
            try
            {
                Prune();
                foreach (Record record in records)
                {
                    if (record.ModId != modId) continue;
                    list.Add(new ModNpcInfo(record.ModId, record.NpcId, record.Clone.ID, record.SourceId, record.Clone.Name, record.Clone.FullName, record.Go.transform.position, !record.Info.IsDead()));
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "ModNpcs.List", e);
            }
            return list;
        }

        internal static void Init()
        {
            Events.SceneLoaded += OnSceneLoaded;
            RegisterStore();
        }

        internal static Character CloneOf(int characterId)
        {
            foreach (Record record in records)
            {
                if (record.Clone != null && record.Clone.ID == characterId) return record.Clone;
            }
            return null;
        }

        private static int Build(string modId, string npcId, int sourceId, string name, string lastName, Vector3 position, float yaw, bool keepDialogue, SavedNpcLook look, bool inFile)
        {
            if (CharacterDatabase.Characters.Count == 0) return 0;
            Character source = FindSource(sourceId);
            GameObject prefab = source == null ? null : (source.prefabWithAi != null ? source.prefabWithAi : source.prefab);
            if (prefab == null) return 0;
            bool sliders = prefab.GetComponentInChildren<Appearance>(true) != null;
            Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);
            RaycastHit hit;
            if (!Physics.Raycast(position + Vector3.up * GroundLift, Vector3.down, out hit, GroundReach, ~0, QueryTriggerInteraction.Ignore))
            {
                ApiLog.Once(Owner, "no ground", "no collider within " + GroundReach + " below the spawn position, the NPC was not spawned");
                return 0;
            }
            Character clone = UnityEngine.Object.Instantiate(source);
            clone.name = modId + "/" + npcId;
            clone.ID = Allocate(modId, npcId);
            clone.Name = name;
            clone.LastName = lastName ?? "";
            clone.resident = null;
            if (look != null) ModNpcsLook.Write(clone, look);
            GameObject holder = new GameObject("Framework NPC holder");
            holder.SetActive(false);
            GameObject go = null;
            bool listed = false;
            try
            {
                Vector3 at = NPCSpawnUtilities.FindValidSpawnPosition(hit.point, clone.FullName, 0.25f, 1.8f, rotation);
                go = UnityEngine.Object.Instantiate(prefab, at, rotation, holder.transform);
                NPCInfo info = go.GetComponent<NPCInfo>();
                if (info == null) throw new InvalidOperationException("the prefab of " + source.FullName + " has no NPCInfo");
                Configure(go, info, source, clone, keepDialogue);
                CharacterDatabase.Characters.Add(clone);
                listed = true;
                go.name = clone.name;
                go.transform.SetParent(null, true);
                UnityEngine.Object.DestroyImmediate(holder);
                holder = null;
                NPCManager.instance.AddNpcToList(info);
                records.Add(new Record { ModId = modId, NpcId = npcId, SourceId = source.ID, Source = source, Sliders = sliders, InFile = inFile, Clone = clone, Go = go, Info = info });
                return clone.ID;
            }
            catch
            {
                if (listed) CharacterDatabase.Characters.Remove(clone);
                if (go != null) UnityEngine.Object.DestroyImmediate(go);
                if (holder != null) UnityEngine.Object.DestroyImmediate(holder);
                UnityEngine.Object.Destroy(clone);
                throw;
            }
        }

        private static void Configure(GameObject go, NPCInfo info, Character source, Character clone, bool keepDialogue)
        {
            foreach (MonoBehaviour part in go.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (part != null) SaveController.SetObjectGUID(part, "");
            }
            info.character = clone;
            NPCController controller = info.npcController != null ? info.npcController : go.GetComponent<NPCController>();
            if (controller != null) controller.character = clone;
            foreach (Appearance appearance in go.GetComponentsInChildren<Appearance>(true)) appearance.createdCharacter = clone;
            foreach (CharacterAppearance appearance in go.GetComponentsInChildren<CharacterAppearance>(true))
            {
                if (appearance.createdCharacter == source) appearance.createdCharacter = clone;
            }
            foreach (InteractableTalk talk in go.GetComponentsInChildren<InteractableTalk>(true))
            {
                talk.title = clone.FullName;
                if (!keepDialogue) talk.talkDisabled = true;
            }
            foreach (BarkStarter bark in go.GetComponentsInChildren<BarkStarter>(true)) bark.enabled = false;
            foreach (DialogueSystemTrigger trigger in go.GetComponentsInChildren<DialogueSystemTrigger>(true))
            {
                if (!string.IsNullOrEmpty(trigger.barkConversation)) trigger.enabled = false;
            }
            foreach (PersistentPositionData position in go.GetComponentsInChildren<PersistentPositionData>(true)) UnityEngine.Object.DestroyImmediate(position);
            foreach (IncrementOnDestroy increment in go.GetComponentsInChildren<IncrementOnDestroy>(true)) UnityEngine.Object.DestroyImmediate(increment);
            foreach (PersistentDestructible destructible in go.GetComponentsInChildren<PersistentDestructible>(true)) UnityEngine.Object.DestroyImmediate(destructible);
        }

        private static Character FindSource(int id)
        {
            Character player = CharacterDatabase.Characters[0];
            foreach (Character character in CharacterDatabase.Characters)
            {
                if (character != null && character != player && character.ID == id && CloneOf(id) == null) return character;
            }
            return null;
        }

        private static int Allocate(string modId, string npcId)
        {
            uint hash = 2166136261u;
            foreach (char c in modId + "/" + npcId) hash = unchecked((hash ^ c) * 16777619u);
            int id = BaseId + (int)(hash % IdSpan);
            while (CharacterDatabase.Characters.Exists(c => c != null && c.ID == id)) id++;
            return id;
        }

        internal static bool HasSaved(string modId, string npcId)
        {
            return saved.Count(modId, npcId) > 0;
        }

        private static void Release(Record record, bool destroyNpc)
        {
            KeepLook(record);
            records.Remove(record);
            ModNpcRoutines.EndAll(record.ModId, record.NpcId);
            NpcAppearance.DropNpc(record.Clone.ID);
            CharacterDatabase.Characters.Remove(record.Clone);
            if (record.Go != null)
            {
                if (NPCManager.instance != null && record.Info != null) NPCManager.instance.RemoveNpcFromList(record.Info);
                if (destroyNpc)
                {
                    record.Go.SetActive(false);
                    UnityEngine.Object.Destroy(record.Go, RemovalDelay);
                }
            }
            UnityEngine.Object.Destroy(record.Clone);
        }

        private static SavedNpcFile Snapshot()
        {
            foreach (Record record in records) KeepLook(record);
            return saved;
        }

        private static void KeepLook(Record record)
        {
            try
            {
                SavedNpcLook look = ModNpcsLook.Capture(record.Clone, record.Source, record.Sliders);
                if (record.InFile) saved.SetLook(record.ModId, record.NpcId, look);
                else Remember(record.ModId, record.NpcId, look);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "ModNpcs.KeepLook", e);
            }
        }

        private static SavedNpcLook Remembered(string modId, string npcId)
        {
            SavedNpcLook look;
            return looks.TryGetValue(modId + "/" + npcId, out look) ? look : null;
        }

        private static void Remember(string modId, string npcId, SavedNpcLook look)
        {
            if (look == null) looks.Remove(modId + "/" + npcId);
            else looks[modId + "/" + npcId] = look;
        }

        private static void ForgetLooks(string modId)
        {
            string prefix = modId + "/";
            foreach (string key in new List<string>(looks.Keys))
            {
                if (key.StartsWith(prefix, StringComparison.Ordinal)) looks.Remove(key);
            }
        }

        internal static bool ForgetLook(string modId, string npcId)
        {
            if (SaveGate.Busy) return false;
            bool remembered = looks.Remove(modId + "/" + npcId);
            bool inFile = saved.SetLook(modId, npcId, null);
            Record record = Find(modId, npcId);
            if (record != null) ResetCopy(record);
            return remembered || inFile;
        }

        private static void ResetCopy(Record record)
        {
            ModNpcsLook.Reset(record.Clone, record.Source, record.Sliders);
            Appearance ap = record.Go.GetComponentInChildren<Appearance>(true);
            if (record.Sliders && PlayerLook.IsReady(ap)) ap.LoadCharacter();
        }

        private static void Prune()
        {
            List<Record> gone = records.FindAll(r => r.Go == null);
            foreach (Record record in gone) Release(record, false);
        }

        private static void RequireSpawn(string npcId, string name, Vector3 position, float yaw)
        {
            if (string.IsNullOrEmpty(npcId) || npcId.Length > MaxIdLength) throw new ArgumentException("npcId must be 1 to " + MaxIdLength + " characters", "npcId");
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("name must be a non-empty string", "name");
            if (!Finite(position) || float.IsNaN(yaw) || float.IsInfinity(yaw)) throw new ArgumentException("position and yaw must be finite", "position");
        }

        private static void OnSceneLoaded(string scene)
        {
            try
            {
                Prune();
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "ModNpcs.SceneLoaded", e);
            }
            Events.WhenReady(() => RestoreSaved(scene));
        }

        private static void RestoreSaved(string scene)
        {
            if (SceneManager.GetActiveScene().name != scene) return;
            if (SaveGate.Busy || NPCManager.instance == null) return;
            try
            {
                Prune();
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "ModNpcs.RestoreSaved prune", e);
            }
            foreach (SavedNpcEntry entry in saved.Restorable(scene, m => ModIds.IsInstalled(m, used)))
            {
                try
                {
                    if (Find(entry.Mod, entry.NpcId) != null) continue;
                    Build(entry.Mod, entry.NpcId, entry.Source, entry.Name, entry.LastName, new Vector3(entry.X, entry.Y, entry.Z), entry.Yaw, entry.KeepDialogue, entry.Look, true);
                }
                catch (Exception e)
                {
                    ApiLog.Once(Owner, "ModNpcs.RestoreSaved", e);
                }
            }
        }

        private static void RegisterStore()
        {
            try
            {
                SaveData.Register<SavedNpcFile>(Owner, Snapshot, file =>
                {
                    saved = SavedNpcFile.Clean(file);
                    looks.Clear();
                });
            }
            catch (Exception e)
            {
                CoreLog.For(Owner).Error("mod npc save data not registered, saved mod NPCs last for the session only: " + e);
            }
        }

        private static Record Find(string modId, string npcId)
        {
            return records.Find(r => r.ModId == modId && r.NpcId == npcId && r.Go != null);
        }

        private static bool Finite(Vector3 v)
        {
            return IsNumber(v.x) && IsNumber(v.y) && IsNumber(v.z);
        }

        private static bool IsNumber(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private sealed class Record
        {
            internal string ModId;
            internal string NpcId;
            internal int SourceId;
            internal Character Clone;
            internal GameObject Go;
            internal NPCInfo Info;
            internal Character Source;
            internal bool Sliders;
            internal bool InFile;
        }
    }
}
