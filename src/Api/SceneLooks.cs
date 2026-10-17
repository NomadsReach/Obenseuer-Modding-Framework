using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
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

        public static bool SyncPose(string modId, GameObject body)
        {
            string reason;
            return SyncPose(modId, body, out reason);
        }

        public static bool SyncPose(string modId, GameObject body, out string reason)
        {
            reason = null;
            string what = "sync pose " + modId;
            bool logged = false;
            try
            {
                if (!ModIds.IsValid(modId)) reason = "invalid mod id";
                else if (body == null) reason = "no body";
                else if (SceneLookApply.SyncPose(modId, body, out reason)) return true;
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

        public static bool SetAutoPose(string modId, GameObject body, bool enabled)
        {
            string reason;
            return SetAutoPose(modId, body, enabled, out reason);
        }

        public static bool SetAutoPose(string modId, GameObject body, bool enabled, out string reason)
        {
            reason = null;
            string what = "auto pose " + modId + (enabled ? " on" : " off");
            bool logged = false;
            try
            {
                if (!ModIds.IsValid(modId)) reason = "invalid mod id";
                else if (body == null) reason = "no body";
                else if (SceneLookApply.AutoPose(modId, body, enabled, out reason)) return true;
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

        public static SceneLookRigView RigOf(string modId, GameObject body)
        {
            string reason;
            return RigOf(modId, body, out reason);
        }

        public static SceneLookRigView RigOf(string modId, GameObject body, out string reason)
        {
            reason = null;
            string what = "rig " + modId;
            bool logged = false;
            try
            {
                if (!ModIds.IsValid(modId)) reason = "invalid mod id";
                else if (body == null) reason = "no body";
                else
                {
                    SceneLookApplied state = SceneLookApply.State(body);
                    if (state == null) reason = "the body has no applied look";
                    else if (state.ModId != modId) reason = "the look on this body was applied by mod " + state.ModId;
                    else
                    {
                        Avatar avatar = state.Avatar != null ? state.Avatar : SceneLookApplyRig.HumanAvatar(body);
                        return new SceneLookRigView(state.Holder, avatar, avatar != null && avatar.isHuman);
                    }
                }
            }
            catch (Exception e)
            {
                reason = e.Message;
                ApiLog.Once(Owner, what, e);
                logged = true;
            }
            if (!logged) ApiLog.Once(Owner, what, reason);
            return null;
        }

        public static string Describe(SceneLook look)
        {
            GameObject body = look == null || look.Look == null ? null : SceneLookApply.BodyOf(look.Look);
            return Describe(look, body);
        }

        public static string Describe(SceneLook look, GameObject body)
        {
            try
            {
                if (look == null) return "no look";
                if (!look.Ready || look.Look == null) return "the look is not loaded: " + look.Reason;
                SceneLookLook data = look.Look;
                StringBuilder text = new StringBuilder();
                text.AppendLine("look " + data.FileName + " " + look.Path + " in " + look.Scene);
                text.AppendLine("nodes " + data.Nodes.Count + " | renderers " + data.Renderers.Count + " | bone names " + (look.BoneNames == null ? 0 : look.BoneNames.Length) + " | vertices " + look.VertexCount);
                text.AppendLine("node tree, file TRS:");
                Tree(text, data, "  ");
                text.AppendLine("renderers, bind scale:");
                Dictionary<long, SceneLookNode> byTransform = SceneLookFrame.ByTransform(data);
                foreach (SceneLookLookRenderer renderer in data.Renderers)
                {
                    SceneLookNode node = SceneLookFrame.Node(data, renderer.Source.GameObject.PathId);
                    string chain = node == null ? "0" : SceneLookApplyRig.Number(SceneLookFrame.Scale(SceneLookFrame.RelativeWithoutRoot(data, node, byTransform)));
                    text.AppendLine("  " + renderer.Name + " | bones " + renderer.Bones.Count + " | bind poses " + Poses(renderer) + " | mesh frame scale " + chain + " | bind pose scale " + Bind(renderer));
                }
                if (body == null)
                {
                    text.AppendLine("path and avatar: no body is using this look, apply it first or pass a body");
                }
                else
                {
                    SceneLookRigPlan plan = SceneLookRig.Decide(data, SceneLookApplyRig.Names(body), body.name, SceneLookApplyRig.AvatarKind(body));
                    text.AppendLine("path " + Label(plan.Path) + (plan.Reason == null ? string.Empty : ": " + plan.Reason));
                    text.AppendLine("human roles " + plan.RequiredCovered + " of " + plan.RequiredBones + " | mapped bones " + plan.Bones.Count);
                    SceneLookApplied state = SceneLookApply.State(body);
                    Avatar avatar = state != null && state.Avatar != null ? state.Avatar : SceneLookApplyRig.HumanAvatar(body);
                    if (avatar == null) text.AppendLine("avatar none");
                    else text.AppendLine("avatar valid " + avatar.isValid + " | human " + avatar.isHuman + " | held by the look " + (state != null && state.Avatar != null));
                }
                return text.ToString();
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "describe", e);
                return "describe failed: " + e.Message;
            }
        }

        private static void Tree(StringBuilder text, SceneLookLook look, string indent)
        {
            Dictionary<long, SceneLookNode> byPath = new Dictionary<long, SceneLookNode>(look.Nodes.Count);
            foreach (SceneLookNode node in look.Nodes) byPath[node.Transform] = node;
            SceneLookNode root = null;
            foreach (SceneLookNode node in look.Nodes)
            {
                if (node.GameObject == look.Root) root = node;
            }
            if (root == null) return;
            Tree(text, root, byPath, indent, 0);
        }

        private static void Tree(StringBuilder text, SceneLookNode node, Dictionary<long, SceneLookNode> byPath, string indent, int depth)
        {
            if (node == null || depth > 64) return;
            text.AppendLine(indent + node.Name + " | pos (" + Number(node.Position) + ") | rot (" + Number(node.Rotation) + ") | scale (" + Number(node.Scale) + ")");
            if (node.Children == null) return;
            foreach (SceneLookRef child in node.Children)
            {
                SceneLookNode found;
                if (child.FileId != 0 || !byPath.TryGetValue(child.PathId, out found)) continue;
                Tree(text, found, byPath, indent + "  ", depth + 1);
            }
        }

        private static string Number(float[] values)
        {
            StringBuilder text = new StringBuilder();
            for (int i = 0; i < values.Length; i++)
            {
                if (i > 0) text.Append(", ");
                text.Append(values[i].ToString("0.###", CultureInfo.InvariantCulture));
            }
            return text.ToString();
        }

        private static int Poses(SceneLookLookRenderer renderer)
        {
            if (renderer.Mesh == null || renderer.Mesh.BindPoses == null) return 0;
            return renderer.Mesh.BindPoses.Length;
        }

        private static string Bind(SceneLookLookRenderer renderer)
        {
            if (renderer.Mesh == null || renderer.Mesh.BindPoses == null || renderer.Mesh.BindPoses.Length == 0) return "none";
            return SceneLookApplyRig.Number(SceneLookApplyRig.BindScale(renderer.Mesh.BindPoses[0]));
        }

        private static string Label(SceneLookRigPath path)
        {
            if (path == SceneLookRigPath.Name) return "name";
            if (path == SceneLookRigPath.Retarget) return "retarget";
            return "refuse";
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
