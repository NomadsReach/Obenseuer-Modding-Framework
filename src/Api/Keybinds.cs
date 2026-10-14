using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.Mono.Configuration;
using ObenseuerCore.Ui;
using UnityEngine;

namespace ObenseuerCore
{
    public static class Keybinds
    {
        private const string FileName = "ObenseuerCore.ModKeybinds.cfg";
        private const string PageTitle = "Mod Keybinds";
        private static readonly List<ModKeybind> registered = new List<ModKeybind>();
        private static readonly Func<string, KeyCode> keyOf = KeyOf;
        private static readonly Func<string, KeyCode> buttonOf = ButtonOf;
        private static readonly Func<string, string> labelOf = LabelOf;
        private static readonly Func<string, bool> downOf = action => PressedOf(action, true);
        private static readonly Func<string, bool> heldOf = action => PressedOf(action, false);
        private static ConfigFile file;
        private static bool pageAdded;

        public static KeyCode Key(string action)
        {
            SettingsGuard.RequireText(action, "action");
            return SettingsGuard.Read("Keybinds.Key", keyOf, action, KeyCode.None);
        }

        public static KeyCode Button(string action)
        {
            SettingsGuard.RequireText(action, "action");
            return SettingsGuard.Read("Keybinds.Button", buttonOf, action, KeyCode.None);
        }

        public static string Label(string action)
        {
            SettingsGuard.RequireText(action, "action");
            return SettingsGuard.Read("Keybinds.Label", labelOf, action, "");
        }

        public static bool Down(string action)
        {
            SettingsGuard.RequireText(action, "action");
            return SettingsGuard.Read("Keybinds.Down", downOf, action, false);
        }

        public static bool Held(string action)
        {
            SettingsGuard.RequireText(action, "action");
            return SettingsGuard.Read("Keybinds.Held", heldOf, action, false);
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
            WarnSharedKey(bind);
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

        private static void WarnSharedKey(ModKeybind bind)
        {
            ConfigDefinition mine = bind.Entry.Definition;
            KeyboardShortcut key = bind.Entry.Value;
            foreach (ModKeybind other in registered)
            {
                ConfigDefinition theirs = other.Entry.Definition;
                if (theirs.Section == mine.Section || !KeyChord.Overlaps(key, other.Entry.Value)) continue;
                CoreLog.For(GameSettings.Owner).Warn("key " + key + " of " + mine.Section + " (" + mine.Key + ") is also bound by " + theirs.Section + " (" + theirs.Key + " = " + other.Entry.Value + "), both fire");
            }
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
        private static readonly Func<ModKeybind, bool> downOf = bind => KeyChord.Down(bind.entry.Value, KeyChord.KeyDownOf, KeyChord.KeyHeldOf);
        private static readonly Func<ModKeybind, bool> upOf = bind => KeyChord.Up(bind.entry.Value, KeyChord.KeyUpOf, KeyChord.KeyHeldOf);
        private static readonly Func<ModKeybind, bool> heldOf = bind => KeyChord.Held(bind.entry.Value, KeyChord.KeyHeldOf);
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
            return SettingsGuard.Read("ModKeybind.Down", downOf, this, false);
        }

        public bool Up()
        {
            return SettingsGuard.Read("ModKeybind.Up", upOf, this, false);
        }

        public bool Held()
        {
            return SettingsGuard.Read("ModKeybind.Held", heldOf, this, false);
        }
    }

    internal static class KeyChord
    {
        internal static readonly Func<KeyCode, bool> KeyDownOf = Input.GetKeyDown;
        internal static readonly Func<KeyCode, bool> KeyUpOf = Input.GetKeyUp;
        internal static readonly Func<KeyCode, bool> KeyHeldOf = Input.GetKey;

        internal static bool Down(KeyboardShortcut shortcut, Func<KeyCode, bool> edge, Func<KeyCode, bool> held)
        {
            return Match(shortcut, edge, held);
        }

        internal static bool Up(KeyboardShortcut shortcut, Func<KeyCode, bool> edge, Func<KeyCode, bool> held)
        {
            return Match(shortcut, edge, held);
        }

        internal static bool Held(KeyboardShortcut shortcut, Func<KeyCode, bool> held)
        {
            return Match(shortcut, held, held);
        }

        internal static bool Overlaps(KeyboardShortcut a, KeyboardShortcut b)
        {
            if (a.MainKey == KeyCode.None || a.MainKey != b.MainKey) return false;
            HashSet<KeyCode> held = new HashSet<KeyCode>(a.Modifiers);
            return held.IsSubsetOf(b.Modifiers) || held.IsSupersetOf(b.Modifiers);
        }

        private static bool Match(KeyboardShortcut shortcut, Func<KeyCode, bool> mainKey, Func<KeyCode, bool> modifier)
        {
            KeyCode main = shortcut.MainKey;
            if (main == KeyCode.None || !mainKey(main)) return false;
            foreach (KeyCode held in shortcut.Modifiers)
            {
                if (!modifier(held)) return false;
            }
            return true;
        }
    }
}
