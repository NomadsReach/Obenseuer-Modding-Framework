using System;
using System.Collections.Generic;

namespace ObenseuerCore
{
    internal static class FurnitureCategories
    {
        internal const string Owner = "obenseuer.moddingframework.furniture.categories";
        private const int FirstCustom = 1000;
        private static readonly List<string> names = new List<string>();

        internal static bool Installed;

        internal static bool Available
        {
            get { return Installed && !CorePatcher.IsDisabled(Owner); }
        }

        internal static void Install()
        {
            bool ok;
            try
            {
                ok = CorePatcher.PatchCore(typeof(BuildingSystemSlot), "GetCategoryName", new[] { typeof(Furniture.Category) }, CorePatcher.Handler(typeof(FurnitureCategories), "GetCategoryNamePrefix"), null, null, true, Owner);
            }
            catch (Exception e)
            {
                ok = false;
                ApiLog.Once(Owner, "category install", e);
            }
            Installed = ok;
            if (!ok) CoreLog.For(Owner).Warn("custom furniture categories not installed, mod furniture keeps its template category");
        }

        internal static bool IsReserved(string name)
        {
            return name != null && (string.Equals(name, "All", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "None", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "Surfaces", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "Items", StringComparison.OrdinalIgnoreCase));
        }

        internal static Furniture.Category CategoryFor(string name, Furniture.Category template)
        {
            if (string.IsNullOrEmpty(name) || !Available) return template;
            return Value(name);
        }

        internal static string DisplayName(Furniture.Category category)
        {
            int index = (int)category - FirstCustom;
            if (index >= 0 && index < names.Count) return names[index];
            return category.ToString();
        }

        internal static Furniture.Category Value(string name)
        {
            foreach (Furniture.Category value in Enum.GetValues(typeof(Furniture.Category)))
            {
                if (string.Equals(BuildingSystemSlot.GetCategoryName(value), name, StringComparison.OrdinalIgnoreCase)) return value;
            }
            int index = IndexOf(name);
            if (index < 0)
            {
                names.Add(name);
                index = names.Count - 1;
            }
            return (Furniture.Category)(FirstCustom + index);
        }

        private static int IndexOf(string name)
        {
            for (int i = 0; i < names.Count; i++)
            {
                if (string.Equals(names[i], name, StringComparison.OrdinalIgnoreCase)) return i;
            }
            return -1;
        }

        private static bool GetCategoryNamePrefix(Furniture.Category category, ref string __result)
        {
            int index = (int)category - FirstCustom;
            if (index < 0 || index >= names.Count) return true;
            __result = names[index];
            return false;
        }
    }
}
