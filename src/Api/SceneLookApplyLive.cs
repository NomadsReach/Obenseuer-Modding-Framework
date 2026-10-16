using System.Collections.Generic;
using UnityEngine;

namespace ObenseuerCore
{
    internal static class SceneLookApplyLive
    {
        private const string Owner = "obenseuer.moddingframework.scenelooks";

        internal static bool Reskin(string modId, SceneLookLook look, GameObject liveNpc, out string reason)
        {
            reason = null;
            if (liveNpc == null)
            {
                reason = "no npc";
                return false;
            }
            List<GameObject> own = Hidden(liveNpc);
            List<int> ownIds = Ids(own);
            List<int> before = Children(liveNpc);
            if (!SceneLookApply.Apply(modId, look, liveNpc, out reason)) return false;
            int holderId = SceneLookLivePlan.Holder(before, Children(liveNpc));
            GameObject holder = holderId == 0 ? null : Find(liveNpc, holderId);
            List<int> kept = SceneLookLivePlan.Watch(ownIds, Objects(liveNpc));
            if (holder == null)
            {
                ApiLog.Once(Owner, "reskin " + modId, "the look was applied but the object it was added under was not found, so the npc's own skinned mesh renderers are not watched after the game touches them");
                return true;
            }
            if (kept.Count == 0) return true;
            List<GameObject> watch = new List<GameObject>(kept.Count);
            foreach (int index in kept) watch.Add(own[index]);
            SceneLookLiveGuard guard = holder.AddComponent<SceneLookLiveGuard>();
            guard.Setup(watch);
            return true;
        }

        private static List<GameObject> Hidden(GameObject npc)
        {
            List<GameObject> hidden = new List<GameObject>();
            SkinnedMeshRenderer[] renderers = npc.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (SkinnedMeshRenderer renderer in renderers)
            {
                if (renderer == null || !renderer.enabled) continue;
                if (hidden.Contains(renderer.gameObject)) continue;
                hidden.Add(renderer.gameObject);
            }
            return hidden;
        }

        private static List<int> Ids(List<GameObject> objects)
        {
            List<int> ids = new List<int>(objects.Count);
            foreach (GameObject item in objects) ids.Add(item.GetInstanceID());
            return ids;
        }

        private static List<int> Children(GameObject npc)
        {
            Transform root = npc.transform;
            List<int> ids = new List<int>(root.childCount);
            for (int i = 0; i < root.childCount; i++) ids.Add(root.GetChild(i).gameObject.GetInstanceID());
            return ids;
        }

        private static List<int> Objects(GameObject npc)
        {
            Transform[] transforms = npc.GetComponentsInChildren<Transform>(true);
            List<int> ids = new List<int>(transforms.Length);
            foreach (Transform transform in transforms) ids.Add(transform.gameObject.GetInstanceID());
            return ids;
        }

        private static GameObject Find(GameObject npc, int id)
        {
            Transform[] transforms = npc.GetComponentsInChildren<Transform>(true);
            foreach (Transform transform in transforms)
            {
                if (transform.gameObject.GetInstanceID() == id) return transform.gameObject;
            }
            return null;
        }
    }

    internal sealed class SceneLookLiveGuard : MonoBehaviour
    {
        private const string Owner = "obenseuer.moddingframework.scenelooks";
        private const float Period = 0.5f;
        private GameObject[] own;
        private float next;

        internal void Setup(List<GameObject> ownObjects)
        {
            own = ownObjects.ToArray();
            next = Time.unscaledTime + Period;
        }

        private void LateUpdate()
        {
            if (Time.unscaledTime < next) return;
            next = Time.unscaledTime + Period;
            int hidden = 0;
            for (int i = 0; i < own.Length; i++)
            {
                if (own[i] == null) continue;
                SkinnedMeshRenderer[] renderers = own[i].GetComponents<SkinnedMeshRenderer>();
                foreach (SkinnedMeshRenderer renderer in renderers)
                {
                    if (renderer == null || !renderer.enabled) continue;
                    renderer.enabled = false;
                    hidden++;
                }
            }
            if (hidden == 0) return;
            ApiLog.Once(Owner, "reskin reapplied", "the game turned the npc's own skinned mesh renderers back on after a re-skin, the framework hid them again");
        }
    }
}
