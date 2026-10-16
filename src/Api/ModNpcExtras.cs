using System;
using System.Collections.Generic;
using NPC;
using UnityEngine.AI;
using PixelCrushers.DialogueSystem;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ObenseuerCore
{
    internal static class ModNpcExtras
    {
        private const int MaxPerMod = 64;
        private const int MaxTotal = 256;
        private const int MaxStripPasses = 8;
        private static readonly List<Record> records = new List<Record>();

        internal static void Init()
        {
            Events.SceneLoadedWithMode += OnSceneLoadedWithMode;
        }

        internal static void RequireExtra(string extraId, Vector3 position, float yaw)
        {
            if (string.IsNullOrEmpty(extraId) || extraId.Length > ModNpcs.MaxIdLength) throw new ArgumentException("extraId must be 1 to " + ModNpcs.MaxIdLength + " characters", "extraId");
            if (!ModNpcs.Finite(position) || float.IsNaN(yaw) || float.IsInfinity(yaw)) throw new ArgumentException("position and yaw must be finite", "position");
        }

        internal static GameObject Spawn(string modId, string extraId, int sourceCharacterId, int bodyCharacterId, Vector3 position, float yaw)
        {
            if (!ModNpcs.IsAvailable || SaveGate.Busy || NPCManager.instance == null) return null;
            try
            {
                Prune();
                if (Find(modId, extraId) != null || Count(modId) >= MaxPerMod || Total() >= MaxTotal) return null;
                return Build(modId, extraId, sourceCharacterId, bodyCharacterId, position, yaw);
            }
            catch (Exception e)
            {
                ApiLog.Once(ModNpcs.Owner, "ModNpcs.SpawnExtra", e);
                return null;
            }
        }

        internal static bool Despawn(string modId, string extraId)
        {
            if (string.IsNullOrEmpty(extraId)) return false;
            try
            {
                Prune();
                Record record = Find(modId, extraId);
                if (record == null) return false;
                Drop(record);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(ModNpcs.Owner, "ModNpcs.DespawnExtra", e);
                return false;
            }
        }

        internal static int Clear(string modId)
        {
            try
            {
                Prune();
                List<Record> mine = records.FindAll(r => r.ModId == modId);
                foreach (Record record in mine) Drop(record);
                return mine.Count;
            }
            catch (Exception e)
            {
                ApiLog.Once(ModNpcs.Owner, "ModNpcs.ClearExtras", e);
                return 0;
            }
        }

        internal static GameObject ObjectOf(string modId, string extraId)
        {
            if (string.IsNullOrEmpty(extraId)) return null;
            Record record = Find(modId, extraId);
            return record == null ? null : record.Go;
        }

        private static GameObject Build(string modId, string extraId, int sourceId, int bodyId, Vector3 position, float yaw)
        {
            if (CharacterDatabase.Characters.Count == 0) return null;
            Character source = ModNpcs.FindSource(sourceId);
            Character body = source == null ? null : ModNpcs.FindBody(source, bodyId);
            GameObject prefab = body == null ? null : ModNpcs.PrefabOf(body);
            if (prefab == null) return null;
            Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);
            RaycastHit hit;
            if (!ModNpcs.Ground(position, out hit)) return null;
            Character clone = UnityEngine.Object.Instantiate(source);
            clone.name = modId + "/extra/" + extraId;
            clone.ID = ModNpcs.Allocate(modId, "extra/" + extraId);
            clone.resident = null;
            CharacterDatabase.Characters.Add(clone);
            GameObject holder = new GameObject("Framework extra holder");
            holder.SetActive(false);
            GameObject go = null;
            bool placed = false;
            try
            {
                go = UnityEngine.Object.Instantiate(prefab, hit.point, rotation, holder.transform);
                ModNpcs.Repoint(go, source, body, clone);
                if (!Strip(go)) return null;
                Quiet(go);
                go.name = clone.name;
                go.transform.SetParent(null, true);
                records.Add(new Record { ModId = modId, ExtraId = extraId, Clone = clone, Go = go, Rig = go.GetComponentInChildren<Animator>(true) });
                placed = true;
                return go;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(holder);
                if (!placed)
                {
                    CharacterDatabase.Characters.Remove(clone);
                    UnityEngine.Object.Destroy(clone);
                }
            }
        }

        private static void Quiet(GameObject go)
        {
            foreach (Animator animator in go.GetComponentsInChildren<Animator>(true))
            {
                animator.fireEvents = false;
                animator.applyRootMotion = false;
            }
            foreach (NavMeshAgent agent in go.GetComponentsInChildren<NavMeshAgent>(true)) agent.enabled = false;
            foreach (Rigidbody body in go.GetComponentsInChildren<Rigidbody>(true)) body.isKinematic = true;
        }

        private static bool Strip(GameObject go)
        {
            for (int pass = 0; pass < MaxStripPasses; pass++)
            {
                List<MonoBehaviour> pending = Strippable(go);
                int removed = 0;
                for (int i = pending.Count - 1; i >= 0; i--)
                {
                    if (TryRemove(pending[i])) removed++;
                }
                if (removed == 0) break;
            }
            bool clean = true;
            foreach (MonoBehaviour part in Strippable(go))
            {
                string type = part.GetType().Name;
                ApiLog.Once(ModNpcs.Owner, "extra component " + type, "the component " + type + " could not be removed from an extra");
                if (part is NPCInfo || part is NPCController) clean = false;
            }
            if (!clean) return false;
            foreach (MonoBehaviour part in go.GetComponentsInChildren<MonoBehaviour>(true)) SaveController.SetObjectGUID(part, "");
            return true;
        }

        private static List<MonoBehaviour> Strippable(GameObject go)
        {
            List<MonoBehaviour> parts = new List<MonoBehaviour>();
            foreach (MonoBehaviour part in go.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (part != null && !(part is Appearance) && !(part is CharacterAppearance)) parts.Add(part);
            }
            return parts;
        }

        private static bool TryRemove(MonoBehaviour part)
        {
            try
            {
                UnityEngine.Object.DestroyImmediate(part);
            }
            catch (Exception)
            {
                return false;
            }
            return part == null;
        }

        private static void Drop(Record record)
        {
            records.Remove(record);
            NpcAnimation.ReleaseOn(record.Rig);
            if (record.Go != null)
            {
                record.Go.SetActive(false);
                UnityEngine.Object.Destroy(record.Go);
            }
            if (record.Clone != null)
            {
                CharacterDatabase.Characters.Remove(record.Clone);
                UnityEngine.Object.Destroy(record.Clone);
            }
        }

        private static void Prune()
        {
            List<Record> gone = records.FindAll(r => r.Go == null);
            foreach (Record record in gone) Drop(record);
        }

        private static int Count(string modId)
        {
            int count = 0;
            foreach (Record record in records)
            {
                if (record.ModId == modId && record.Go != null) count++;
            }
            return count;
        }

        private static int Total()
        {
            int count = 0;
            foreach (Record record in records)
            {
                if (record.Go != null) count++;
            }
            return count;
        }

        private static Record Find(string modId, string extraId)
        {
            return records.Find(r => r.ModId == modId && r.ExtraId == extraId && r.Go != null);
        }

        private static void OnSceneLoadedWithMode(string scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single) return;
            List<Record> all = new List<Record>(records);
            foreach (Record record in all) Drop(record);
        }

        private sealed class Record
        {
            internal string ModId;
            internal string ExtraId;
            internal Character Clone;
            internal GameObject Go;
            internal Animator Rig;
        }
    }
}
