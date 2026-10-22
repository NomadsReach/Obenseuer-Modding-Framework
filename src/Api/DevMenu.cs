using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using ObenseuerCore.Ui;
using UnityEngine;

namespace ObenseuerCore
{
    public static class DevMenu
    {
        internal const string Owner = "DevMenu";
        private const string KeybindName = "DeveloperMenu";
        private const string CommandName = "omf_dev";
        private const int MaxEntries = 200;
        private static readonly List<DevSection> sections = new List<DevSection>();
        private static readonly List<DevWatch> watches = new List<DevWatch>();
        private static ModKeybind key;

        internal static List<DevSection> Sections
        {
            get { return sections; }
        }

        internal static List<DevWatch> Watches
        {
            get { return watches; }
        }

        internal static ModKeybind Key
        {
            get { return key; }
        }

        public static bool SaveIsDevModified
        {
            get { return DevMenuFlag.Modified; }
        }

        public static bool AddSection(string modId, string title, Action<RectTransform> build)
        {
            if (!ModIds.IsValid(modId)) return Refuse("AddSection", "modId must be a valid mod id");
            if (!DevMenuRules.ValidTitle(title)) return Refuse("AddSection", "title must be 1 to " + DevMenuRules.MaxTitle + " characters and not blank");
            if (build == null) return Refuse("AddSection", "build must not be null");
            int index = sections.FindIndex(s => string.Equals(s.ModId, modId, StringComparison.OrdinalIgnoreCase) && s.Title == title);
            if (index < 0 && sections.Count >= MaxEntries) return Refuse("AddSection", "the section list is full (" + MaxEntries + "), this section was not added");
            DevSection section = new DevSection(modId, title, build);
            if (index >= 0) sections[index] = section;
            else sections.Add(section);
            return true;
        }

        public static bool AddWatch(string modId, string name, Func<string> value)
        {
            if (!ModIds.IsValid(modId)) return Refuse("AddWatch", "modId must be a valid mod id");
            if (!DevMenuRules.ValidTitle(name)) return Refuse("AddWatch", "name must be 1 to " + DevMenuRules.MaxTitle + " characters and not blank");
            if (value == null) return Refuse("AddWatch", "value must not be null");
            int index = watches.FindIndex(w => string.Equals(w.ModId, modId, StringComparison.OrdinalIgnoreCase) && w.Name == name);
            if (index < 0 && watches.Count >= MaxEntries) return Refuse("AddWatch", "the watch list is full (" + MaxEntries + "), this watch was not added");
            DevWatch watch = new DevWatch(modId, name, value);
            if (index >= 0) watches[index] = watch;
            else watches.Add(watch);
            return true;
        }

        internal static void Init(ConfigFile config)
        {
            bool enabled = config.Bind("Debug", "DeveloperMenu", false, "Turns on the Developer menu: F8 (rebindable under Mod Keybinds) and the omf_dev console command open a window with quest, state and mod tools. Off by default; when off nothing is built.").Value;
            if (!enabled) return;
            DevMenuFlag.Init();
            key = Keybinds.Register(ModIds.CoreId, KeybindName, "Developer menu", KeyCode.F8);
            if (!ConsoleCommands.Register(ModIds.CoreId, CommandName, "Opens the Developer menu.", args => DevMenuWindow.Open())) CoreLog.For(Owner).Warn("omf_dev not registered, the console is not available");
            DevMenuHost.Create();
            DevMenuNoClip.Init();
        }

        internal static bool Act(string action, string target, Func<bool> call)
        {
            bool ok;
            try
            {
                ok = call();
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, action, e);
                ok = false;
            }
            DevMenuFlag.Mark(ok);
            CoreLog.For(Owner).Info(action + (string.IsNullOrEmpty(target) ? "" : " " + target) + ": " + (ok ? "done" : "refused"));
            return ok;
        }

        private static bool Refuse(string what, string message)
        {
            ApiLog.Once(Owner, what + " " + message, message);
            return false;
        }
    }

    internal sealed class DevSection
    {
        internal readonly string ModId;
        internal readonly string Title;
        internal readonly Action<RectTransform> Build;

        internal DevSection(string modId, string title, Action<RectTransform> build)
        {
            ModId = modId;
            Title = title;
            Build = build;
        }
    }

    internal sealed class DevWatch
    {
        internal readonly string ModId;
        internal readonly string Name;
        internal readonly Func<string> Value;

        internal DevWatch(string modId, string name, Func<string> value)
        {
            ModId = modId;
            Name = name;
            Value = value;
        }
    }
}
