using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ObenseuerCore
{
    internal static class SceneIds
    {
        private static readonly Dictionary<Type, FieldInfo> guidFields = new Dictionary<Type, FieldInfo>();

        internal static string Of(Component component)
        {
            return IdOf(component, true);
        }

        internal static T Find<T>(string id) where T : Component
        {
            if (string.IsNullOrEmpty(id)) return null;
            bool path = id.IndexOf(':') >= 0;
            Component known = SaveController.GetObjectByGUID(id) as Component;
            if (known != null && known.gameObject.activeInHierarchy && known is T && InActiveScene(known) && IdOf(known, path) == id) return (T)known;
            foreach (T candidate in Active<T>())
            {
                if (IdOf(candidate, path) == id) return candidate;
            }
            return null;
        }

        internal static List<T> Active<T>() where T : Component
        {
            return Active<T>(FindObjectsInactive.Exclude);
        }

        internal static List<T> Active<T>(FindObjectsInactive inactive) where T : Component
        {
            List<T> list = new List<T>();
            foreach (T candidate in UnityEngine.Object.FindObjectsByType<T>(inactive, FindObjectsSortMode.None))
            {
                if (InActiveScene(candidate)) list.Add(candidate);
            }
            return list;
        }

        private static bool InActiveScene(Component component)
        {
            return component.gameObject.scene == SceneManager.GetActiveScene();
        }

        private static string IdOf(Component component, bool allowPath)
        {
            if (component == null) return "";
            string own = OwnGuid(component);
            if (!string.IsNullOrEmpty(own)) return own;
            string guid = WorldObjects.GuidOf(component.gameObject);
            if (!string.IsNullOrEmpty(guid)) return guid;
            return allowPath ? PathOf(component.transform) : "";
        }

        private static string OwnGuid(Component component)
        {
            Type type = component.GetType();
            FieldInfo field;
            if (!guidFields.TryGetValue(type, out field))
            {
                field = type.GetField("GUID");
                guidFields[type] = field;
            }
            return field == null ? null : (string)field.GetValue(component);
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
