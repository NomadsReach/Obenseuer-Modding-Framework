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
        public int BodyCharacterId { get; private set; }
        public string Name { get; private set; }
        public string DisplayName { get; private set; }
        public Vector3 Position { get; private set; }
        public bool Alive { get; private set; }

        internal ModNpcInfo(string modId, string id, int characterId, int sourceCharacterId, int bodyCharacterId, string name, string displayName, Vector3 position, bool alive)
        {
            ModId = modId;
            Id = id;
            CharacterId = characterId;
            SourceCharacterId = sourceCharacterId;
            BodyCharacterId = bodyCharacterId;
            Name = name;
            DisplayName = displayName;
            Position = position;
            Alive = alive;
        }
    }

    public static class ModNpcs
    {
        internal const string Owner = "obenseuer.moddingframework.modnpcs";
        internal const int MaxIdLength = 64;
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
            return Spawn(modId, npcId, sourceCharacterId, 0, name, lastName, position, yaw, keepSourceDialogue);
        }

        public static int Spawn(string modId, string npcId, int sourceCharacterId, int bodyCharacterId, string name, string lastName, Vector3 position, float yaw, bool keepSourceDialogue)
        {
            ModIds.Require(modId, "modId");
            RequireSpawn(npcId, name, position, yaw);
            if (!IsAvailable || SaveGate.Busy || NPCManager.instance == null) return 0;
            try
            {
                Prune();
                if (Find(modId, npcId) != null) return 0;
                return Build(modId, npcId, sourceCharacterId, bodyCharacterId, name, lastName, position, yaw, keepSourceDialogue, Remembered(modId, npcId), false);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "ModNpcs.Spawn", e);
                return 0;
            }
        }

        public static int SpawnSaved(string modId, string npcId, int sourceCharacterId, string name, string lastName, Vector3 position, float yaw, string scene, bool keepSourceDialogue = false)
        {
            return SpawnSaved(modId, npcId, sourceCharacterId, 0, name, lastName, position, yaw, scene, keepSourceDialogue);
        }

        public static int SpawnSaved(string modId, string npcId, int sourceCharacterId, int bodyCharacterId, string name, string lastName, Vector3 position, float yaw, string scene, bool keepSourceDialogue)
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
                int id = Build(modId, npcId, sourceCharacterId, bodyCharacterId, name, lastName, position, yaw, keepSourceDialogue, look, true);
                if (id == 0) return 0;
                used.Add(modId);
                saved.Put(new SavedNpcEntry { Mod = modId, NpcId = npcId, Source = sourceCharacterId, Body = bodyCharacterId, Name = name, LastName = lastName ?? "", X = position.x, Y = position.y, Z = position.z, Yaw = yaw, Scene = scene, KeepDialogue = keepSourceDialogue, Look = look });
                return id;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "ModNpcs.SpawnSaved", e);
                return 0;
            }
        }

        public static int SpawnWithLook(string modId, string npcId, int sourceCharacterId, SceneLook look, string name, string lastName, Vector3 position, float yaw)
        {
            return SpawnWithLook(modId, npcId, sourceCharacterId, 0, look, name, lastName, position, yaw);
        }

        public static int SpawnWithLook(string modId, string npcId, int sourceCharacterId, int bodyCharacterId, SceneLook look, string name, string lastName, Vector3 position, float yaw)
        {
            ModIds.Require(modId, "modId");
            RequireSpawn(npcId, name, position, yaw);
            try
            {
                if (look == null || !look.Ready)
                {
                    ApiLog.Once(Owner, "SpawnWithLook " + modId + "/" + npcId, look == null ? "no look" : "the look is not ready: " + look.Reason);
                    return 0;
                }
                int id = Spawn(modId, npcId, sourceCharacterId, bodyCharacterId, name, lastName, position, yaw, false);
                Record record = id == 0 ? null : Find(modId, npcId);
                GameObject body = record == null ? null : record.Go;
                string reason = null;
                bool applied = body != null && SceneLooks.Apply(modId, look, body, out reason);
                SceneLookLiveAction action = SceneLookLivePlan.Spawn(id != 0, applied);
                if (action == SceneLookLiveAction.Keep) return id;
                if (action == SceneLookLiveAction.Refuse) return 0;
                Discard(modId, npcId);
                ApiLog.Once(Owner, "SpawnWithLook " + modId + "/" + npcId, reason == null ? "the look was not applied" : reason);
                return 0;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "ModNpcs.SpawnWithLook", e);
                return 0;
            }
        }

        internal static bool Discard(string modId, string npcId)
        {
            Record record = Find(modId, npcId);
            if (record == null) return false;
            Release(record, true, false);
            return true;
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

        public static bool MoveTo(string modId, string npcId, Vector3 position)
        {
            ModIds.Require(modId, "modId");
            if (string.IsNullOrEmpty(npcId)) return false;
            if (!Finite(position))
            {
                ApiLog.Once(Owner, "ModNpcs.MoveTo position", "position must be finite, so the NPC was not moved");
                return false;
            }
            try
            {
                Record record = Find(modId, npcId);
                if (record == null)
                {
                    ApiLog.Once(Owner, "ModNpcs.MoveTo " + modId + " " + npcId, "refused: no NPC " + npcId + " of " + modId + " is loaded");
                    return false;
                }
                RaycastHit hit;
                if (!Ground(position, out hit)) return false;
                bool moved = NpcActions.Teleport(record.Clone.ID, hit.point);
                if (!moved) ApiLog.Once(Owner, "ModNpcs.MoveTo not moved " + modId + " " + npcId, "refused: " + npcId + " is dead, inactive or has no movement agent, so it was not moved");
                return moved;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "ModNpcs.MoveTo", e);
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
                    list.Add(new ModNpcInfo(record.ModId, record.NpcId, record.Clone.ID, record.SourceId, record.BodyId, record.Clone.Name, record.Clone.FullName, record.Go.transform.position, !record.Info.IsDead()));
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "ModNpcs.List", e);
            }
            return list;
        }

        public static GameObject SpawnExtra(string modId, string extraId, int sourceCharacterId, int bodyCharacterId, Vector3 position, float yaw)
        {
            ModIds.Require(modId, "modId");
            ModNpcExtras.RequireExtra(extraId, position, yaw);
            return ModNpcExtras.Spawn(modId, extraId, sourceCharacterId, bodyCharacterId, position, yaw);
        }

        public static bool DespawnExtra(string modId, string extraId)
        {
            ModIds.Require(modId, "modId");
            return ModNpcExtras.Despawn(modId, extraId);
        }

        public static int ClearExtras(string modId)
        {
            ModIds.Require(modId, "modId");
            return ModNpcExtras.Clear(modId);
        }

        public static GameObject ExtraObject(string modId, string extraId)
        {
            ModIds.Require(modId, "modId");
            return ModNpcExtras.ObjectOf(modId, extraId);
        }

        internal static void Init()
        {
            Events.SceneLoaded += OnSceneLoaded;
            RegisterStore();
            ModNpcExtras.Init();
        }

        internal static Character CloneOf(int characterId)
        {
            foreach (Record record in records)
            {
                if (record.Clone != null && record.Clone.ID == characterId) return record.Clone;
            }
            return null;
        }

        private static int Build(string modId, string npcId, int sourceId, int bodyId, string name, string lastName, Vector3 position, float yaw, bool keepDialogue, SavedNpcLook look, bool inFile)
        {
            if (CharacterDatabase.Characters.Count == 0) return 0;
            Character source = FindSource(sourceId);
            Character body = source == null ? null : FindBody(source, bodyId);
            GameObject prefab = body == null ? null : PrefabOf(body);
            if (prefab == null) return 0;
            bool sliders = prefab.GetComponentInChildren<Appearance>(true) != null;
            Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);
            RaycastHit hit;
            if (!Ground(position, out hit)) return 0;
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
                if (info == null) throw new InvalidOperationException("the prefab of " + body.FullName + " has no NPCInfo");
                Configure(go, info, source, body, clone, keepDialogue);
                CharacterDatabase.Characters.Add(clone);
                listed = true;
                go.name = clone.name;
                go.transform.SetParent(null, true);
                UnityEngine.Object.DestroyImmediate(holder);
                holder = null;
                NPCManager.instance.AddNpcToList(info);
                records.Add(new Record { ModId = modId, NpcId = npcId, SourceId = source.ID, Source = source, BodyId = body == source ? 0 : body.ID, Sliders = sliders, InFile = inFile, Clone = clone, Go = go, Info = info });
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

        private static void Configure(GameObject go, NPCInfo info, Character source, Character body, Character clone, bool keepDialogue)
        {
            foreach (MonoBehaviour part in go.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (part != null) SaveController.SetObjectGUID(part, "");
            }
            info.character = clone;
            NPCController controller = info.npcController != null ? info.npcController : go.GetComponent<NPCController>();
            if (controller != null) controller.character = clone;
            Repoint(go, source, body, clone);
            bool otherBody = body != source;
            if (keepDialogue && otherBody) ApiLog.Once(Owner, "keepSourceDialogue other body", "keepSourceDialogue is ignored when the body is not the source, the copy keeps its talk disabled");
            foreach (InteractableTalk talk in go.GetComponentsInChildren<InteractableTalk>(true))
            {
                talk.title = clone.FullName;
                if (!keepDialogue || otherBody) talk.talkDisabled = true;
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

        internal static void Repoint(GameObject go, Character source, Character body, Character clone)
        {
            foreach (Appearance appearance in go.GetComponentsInChildren<Appearance>(true)) appearance.createdCharacter = clone;
            foreach (CharacterAppearance appearance in go.GetComponentsInChildren<CharacterAppearance>(true))
            {
                if (appearance.createdCharacter == source) appearance.createdCharacter = clone;
                else if (appearance.createdCharacter == body && body != source)
                {
                    if (MeshesMatch(clone, appearance.transform)) appearance.createdCharacter = clone;
                    else ApiLog.Once(Owner, "body rig kept", "the body rig has no mesh of the copy's look, so it keeps the body's character and the copy shows the body's look on that rig");
                }
            }
        }

        internal static bool Ground(Vector3 position, out RaycastHit hit)
        {
            if (Physics.Raycast(position + Vector3.up * GroundLift, Vector3.down, out hit, GroundReach, ~0, QueryTriggerInteraction.Ignore)) return true;
            ApiLog.Once(Owner, "no ground", "no ground found at the position: no collider within " + GroundReach + " below it");
            return false;
        }

        internal static Character FindSource(int id)
        {
            Character player = CharacterDatabase.Characters[0];
            foreach (Character character in CharacterDatabase.Characters)
            {
                if (character != null && character != player && character.ID == id && CloneOf(id) == null) return character;
            }
            return null;
        }

        internal static Character FindBody(Character source, int id)
        {
            if (id < 0)
            {
                ApiLog.Once(Owner, "negative body", "bodyCharacterId must not be negative, the spawn was refused");
                return null;
            }
            if (id != 0) return FindCharacter(id);
            GameObject prefab = PrefabOf(source);
            if (prefab != null && prefab.GetComponentInChildren<Appearance>(true) != null) return source;
            return DefaultBody();
        }

        private static Character FindCharacter(int id)
        {
            Character player = CharacterDatabase.Characters[0];
            foreach (Character character in CharacterDatabase.Characters)
            {
                if (character != null && character != player && character.ID == id && CloneOf(id) == null) return character;
            }
            return null;
        }

        private static bool MeshesMatch(Character clone, Transform root)
        {
            for (int i = 0; i < root.childCount; i++)
            {
                if (NpcAppearance.Entry(clone, root.GetChild(i).name) != null) return true;
            }
            return false;
        }

        private static Character DefaultBody()
        {
            Character player = CharacterDatabase.Characters[0];
            Character best = null;
            foreach (Character character in CharacterDatabase.Characters)
            {
                if (character == null || character == player || CloneOf(character.ID) != null) continue;
                if (best != null && character.ID >= best.ID) continue;
                GameObject prefab = PrefabOf(character);
                if (prefab == null || prefab.GetComponent<NPCInfo>() == null || prefab.GetComponentInChildren<Appearance>(true) == null) continue;
                best = character;
            }
            if (best == null) ApiLog.Once(Owner, "no body template", "no non-player character has a prefab with an NPCInfo and an Appearance, so a character with no prefab cannot be spawned");
            return best;
        }

        internal static GameObject PrefabOf(Character character)
        {
            return character.prefabWithAi != null ? character.prefabWithAi : character.prefab;
        }

        internal static int Allocate(string modId, string npcId)
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
            Release(record, destroyNpc, true);
        }

        private static void Release(Record record, bool destroyNpc, bool keepLook)
        {
            if (keepLook) KeepLook(record);
            DropRecord(record, destroyNpc);
        }

        private static void DropRecord(Record record, bool destroyNpc)
        {
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
                    Build(entry.Mod, entry.NpcId, entry.Source, entry.Body, entry.Name, entry.LastName, new Vector3(entry.X, entry.Y, entry.Z), entry.Yaw, entry.KeepDialogue, entry.Look, true);
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

        internal static bool Finite(Vector3 v)
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
            internal int BodyId;
            internal bool Sliders;
            internal bool InFile;
        }
    }
}
