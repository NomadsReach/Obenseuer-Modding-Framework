using System;
using System.Reflection;

namespace ObenseuerCore
{
    public static class Localization
    {
        private const string MenuTable = "Menu";
        private const string NoTranslation = "No translation found";
        private const BindingFlags Statics = BindingFlags.Public | BindingFlags.Static;
        private const BindingFlags Instances = BindingFlags.Public | BindingFlags.Instance;
        private static bool resolved;
        private static PropertyInfo selectedLocale;
        private static PropertyInfo localeName;
        private static PropertyInfo stringDatabase;
        private static MethodInfo getLocalizedString;
        private static MethodInfo tableOf;
        private static MethodInfo entryOf;
        private static object fallback;

        public static string Language()
        {
            return SettingsGuard.Read<string>("Localization.Language", LanguageOf, "");
        }

        public static string Localize(string key)
        {
            return Localize(MenuTable, key);
        }

        public static string Localize(string table, string key)
        {
            SettingsGuard.RequireText(table, "table");
            SettingsGuard.RequireText(key, "key");
            return SettingsGuard.Read<string>("Localization.Localize", () => LocalizeOf(table, key), key);
        }

        private static string LanguageOf()
        {
            Resolve();
            if (selectedLocale == null || localeName == null) return "";
            object locale = selectedLocale.GetValue(null, null);
            if (locale == null) return "";
            string name = (string)localeName.GetValue(locale, null);
            return name ?? "";
        }

        private static string LocalizeOf(string table, string key)
        {
            Resolve();
            if (getLocalizedString == null || stringDatabase == null) return key;
            object database = stringDatabase.GetValue(null, null);
            object tableRef = tableOf.Invoke(null, new object[] { table });
            object entryRef = entryOf.Invoke(null, new object[] { key });
            string text = (string)getLocalizedString.Invoke(database, new object[] { tableRef, entryRef, null, fallback, new object[0] });
            return string.IsNullOrEmpty(text) || text.StartsWith(NoTranslation, StringComparison.Ordinal) ? key : text;
        }

        private static void Resolve()
        {
            if (resolved) return;
            resolved = true;
            Type settings = Type.GetType("UnityEngine.Localization.Settings.LocalizationSettings, Unity.Localization");
            if (settings == null) return;
            selectedLocale = settings.GetProperty("SelectedLocale", Statics);
            stringDatabase = settings.GetProperty("StringDatabase", Statics);
            if (selectedLocale == null || stringDatabase == null) return;
            localeName = selectedLocale.PropertyType.GetProperty("LocaleName", Instances);
            foreach (MethodInfo method in stringDatabase.PropertyType.GetMethods(Instances))
            {
                ParameterInfo[] parameters = method.GetParameters();
                if (method.Name != "GetLocalizedString" || parameters.Length != 5 || parameters[0].ParameterType.Name != "TableReference") continue;
                if (parameters[2].ParameterType != selectedLocale.PropertyType || !parameters[3].ParameterType.IsEnum) continue;
                getLocalizedString = method;
                tableOf = parameters[0].ParameterType.GetMethod("op_Implicit", new[] { typeof(string) });
                entryOf = parameters[1].ParameterType.GetMethod("op_Implicit", new[] { typeof(string) });
                fallback = Enum.Parse(parameters[3].ParameterType, "UseProjectSettings");
                return;
            }
        }
    }
}
