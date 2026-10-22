using System;
using System.Collections.Generic;
using UnityEngine;

namespace ObenseuerCore
{
    internal sealed class SceneLookAssets
    {
        public SceneLookFiles Files;
        public SceneLookLook Look;
        public int Bodies;
        public readonly Dictionary<string, Texture2D> Textures = new Dictionary<string, Texture2D>();
        public readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();
    }

    internal sealed class SceneLookHidden
    {
        public SkinnedMeshRenderer Renderer;
        public bool Enabled;
    }

    internal sealed class SceneLookApplied
    {
        public string ModId;
        public GameObject Body;
        public SceneLookLook Look;
        public GameObject Holder;
        public Avatar Avatar;
        public SceneLookPoseDriver Driver;
        public readonly List<GameObject> Objects = new List<GameObject>();
        public readonly List<Mesh> Meshes = new List<Mesh>();
        public readonly List<SceneLookHidden> Hidden = new List<SceneLookHidden>();
    }

    internal static class SceneLookApply
    {
        private const string Owner = "obenseuer.moddingframework.scenelooks";
        private const string VersionWhat = "unity version";
        private static readonly Dictionary<SceneLookLook, SceneLookAssets> built = new Dictionary<SceneLookLook, SceneLookAssets>();
        private static readonly Dictionary<GameObject, SceneLookApplied> bodies = new Dictionary<GameObject, SceneLookApplied>();

        internal static bool Apply(string modId, SceneLookLook look, GameObject body, out string reason)
        {
            return Apply(modId, look, body, false, out reason);
        }

        internal static bool ApplySkeleton(string modId, SceneLookLook look, GameObject body, out string reason)
        {
            return Apply(modId, look, body, true, out reason);
        }

