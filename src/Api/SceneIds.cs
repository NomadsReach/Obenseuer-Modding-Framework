using System.Globalization;
using System.Text;
using UnityEngine;

namespace ObenseuerCore
{
    internal static class SceneIds
    {
        internal static string Of(Component component)
        {
            if (component == null) return "";
            string own = SaveController.GetObjectGUID(component);
            if (!string.IsNullOrEmpty(own)) return own;
            string guid = WorldObjects.GuidOf(component.gameObject);
            return string.IsNullOrEmpty(guid) ? PathOf(component.transform) : guid;
        }

        internal static T Find<T>(string id) where T : Component
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (T candidate in UnityEngine.Object.FindObjectsByType<T>(FindObjectsSortMode.None))
            {
                if (Of(candidate) == id) return candidate;
            }
            return null;
        }

        private static string PathOf(Transform transform)
        {
            StringBuilder path = new StringBuilder();
            for (Transform node = transform; node != null; node = node.parent)
            {
                path.Insert(0, "/" + node.name + "[" + node.GetSiblingIndex().ToString(CultureInfo.InvariantCulture) + "]");
            }
            return transform.gameObject.scene.name + ":" + path.ToString().Substring(1);
        }
    }
}
