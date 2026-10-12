using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using ObenseuerCore.Ui;
using UnityEngine;

namespace ObenseuerCore
{
    public static class Keybinds
    {
        private const string FileName = "ObenseuerCore.ModKeybinds.cfg";
        private const string PageTitle = "Mod Keybinds";
        private static readonly List<ModKeybind> registered = new List<ModKeybind>();
        private static ConfigFile file;
        private static bool pageAdded;

        public static KeyCode Key(string action)
        {
            SettingsGuard.RequireText(action, "action");
            return SettingsGuard.Read<KeyCode>("Keybinds.Key", () => KeyOf(action), KeyCode.None);
        }

        public static KeyCode Button(string action)
        {
            SettingsGuard.RequireText(action, "action");
            return SettingsGuard.Read<KeyCode>("Keybinds.Button", () => ButtonOf(action), KeyCode.None);
        }

        public static string Label(string action)
        {
            SettingsGuard.RequireText(action, "action");
            return SettingsGuard.Read<string>("Keybinds.Label", () => LabelOf(action), "");
        }

        public static bool Down(string action)
        {
            SettingsGuard.RequireText(action, "action");
            return SettingsGuard.Read<bool>("Keybinds.Down", () => PressedOf(action, true), false);
        }

        public static bool Held(string action)
        {
            SettingsGuard.RequireText(action, "action");
            return SettingsGuard.Read<bool>("Keybinds.Held", () => PressedOf(action, false), false);
        }

        public static ModKeybind Register(string modId, string id, string displayName, KeyCode defaultKey)
        {
            ModIds.Require(modId, "modId");
            SettingsGuard.RequireText(id, "id");
            SettingsGuard.RequireText(displayName, "displayName");
            new ConfigDefinition(modId, id);
            ModKeybind existing = Find(modId, id);
            if (existing != null) return existing;
            ModKeybind bind = SettingsGuard.Read<ModKeybind>("Keybinds.Register", () => Create(modId, id, displayName, defaultKey), null);
            if (bind == null) return null;
            registered.Add(bind);
            if (!pageAdded)
            {
                pageAdded = true;
                ModMenu.AddPage(GameSettings.Owner, PageTitle, BuildPage);
            }
            return bind;
        }

        private static KeyCode KeyOf(string action)
        {
            InputManager input = Ready();
            return input == null ? KeyCode.None : input.GetKeyCode(action, true);
        }

        private static KeyCode ButtonOf(string action)
        {
            InputManager input = Ready();
            return input == null ? KeyCode.None : input.keyBindings.CheckKeyController(action);
        }

        private static string LabelOf(string action)
        {
            InputManager input = Ready();
            if (input == null) return "";
            KeyCode code = input.GetKeyCode(action, false);
            return code == KeyCode.None ? "" : input.GetKeyCodeDisplayText(code);
        }

        private static bool PressedOf(string action, bool down)
        {
            InputManager input = Ready();
            if (input == null) return false;
            return down ? input.GetKeyDown(action) : input.GetKey(action);
        }

        private static InputManager Ready()
        {
            InputManager input = InputManager.instance;
            return input != null && input.keyBindings != null ? input : null;
        }

        private static ModKeybind Find(string modId, string id)
        {
            for (int i = 0; i < registered.Count; i++)
            {
                ConfigDefinition definition = registered[i].Entry.Definition;
                if (definition.Section == modId && definition.Key == id) return registered[i];
            }
            return null;
        }

        private static ModKeybind Create(string modId, string id, string displayName, KeyCode defaultKey)
        {
            ConfigEntry<KeyboardShortcut> entry = Config().Bind(modId, id, new KeyboardShortcut(defaultKey), displayName);
            return new ModKeybind(entry);
        }

        private static ConfigFile Config()
        {
            if (file == null) file = new ConfigFile(Path.Combine(Paths.ConfigPath, FileName), true);
            return file;
        }

        private static void BuildPage(RectTransform content)
        {
            for (int i = 0; i < registered.Count; i++)
            {
                ConfigEntry<KeyboardShortcut> entry = registered[i].Entry;
                ModUi.KeyBinding(content, entry.Description.Description, entry.Value, value => entry.Value = value);
            }
        }
    }

    public sealed class ModKeybind
    {
        private readonly ConfigEntry<KeyboardShortcut> entry;

        internal ModKeybind(ConfigEntry<KeyboardShortcut> entry)
        {
            this.entry = entry;
        }

        internal ConfigEntry<KeyboardShortcut> Entry
        {
            get { return entry; }
        }

        public KeyCode Key
        {
            get { return entry.Value.MainKey; }
        }

        public bool Down()
        {
            return SettingsGuard.Read<bool>("ModKeybind.Down", () => entry.Value.IsDown(), false);
        }

        public bool Held()
        {
            return SettingsGuard.Read<bool>("ModKeybind.Held", () => entry.Value.IsPressed(), false);
        }
    }
}
