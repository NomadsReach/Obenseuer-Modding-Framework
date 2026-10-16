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
            reason = null;
            string what = "apply " + modId;
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
                    SceneLookRigPlan plan = SceneLookRig.Decide(look, SceneLookApplyRig.Names(body), body.name, SceneLookApplyRig.AvatarKind(body));
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
                        if (plan.Path == SceneLookRigPath.Name) SceneLookApplyRig.Bind(look, body, plan, assets, state, out reason);
                        else SceneLookApplyRig.Retarget(look, body, plan, assets, state, out reason);
                        if (reason == null)
                        {
                            Hide(body, state);
                            SceneLookBodyWatch watch = state.Holder.AddComponent<SceneLookBodyWatch>();
                            watch.Body = body;
                            bodies[body] = state;
                            assets.Bodies++;
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
            if (!logged) ApiLog.Once(Owner, what, reason);
            return false;
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
