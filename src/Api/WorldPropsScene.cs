using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ObenseuerCore
{
    internal sealed class WorldPropsPending
    {
        internal string Scene;
        internal int Frame;
    }

    internal sealed class WorldPropsDriver : MonoBehaviour
    {
        private void LateUpdate()
        {
            WorldPropsScene.Tick();
        }
    }

    internal static class WorldPropsScene
    {
        private static readonly List<WorldPropsPending> pending = new List<WorldPropsPending>();
        private static GameObject host;

        internal static void Init()
        {
            Ensure();
            Events.SceneLoaded += OnSceneLoaded;
            Events.SceneLoadedWithMode += OnSceneLoadedWithMode;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        internal static void Ensure()
        {
            if (host != null) return;
            host = new GameObject("ObenseuerCore.WorldProps");
            UnityEngine.Object.DontDestroyOnLoad(host);
            host.AddComponent<WorldPropsDriver>();
        }

        internal static void Queue(string scene)
        {
            if (string.IsNullOrEmpty(scene) || Pending(scene)) return;
            pending.Add(new WorldPropsPending { Scene = scene, Frame = Time.frameCount });
        }

        internal static void Tick()
        {
            if (pending.Count == 0) return;
            for (int i = pending.Count - 1; i >= 0; i--)
            {
                WorldPropsPending item = pending[i];
                if (Time.frameCount <= item.Frame) continue;
                if (SaveGate.Busy) continue;
                pending.RemoveAt(i);
                if (!SceneManager.GetSceneByName(item.Scene).isLoaded) continue;
                try
                {
                    WorldProps.Restore(item.Scene);
                }
                catch (Exception e)
                {
                    ApiLog.Once(WorldProps.Owner, "WorldProps.Restore", e);
                }
            }
        }

        private static void OnSceneLoaded(string scene)
        {
            Events.WhenReady(() => Queue(scene));
        }

        private static void OnSceneLoadedWithMode(string scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single) return;
            WorldProps.MarkNotLive();
            pending.Clear();
        }

        private static void OnSceneUnloaded(Scene scene)
        {
            try
            {
                for (int i = pending.Count - 1; i >= 0; i--)
                {
                    if (pending[i].Scene == scene.name) pending.RemoveAt(i);
                }
                WorldProps.DropScene(scene.name);
            }
            catch (Exception e)
            {
                ApiLog.Once(WorldProps.Owner, "WorldProps scene unloaded", e);
            }
        }

        private static bool Pending(string scene)
        {
            foreach (WorldPropsPending item in pending)
            {
                if (item.Scene == scene) return true;
            }
            return false;
        }
    }
}