        private static bool Apply(string modId, SceneLookLook look, GameObject body, bool skeleton, out string reason)
        {
            reason = null;
            string what = "apply " + modId;
            SceneLookRigPath planPath = SceneLookRigPath.Refuse;
            SceneLookApplied state = null;
            SceneLookAssets assetsLeft = null;
            bool logged = false;
            try
            {
                if (Application.unityVersion != SceneLookFile.UnityVersion)
                {
                    what = VersionWhat;
                    reason = "unity version not verified: " + Application.unityVersion + " is not " + SceneLookFile.UnityVersion;
                }
                else if (look == null) reason = "no look";
                else if (body == null) reason = "no body";
                else if (look.Files == null) reason = "look has no open file session";
                else if (look.Nodes == null || look.Renderers == null || look.Renderers.Count == 0) reason = "no skinned renderers under the root";
                else
                {
                    Prune();
                    reason = Occupied(body);
                }
                if (reason == null)
                {
                    SceneLookRigPlan plan = skeleton ? SceneLookApplyRig.SkeletonPlan(look, body) : SceneLookRig.Decide(look, SceneLookApplyRig.Names(body), body.name, SceneLookApplyRig.AvatarKind(body));
                    planPath = plan.Path;
                    if (plan.Path == SceneLookRigPath.Refuse)
                    {
                        reason = plan.Reason;
                    }
                    else
                    {
                        SceneLookAssets assets = Assets(look);
                        state = new SceneLookApplied();
                        state.ModId = modId;
                        state.Body = body;
                        state.Look = look;
                        if (plan.Path == SceneLookRigPath.Retarget) SceneLookApplyRig.Retarget(look, body, plan, assets, state, out reason);
                        else SceneLookApplyRig.Bind(look, body, plan, assets, state, out reason);
                        if (reason == null)
                        {
                            Hide(body, state);
                            SceneLookBodyWatch watch = state.Holder.AddComponent<SceneLookBodyWatch>();
                            watch.Body = body;
                            bodies[body] = state;
                            assets.Bodies++;
                            Note(modId, look, body, plan);
                            return true;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                reason = e.Message;
                ApiLog.Once(Owner, what, e);
                logged = true;
            }
            if (state != null) Undo(state);
            if (look != null && built.TryGetValue(look, out assetsLeft))
            {
                if (assetsLeft.Bodies <= 0) Forget(look, assetsLeft);
            }
            if (reason == null) reason = "the look could not be applied";
            string note = look == null ? string.Empty : SceneLookApplyRig.ScaleNote(look);
            if (note.Length > 0) reason += " | " + note;
            if (!logged) ApiLog.Once(Owner, what, reason + Path(planPath));
            return false;
        }

        internal static SceneLookApplied State(GameObject body)
        {
            Prune();
            if (body == null) return null;
            SceneLookApplied state;
            if (!bodies.TryGetValue(body, out state)) return null;
            return state;
        }

        internal static GameObject BodyOf(SceneLookLook look)
        {
            Prune();
            foreach (KeyValuePair<GameObject, SceneLookApplied> pair in bodies)
            {
                if (pair.Value.Look == look) return pair.Key;
            }
            return null;
        }

        internal static bool SyncPose(string modId, GameObject body, out string reason)
        {
            reason = null;
            SceneLookPoseDriver driver;
            if (!Driver(modId, body, out driver, out reason)) return false;
            driver.Sync();
            return true;
        }

        internal static bool AutoPose(string modId, GameObject body, bool enabled, out string reason)
        {
            reason = null;
            SceneLookPoseDriver driver;
            if (!Driver(modId, body, out driver, out reason)) return false;
            driver.Auto = enabled;
            return true;
        }

        private static bool Driver(string modId, GameObject body, out SceneLookPoseDriver driver, out string reason)
        {
            driver = null;
            reason = null;
            SceneLookApplied state = State(body);
            if (state == null)
            {
                reason = "the body has no applied look";
                return false;
            }
            if (state.ModId != modId)
            {
                reason = "the look on this body was applied by mod " + state.ModId;
                return false;
            }
            if (state.Driver == null)
            {
                reason = "the look on this body has no pose driver";
                return false;
            }
            driver = state.Driver;
            return true;
        }

        private static string Occupied(GameObject body)
        {
            foreach (KeyValuePair<GameObject, SceneLookApplied> pair in bodies)
            {
                if (pair.Key == body) return "the body already has an applied look";
                if (pair.Key == null) continue;
                if (body.transform.IsChildOf(pair.Key.transform) || pair.Key.transform.IsChildOf(body.transform)) return "the body is above or below a body that already has an applied look";
            }
            return null;
        }

        internal static bool Remove(string modId, GameObject body, out string reason)
        {
            reason = null;
            try
            {
                Prune();
                if (body == null)
                {
                    reason = "no body";
                    return false;
                }
                SceneLookApplied state;
                if (!bodies.TryGetValue(body, out state))
                {
                    reason = "the body has no applied look";
                    return false;
                }
                if (state.ModId != modId)
                {
                    reason = "the look on this body was applied by mod " + state.ModId;
                    return false;
                }
                bodies.Remove(body);
                Finish(state);
                return true;
            }
            catch (Exception e)
            {
                reason = e.Message;
                ApiLog.Once(Owner, "remove " + modId, e);
                return false;
            }
        }

        internal static void Release(SceneLookLook look)
        {
            try
            {
                Prune();
                if (look == null) return;
                SceneLookAssets assets;
                if (!built.TryGetValue(look, out assets)) return;
                if (assets.Bodies > 0) return;
                Forget(look, assets);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "release", e);
            }
        }

        private static void Prune()
        {
            List<GameObject> dead = null;
            foreach (KeyValuePair<GameObject, SceneLookApplied> pair in bodies)
            {
                if (pair.Key != null) continue;
                if (dead == null) dead = new List<GameObject>();
                dead.Add(pair.Key);
            }
            if (dead == null) return;
            foreach (GameObject body in dead)
            {
                SceneLookApplied state = bodies[body];
                bodies.Remove(body);
                Finish(state);
            }
        }

        internal static void BodyGone(GameObject body)
        {
            try
            {
                if (ReferenceEquals(body, null)) return;
                SceneLookApplied state;
                if (!bodies.TryGetValue(body, out state)) return;
                bodies.Remove(body);
                Finish(state);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "body gone", e);
            }
        }

        private static void Finish(SceneLookApplied state)
        {
            Undo(state);
            SceneLookAssets assets;
            if (state.Look != null && built.TryGetValue(state.Look, out assets))
            {
                assets.Bodies--;
                if (assets.Bodies <= 0) Forget(state.Look, assets);
            }
        }

        private static void Forget(SceneLookLook look, SceneLookAssets assets)
        {
            built.Remove(look);
            foreach (Texture2D texture in assets.Textures.Values)
            {
                if (texture != null) UnityEngine.Object.Destroy(texture);
            }
            foreach (Material material in assets.Materials.Values)
            {
                if (material != null) UnityEngine.Object.Destroy(material);
            }
            assets.Textures.Clear();
            assets.Materials.Clear();
            assets.Bodies = 0;
        }

        private static SceneLookAssets Assets(SceneLookLook look)
        {
            SceneLookAssets assets;
            if (built.TryGetValue(look, out assets)) return assets;
            assets = new SceneLookAssets();
            assets.Files = look.Files;
            assets.Look = look;
            built[look] = assets;
            return assets;
        }

        private static void Undo(SceneLookApplied state)
        {
            if (state == null) return;
            foreach (GameObject created in state.Objects)
            {
                if (created != null) UnityEngine.Object.Destroy(created);
            }
            state.Objects.Clear();
            foreach (Mesh mesh in state.Meshes)
            {
                if (mesh != null) UnityEngine.Object.Destroy(mesh);
            }
            state.Meshes.Clear();
            foreach (SceneLookHidden hidden in state.Hidden)
            {
                if (hidden.Renderer != null) hidden.Renderer.enabled = hidden.Enabled;
            }
            state.Hidden.Clear();
            if (state.Avatar != null) UnityEngine.Object.Destroy(state.Avatar);
            state.Avatar = null;
            state.Driver = null;
            state.Holder = null;
        }

        private static string Path(SceneLookRigPath path)
        {
            if (path == SceneLookRigPath.Name) return " | path name";
            if (path == SceneLookRigPath.Retarget) return " | path retarget";
            if (path == SceneLookRigPath.Skeleton) return " | path skeleton";
            return " | path refuse";
        }

        private static void Note(string modId, SceneLookLook look, GameObject body, SceneLookRigPlan plan)
        {
            try
            {
                string text = "applied " + look.FileName + " to " + body.name + Path(plan.Path);
                text += " | bones " + plan.Bones.Count + " of " + plan.LookBones;
                if (plan.Path == SceneLookRigPath.Skeleton) text += " (by name " + plan.NameMapped + ", by role " + plan.RoleMapped + ")";
                text += SceneLookApplyRig.ScaleNote(look);
                text += SceneLookApplyRig.AxisNote(look);
                CoreLog.For(modId).Info(text);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "apply note", e);
            }
        }

        private static void Hide(GameObject body, SceneLookApplied state)
        {
            SkinnedMeshRenderer[] renderers = body.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (SkinnedMeshRenderer renderer in renderers)
            {
                if (renderer == null) continue;
                if (state.Objects.Contains(renderer.gameObject)) continue;
                SceneLookHidden hidden = new SceneLookHidden();
                hidden.Renderer = renderer;
                hidden.Enabled = renderer.enabled;
                state.Hidden.Add(hidden);
                renderer.enabled = false;
            }
        }
    }

    internal sealed class SceneLookBodyWatch : MonoBehaviour
    {
        internal GameObject Body;

        private void OnDestroy()
        {
            SceneLookApply.BodyGone(Body);
        }
    }
}
