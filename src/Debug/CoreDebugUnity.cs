using System.Globalization;
using UnityEngine;

namespace ObenseuerCore
{
    internal static class CoreDebugUnity
    {
        internal static string Describe(object value)
        {
            if (value is Vector3)
            {
                Vector3 v = (Vector3)value;
                return string.Format(CultureInfo.InvariantCulture, "({0:0.00}, {1:0.00}, {2:0.00})", v.x, v.y, v.z);
            }
            if (!(value is Object)) return null;
            Object unity = (Object)value;
            if (unity == null) return value.GetType().Name + " (destroyed)";
            if (!Core.OnMain()) return unity.GetType().Name;
            return unity.GetType().Name + " \"" + unity.name + "\"";
        }
    }
}
