using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ObenseuerCore
{
    internal sealed class WorldPropRefusal
    {
        internal readonly string Where;
        internal readonly string Why;

        internal WorldPropRefusal(string where, string why)
        {
            Where = where;
            Why = why;
        }
    }

    public static class WorldProps
    {
        internal const string Owner = "obenseuer.moddingframework.worldprops";
        internal const string StoreModId = Owner;
        internal const int MaxLive = 1024;
        private static readonly Dictionary<string, GameObject> live = new Dictionary<string, GameObject>();
        private static readonly Dictionary<string, Action<WorldPropUse>> handlers = new Dictionary<string, Action<WorldPropUse>>();
        private static readonly HashSet<string> used = new HashSet<string>();
        private static SavedPropFile saved = new SavedPropFile();
        private static bool ready;

        public static event Action<WorldPropSpawned> Spawned;

        public static bool IsAvailable
        {
            get { return ready && !CorePatcher.IsDisabled(Owner); }
        }

        public static GameObject Place(string modId, string propId, string scene, Vector3 position, Quaternion rotation, Vector3 scale, WorldPropSource source, WorldPropOptions options)
        {
            ModIds.Require(modId, "modId");
            RequireProp(propId);
            if (string.IsNullOrEmpty(scene)) throw new ArgumentException("scene must be a non-empty scene name", "scene");
            if (scene.Length > SavedPropFile.MaxSceneLength) throw new ArgumentException("scene must be at most " + SavedPropFile.MaxSceneLength + " characters", "scene");
            if (source == null) throw new ArgumentNullException("source");
            WorldPropOptions settings = options == null ? new WorldPropOptions() : options.Copy();
            if (settings.InteractKey != null && settings.InteractKey.Length > SavedPropFile.MaxInteractKeyLength) throw new ArgumentException("InteractKey must be at most " + SavedPropFile.MaxInteractKeyLength + " characters", "options");
            if (settings.Data != null && settings.Data.Length > SavedPropFile.MaxDataLength) throw new ArgumentException("Data must be at most " + SavedPropFile.MaxDataLength + " characters", "options");
            if (!MainThread()) return null;
            WorldPropRefusal refusal = Refuse(modId, propId, scene, position, rotation, scale, source, settings);
            if (refusal != null)
            {
                ApiLog.Once(Owner, refusal.Where, refusal.Why);
                return null;
            }
            try
            {
                SavedPropEntry entry = Entry(modId, propId, scene, position, rotation, scale, source, settings);
                SavedPropEntry old = saved.Find(modId, propId);
                if (old != null) Despawn(old);
                saved.Put(entry);
                used.Add(modId);
                Flush();
                if (!SceneManager.GetSceneByName(scene).isLoaded) return null;
                return Spawn(entry);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "WorldProps.Place", e);
                return null;
            }
        }

        internal static WorldPropRefusal Refuse(string modId, string propId, string scene, Vector3 position, Quaternion rotation, Vector3 scale, WorldPropSource source, WorldPropOptions settings)
        {
            if (settings == null) settings = new WorldPropOptions();
            if (!IsAvailable) return new WorldPropRefusal("WorldProps.Place off", "the world props store is not ready, nothing is placed");
            if (SaveGate.Busy) return new WorldPropRefusal("WorldProps.Place busy", "a write is refused while a save loads or saves");
            if (!SavedPropFile.PoseAllowed(position, rotation, scale)) return new WorldPropRefusal("WorldProps.Place pose", "the pose must be finite and scale must be " + SavedPropFile.MinScale + " to " + SavedPropFile.MaxScale + " on each axis");
            if (settings.Layer != 0) return new WorldPropRefusal("WorldProps.Place layer", "only the Default layer is supported in this version");
            if (!settings.Collider && !string.IsNullOrEmpty(settings.InteractKey)) return new WorldPropRefusal("WorldProps.Place interaction", "an interaction needs a collider, so this prop is refused");
            if (!saved.Room(modId, propId)) return new WorldPropRefusal("WorldProps.Place cap " + modId, "one mod keeps at most " + SavedPropFile.MaxEntries + " props, this one is refused");
            string reason = WorldPropsBuild.Check(source);
            if (reason != null) return new WorldPropRefusal("WorldProps.Place source " + source.Kind + " " + source.Key + " " + source.Alt, reason);
            if (SaveData.Folder == null) return new WorldPropRefusal("WorldProps.Place save", "no save is loaded, so the prop is not recorded and nothing is placed");
            return null;
        }

        public static bool Move(string modId, string propId, Vector3 position, Quaternion rotation)
        {
            ModIds.Require(modId, "modId");
            RequireProp(propId);
            if (!MainThread()) return false;
            if (!IsAvailable)
            {
                ApiLog.Once(Owner, "WorldProps.Move off", "the world props store is not ready, nothing is moved");
                return false;
            }
            if (SaveGate.Busy)
            {
                ApiLog.Once(Owner, "WorldProps.Move busy", "a write is refused while a save loads or saves");
                return false;
            }
            if (!SavedPropFile.Finite(position) || !SavedPropFile.Finite(rotation))
            {
                ApiLog.Once(Owner, "WorldProps.Move pose", "position and rotation must be finite");
                return false;
            }
            try
            {
                SavedPropEntry entry = saved.Find(modId, propId);
                if (entry == null)
                {
                    ApiLog.Once(Owner, "WorldProps.Move missing", "no prop of this mod has that id");
                    return false;
                }
                if (entry.Static)
                {
                    ApiLog.Once(Owner, "WorldProps.Move static", "a static prop cannot be moved, place it again instead");
                    return false;
                }
                entry.Position = position;
                entry.Rotation = rotation;
                GameObject go;
                if (live.TryGetValue(Key(modId, propId), out go) && go != null)
                {
                    go.transform.position = position;
                    go.transform.rotation = rotation;
                }
                Flush();
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "WorldProps.Move", e);
                return false;
            }
        }

        public static bool Remove(string modId, string propId)
        {
            ModIds.Require(modId, "modId");
            RequireProp(propId);
            if (!MainThread()) return false;
            if (!IsAvailable)
            {
                ApiLog.Once(Owner, "WorldProps.Remove off", "the world props store is not ready, nothing is removed");
                return false;
            }
            if (SaveGate.Busy)
            {
                ApiLog.Once(Owner, "WorldProps.Remove busy", "a write is refused while a save loads or saves");
                return false;
            }
            try
            {
                SavedPropEntry entry = saved.Find(modId, propId);
                if (entry == null)
                {
                    ApiLog.Once(Owner, "WorldProps.Remove missing", "no prop of this mod has that id");
                    return false;
                }
                Despawn(entry);
                saved.Drop(modId, propId);
                Flush();
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "WorldProps.Remove", e);
                return false;
            }
        }

        public static GameObject Get(string modId, string propId)
        {
            ModIds.Require(modId, "modId");
            RequireProp(propId);
            if (!ready) return null;
            return Live(modId, propId);
        }

        public static List<WorldPropInfo> List(string modId, string scene)
        {
            ModIds.Require(modId, "modId");
            if (string.IsNullOrEmpty(scene)) throw new ArgumentException("scene must be a non-empty scene name", "scene");
            List<WorldPropInfo> list = new List<WorldPropInfo>();
            if (!ready) return list;
            foreach (SavedPropEntry entry in saved.Entries)
            {
                if (entry.Mod == modId && entry.Scene == scene) list.Add(Info(entry));
            }
            return list;
        }

        public static int Clear(string modId)
        {
            ModIds.Require(modId, "modId");
            if (!MainThread()) return 0;
            if (!IsAvailable)
            {
                ApiLog.Once(Owner, "WorldProps.Clear off", "the world props store is not ready, nothing is cleared");
                return 0;
            }
            if (SaveGate.Busy)
            {
                ApiLog.Once(Owner, "WorldProps.Clear busy", "a write is refused while a save loads or saves");
                return 0;
            }
            try
            {
                List<SavedPropEntry> mine = saved.Entries.FindAll(e => e.Mod == modId);
                foreach (SavedPropEntry entry in mine) Despawn(entry);
                int dropped = saved.Drop(modId, null);
                if (dropped > 0) Flush();
                return dropped;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "WorldProps.Clear", e);
                return 0;
            }
        }

        public static bool OnUse(string modId, string key, Action<WorldPropUse> handler)
        {
            ModIds.Require(modId, "modId");
            if (string.IsNullOrEmpty(key) || key.Length > SavedPropFile.MaxInteractKeyLength) throw new ArgumentException("key must be 1 to " + SavedPropFile.MaxInteractKeyLength + " characters", "key");
            if (handler == null) throw new ArgumentNullException("handler");
            if (!MainThread()) return false;
            if (!IsAvailable)
            {
                ApiLog.Once(Owner, "WorldProps.OnUse off", "the world props store is not ready, the handler is not registered");
                return false;
            }
            string id = HandlerKey(modId, key);
            if (handlers.ContainsKey(id))
            {
                ApiLog.Once(Owner, "WorldProps.OnUse " + id, "a handler for this key is already registered, the second call is refused");
                return false;
            }
            handlers[id] = handler;
            used.Add(modId);
            return true;
        }

        internal static void Init()
        {
            try
            {
                SaveData.Register<SavedPropFile>(StoreModId, Snapshot, LoadStore);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "WorldProps store register", e);
                return;
            }
            ready = true;
            WorldPropsScene.Init();
            if (SaveData.Folder != null) Events.WhenReady(() => WorldPropsScene.Queue(SceneManager.GetActiveScene().name));
        }

        internal static void Restore(string scene)
        {
            foreach (SavedPropEntry entry in saved.Restorable(scene))
            {
                try
                {
                    Spawn(entry);
                }
                catch (Exception e)
                {
                    ApiLog.Once(Owner, "WorldProps.Restore " + entry.PropId, e);
                }
            }
        }

        internal static void MarkNotLive()
        {
            live.Clear();
        }

        internal static void DropScene(string scene)
        {
            foreach (SavedPropEntry entry in saved.Entries.FindAll(e => e.Scene == scene))
            {
                try
                {
                    Despawn(entry);
                }
                catch (Exception e)
                {
                    ApiLog.Once(Owner, "WorldProps.DropScene " + entry.PropId, e);
                }
            }
        }

        internal static GameObject Spawn(SavedPropEntry entry)
        {
            if (SaveGate.Busy) return null;
            if (!ModIds.IsInstalled(entry.Mod, used))
            {
                ApiLog.Once(Owner, "WorldProps uninstalled " + entry.Mod, "its props are kept and not spawned while the mod is not installed");
                return null;
            }
            GameObject existing = Live(entry.Mod, entry.PropId);
            if (existing != null) return existing;
            if (LiveCount() >= MaxLive)
            {
                ApiLog.Once(Owner, "WorldProps live cap", "at most " + MaxLive + " props are live at once, this one is not spawned");
                return null;
            }
            WorldPropBuilt built = WorldPropsBuild.Build(entry);
            if (built.Go == null)
            {
                if (built.Reason != null) ApiLog.Once(Owner, "WorldProps spawn " + entry.Kind + " " + entry.PropId, built.Reason);
                return null;
            }
            live[Key(entry.Mod, entry.PropId)] = built.Go;
            Attach(entry, built.Go);
            Events.Raise(Spawned, "WorldProps.Spawned", h => h(new WorldPropSpawned(built.Go, Info(entry))));
            return built.Go;
        }

        internal static void Despawn(SavedPropEntry entry)
        {
            string key = Key(entry.Mod, entry.PropId);
            GameObject go;
            if (!live.TryGetValue(key, out go)) return;
            live.Remove(key);
            if (go == null) return;
            go.SetActive(false);
            UnityEngine.Object.Destroy(go);
        }

        internal static WorldPropInfo Info(SavedPropEntry entry)
        {
            return new WorldPropInfo(entry, Live(entry.Mod, entry.PropId) != null);
        }

        internal static string HandlerKey(string modId, string key)
        {
            return modId + "\n" + key;
        }

        private static void Attach(SavedPropEntry entry, GameObject go)
        {
            if (string.IsNullOrEmpty(entry.InteractKey)) return;
            Action<WorldPropUse> handler;
            if (!handlers.TryGetValue(HandlerKey(entry.Mod, entry.InteractKey), out handler) || handler == null)
            {
                ApiLog.Once(Owner, "WorldProps no handler " + entry.Mod + " " + entry.InteractKey, "the prop shows no use prompt until the mod registers OnUse for this key");
                return;
            }
            try
            {
                WorldPropsBuild.EnsureCollider(go);
                if (!Interactions.Add(go, entry.Mod, entry.InteractKey, Use(entry, handler)))
                {
                    ApiLog.Once(Owner, "WorldProps attach " + entry.PropId, "the use prompt could not be attached to this prop");
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "WorldProps attach " + entry.PropId, e);
            }
        }

        private static Action Use(SavedPropEntry entry, Action<WorldPropUse> handler)
        {
            return () =>
            {
                try
                {
                    handler(new WorldPropUse(entry, Live(entry.Mod, entry.PropId)));
                }
                catch (Exception e)
                {
                    ApiLog.Once(Owner, "WorldProps.OnUse handler " + entry.PropId, e);
                }
            };
        }

        private static GameObject Live(string modId, string propId)
        {
            GameObject go;
            string key = Key(modId, propId);
            if (!live.TryGetValue(key, out go)) return null;
            if (go != null) return go;
            live.Remove(key);
            return null;
        }

        private static int LiveCount()
        {
            int count = 0;
            foreach (KeyValuePair<string, GameObject> item in live)
            {
                if (item.Value != null) count++;
            }
            return count;
        }

        private static SavedPropEntry Entry(string modId, string propId, string scene, Vector3 position, Quaternion rotation, Vector3 scale, WorldPropSource source, WorldPropOptions settings)
        {
            SavedPropEntry entry = new SavedPropEntry();
            entry.Mod = modId;
            entry.PropId = propId;
            entry.Scene = scene;
            entry.Kind = (int)source.Kind;
            entry.Key = source.Key == null ? "" : source.Key;
            entry.Alt = source.Alt;
            entry.Position = position;
            entry.Rotation = rotation;
            entry.Scale = scale;
            entry.Collider = settings.Collider;
            entry.Static = settings.IsStatic;
            entry.Layer = settings.Layer;
            entry.InteractKey = settings.InteractKey == null ? "" : settings.InteractKey;
            entry.Data = settings.Data == null ? "" : settings.Data;
            entry.LightKind = (int)source.LightKind;
            entry.LightColor = source.LightColor;
            entry.LightRange = source.LightRange;
            entry.LightIntensity = source.LightIntensity;
            entry.LightSpotAngle = source.LightSpotAngle;
            return entry;
        }

        private static SavedPropFile Snapshot()
        {
            return saved;
        }

        private static void LoadStore(SavedPropFile file)
        {
            saved = SavedPropFile.Clean(file);
            live.Clear();
        }

        private static void Flush()
        {
            if (SaveData.Folder == null) return;
            try
            {
                SaveData.Flush(StoreModId);
            }
            catch (Exception e)
            {
                CoreLog.For(Owner).Error("world prop data could not be written: " + e.ToString());
            }
        }

        private static void RequireProp(string propId)
        {
            if (string.IsNullOrEmpty(propId) || propId.Length > SavedPropFile.MaxIdLength) throw new ArgumentException("propId must be 1 to " + SavedPropFile.MaxIdLength + " characters", "propId");
        }

        private static bool MainThread()
        {
            if (Core.OnMain()) return true;
            ApiLog.Once(Owner, "WorldProps off main thread", "not on the Unity main thread, the call is refused");
            return false;
        }

        private static string Key(string modId, string propId)
        {
            return modId + "\n" + propId;
        }
    }
}
