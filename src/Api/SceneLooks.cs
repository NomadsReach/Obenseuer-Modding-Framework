using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ObenseuerCore
{
    public static class SceneLooks
    {
        internal const string Owner = "obenseuer.moddingframework.scenelooks";
        private static readonly Dictionary<string, SceneLook> loaded = new Dictionary<string, SceneLook>();
        private static readonly Dictionary<string, List<string>> holders = new Dictionary<string, List<string>>();
        private static SceneLookFiles files;
        private static string[] scenes;

        public static SceneLookInfo[] List(string sceneName)
        {
            return List(sceneName, true);
        }

        public static SceneLookInfo[] List(string sceneName, bool charactersOnly)
        {
            string what = "list " + sceneName + (charactersOnly ? " characters" : " all");
            try
            {
                string reason = Gate();
                List<SceneLookInfo> looks = null;
                if (reason == null)
                {
                    looks = SceneLookIndex.List(Files(), BuildScenes(), sceneName, out reason);
                    if (looks != null && charactersOnly) looks = SceneLookIndex.Characters(looks);
                }
                if (looks != null) return looks.ToArray();
                ApiLog.Once(Owner, what, reason);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, what, e);
            }
            return new SceneLookInfo[0];
        }

        public static SceneLookInfo FindByName(string sceneName, string name)
        {
            string what = "find " + sceneName + " " + name;
            try
            {
                string reason = Gate();
                SceneLookInfo look = null;
                if (reason == null) look = SceneLookIndex.FindByName(Files(), BuildScenes(), sceneName, name, out reason);
                if (look != null) return look;
                ApiLog.Once(Owner, what, reason);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, what, e);
            }
            return null;
        }

        public static SceneLookInfo FindByCharacter(int characterId)
        {
            string what = "findchar " + characterId;
            try
            {
                string reason = Gate();
                SceneLookInfo look = null;
                if (reason == null) look = SceneLookIndex.FindByCharacter(Files(), BuildScenes(), characterId, out reason);
                if (look != null) return look;
                ApiLog.Once(Owner, what, reason);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, what, e);
            }
            return null;
        }

        public static SceneLookInfo FindByCharacter(string sceneName, int characterId)
        {
            string what = "findchar " + sceneName + " " + characterId;
            try
            {
                string reason = Gate();
                SceneLookInfo look = null;
                if (reason == null) look = SceneLookIndex.FindByCharacter(Files(), BuildScenes(), sceneName, characterId, out reason);
                if (look != null) return look;
                ApiLog.Once(Owner, what, reason);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, what, e);
            }
            return null;
        }

        public static SceneLook Load(string modId, SceneLookInfo info)
        {
            string what = "load info " + (info == null ? "(none)" : info.Path);
            SceneLook look = new SceneLook();
            string reason = null;
            try
            {
                reason = Gate();
                if (reason == null && !ModIds.IsValid(modId)) reason = "invalid mod id";
                if (reason == null && info == null) reason = "no look info";
                if (reason == null && (info.Level == null || info.Root == 0)) reason = "the look info was not built by List or Find";
                if (reason == null)
                {
                    look.Scene = info.Scene;
                    look.Path = info.Path;
                    string key = info.Level + "|" + info.Root;
                    SceneLook cached;
                    if (loaded.TryGetValue(key, out cached))
                    {
                        Hold(modId, key);
                        return cached;
                    }
                    SceneLookLook data = SceneLookLoad.Load(Files(), info.Level, info.Root, out reason);
                    if (data != null)
                    {
                        Fill(look, info, data);
                        loaded[key] = look;
                        Hold(modId, key);
                        return look;
                    }
                }
            }
            catch (Exception e)
            {
                reason = e.Message;
                ApiLog.Once(Owner, what, e);
                look.Failed = true;
                look.Reason = reason;
                return look;
            }
            look.Failed = true;
            look.Reason = reason;
            ApiLog.Once(Owner, what, reason);
            return look;
        }

        public static SceneLook Load(string modId, string sceneName, string rendererRoot)
        {
            string what = "load " + sceneName + " " + rendererRoot;
            SceneLook look = new SceneLook();
            look.Scene = sceneName;
            look.Path = rendererRoot;
            string reason = null;
            try
            {
                reason = Gate();
                if (reason == null && !ModIds.IsValid(modId)) reason = "invalid mod id";
                SceneLookInfo info = null;
                if (reason == null) info = SceneLookIndex.Root(Files(), BuildScenes(), sceneName, rendererRoot, out reason);
                if (info != null)
                {
                    string key = info.Level + "|" + info.Root;
                    SceneLook cached;
                    if (loaded.TryGetValue(key, out cached))
                    {
                        Hold(modId, key);
                        return cached;
                    }
                    SceneLookLook data = SceneLookLoad.Load(Files(), info.Level, info.Root, out reason);
                    if (data != null)
                    {
                        Fill(look, info, data);
                        loaded[key] = look;
                        Hold(modId, key);
                        return look;
                    }
                }
            }
            catch (Exception e)
            {
                reason = e.Message;
                ApiLog.Once(Owner, what, e);
                look.Failed = true;
                look.Reason = reason;
                return look;
            }
            look.Failed = true;
            look.Reason = reason;
            ApiLog.Once(Owner, what, reason);
            return look;
        }

        public static bool Apply(string modId, SceneLook look, GameObject body, out string reason)
        {
            reason = null;
            string what = "apply " + modId + (look == null ? string.Empty : " " + look.Path);
            bool logged = false;
            try
            {
                if (!ModIds.IsValid(modId)) reason = "invalid mod id";
                else if (look == null) reason = "no look";
                else if (!look.Ready) reason = "look is not ready: " + look.Reason;
                else if (body == null) reason = "no body";
                else if (SceneLookApply.Apply(modId, look.Look, body, out reason))
                {
                    Hold(modId, look);
                    return true;
                }
            }
            catch (Exception e)
            {
                reason = e.Message;
                ApiLog.Once(Owner, what, e);
                logged = true;
            }
            if (!logged) ApiLog.Once(Owner, what, reason);
            return false;
        }

        public static bool Reskin(string modId, SceneLook look, GameObject liveNpc, out string reason)
        {
            reason = null;
            string what = "reskin " + modId + (look == null ? string.Empty : " " + look.Path);
            bool logged = false;
            try
            {
                if (!ModIds.IsValid(modId)) reason = "invalid mod id";
                else if (look == null) reason = "no look";
                else if (!look.Ready) reason = "look is not ready: " + look.Reason;
                else if (liveNpc == null) reason = "no npc";
                else if (SceneLookApplyLive.Reskin(modId, look.Look, liveNpc, out reason))
                {
                    Hold(modId, look);
                    return true;
                }
            }
            catch (Exception e)
            {
                reason = e.Message;
                ApiLog.Once(Owner, what, e);
                logged = true;
            }
            if (!logged) ApiLog.Once(Owner, what, reason);
            return false;
        }

        public static bool Remove(string modId, GameObject body, out string reason)
        {
            reason = null;
            string what = "remove " + modId;
            bool logged = false;
            try
            {
                if (!ModIds.IsValid(modId)) reason = "invalid mod id";
                else if (body == null) reason = "no body";
                else if (SceneLookApply.Remove(modId, body, out reason)) return true;
            }
            catch (Exception e)
            {
                reason = e.Message;
                ApiLog.Once(Owner, what, e);
                logged = true;
            }
            if (!logged) ApiLog.Once(Owner, what, reason);
            return false;
        }

        public static void Release(string modId, SceneLook look)
        {
            string what = "release " + modId + (look == null ? string.Empty : " " + look.Path);
            try
            {
                if (look == null || !look.Ready)
                {
                    ApiLog.Once(Owner, what, "the look is not loaded");
                    return;
                }
                string key = KeyOf(look);
                if (key == null)
                {
                    ApiLog.Once(Owner, what, "the look is not in the session cache");
                    return;
                }
                if (!Drop(modId, key))
                {
                    ApiLog.Once(Owner, what, "mod " + modId + " does not hold this look");
                    return;
                }
                if (Held(key)) return;
                loaded.Remove(key);
                holders.Remove(key);
                SceneLookApply.Release(look.Look);
                look.Ready = false;
                look.Look = null;
                look.Reason = "released";
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, what, e);
            }
        }

        private static void Hold(string modId, SceneLook look)
        {
            Hold(modId, KeyOf(look));
        }

        private static void Hold(string modId, string key)
        {
            if (key == null) return;
            List<string> list;
            if (!holders.TryGetValue(key, out list))
            {
                list = new List<string>();
                holders[key] = list;
            }
            if (!list.Contains(modId)) list.Add(modId);
        }

        private static bool Drop(string modId, string key)
        {
            List<string> list;
            if (!holders.TryGetValue(key, out list)) return false;
            return list.RemoveAll(m => m == modId) > 0;
        }

        private static bool Held(string key)
        {
            List<string> list;
            if (!holders.TryGetValue(key, out list)) return false;
            return list.Count > 0;
        }

        private static string KeyOf(SceneLook look)
        {
            foreach (KeyValuePair<string, SceneLook> pair in loaded)
            {
                if (pair.Value == look) return pair.Key;
            }
            return null;
        }

        internal static string Gate()
        {
            if (Application.unityVersion == SceneLookFile.UnityVersion) return null;
            return "Unity " + Application.unityVersion + " is not " + SceneLookFile.UnityVersion + ", scene looks are off";
        }

        internal static bool KnownScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return false;
            string[] names = BuildScenes();
            for (int i = 0; i < names.Length; i++)
            {
                if (string.Equals(names[i], sceneName, StringComparison.Ordinal)) return true;
            }
            return false;
        }

        private static SceneLookFiles Files()
        {
            if (files == null) files = new SceneLookFiles(Application.dataPath);
            return files;
        }

        private static string[] BuildScenes()
        {
            if (scenes == null)
            {
                scenes = new string[SceneManager.sceneCountInBuildSettings];
                for (int i = 0; i < scenes.Length; i++) scenes[i] = Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(i));
            }
            return scenes;
        }

        private static void Fill(SceneLook look, SceneLookInfo info, SceneLookLook data)
        {
            HashSet<string> bones = new HashSet<string>();
            int vertices = 0;
            foreach (SceneLookLookRenderer renderer in data.Renderers)
            {
                vertices += (int)renderer.Mesh.VertexCount;
                foreach (SceneLookBone bone in renderer.Bones) bones.Add(bone.Name);
            }
            List<string> names = new List<string>(bones);
            names.Sort(StringComparer.Ordinal);
            look.Name = info.Name;
            look.Ready = true;
            look.Look = data;
            look.MeshCount = data.Renderers.Count;
            look.VertexCount = vertices;
            look.BoneNames = names.ToArray();
        }
    }
}
