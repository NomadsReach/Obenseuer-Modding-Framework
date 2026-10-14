using System.Collections.Generic;
using BepInEx.Configuration;
using BepInEx.Unity.Mono.Configuration;
using UnityEngine;

namespace ObenseuerCore
{
    internal static class ModMenuWebKeys
    {
        private static readonly Dictionary<string, KeyCode> keys = new Dictionary<string, KeyCode>
        {
            { "KeyA", KeyCode.A }, { "KeyB", KeyCode.B }, { "KeyC", KeyCode.C }, { "KeyD", KeyCode.D }, { "KeyE", KeyCode.E },
            { "KeyF", KeyCode.F }, { "KeyG", KeyCode.G }, { "KeyH", KeyCode.H }, { "KeyI", KeyCode.I }, { "KeyJ", KeyCode.J },
            { "KeyK", KeyCode.K }, { "KeyL", KeyCode.L }, { "KeyM", KeyCode.M }, { "KeyN", KeyCode.N }, { "KeyO", KeyCode.O },
            { "KeyP", KeyCode.P }, { "KeyQ", KeyCode.Q }, { "KeyR", KeyCode.R }, { "KeyS", KeyCode.S }, { "KeyT", KeyCode.T },
            { "KeyU", KeyCode.U }, { "KeyV", KeyCode.V }, { "KeyW", KeyCode.W }, { "KeyX", KeyCode.X }, { "KeyY", KeyCode.Y },
            { "KeyZ", KeyCode.Z },
            { "Digit0", KeyCode.Alpha0 }, { "Digit1", KeyCode.Alpha1 }, { "Digit2", KeyCode.Alpha2 }, { "Digit3", KeyCode.Alpha3 },
            { "Digit4", KeyCode.Alpha4 }, { "Digit5", KeyCode.Alpha5 }, { "Digit6", KeyCode.Alpha6 }, { "Digit7", KeyCode.Alpha7 },
            { "Digit8", KeyCode.Alpha8 }, { "Digit9", KeyCode.Alpha9 },
            { "Numpad0", KeyCode.Keypad0 }, { "Numpad1", KeyCode.Keypad1 }, { "Numpad2", KeyCode.Keypad2 }, { "Numpad3", KeyCode.Keypad3 },
            { "Numpad4", KeyCode.Keypad4 }, { "Numpad5", KeyCode.Keypad5 }, { "Numpad6", KeyCode.Keypad6 }, { "Numpad7", KeyCode.Keypad7 },
            { "Numpad8", KeyCode.Keypad8 }, { "Numpad9", KeyCode.Keypad9 },
            { "NumpadAdd", KeyCode.KeypadPlus }, { "NumpadSubtract", KeyCode.KeypadMinus }, { "NumpadMultiply", KeyCode.KeypadMultiply },
            { "NumpadDivide", KeyCode.KeypadDivide }, { "NumpadDecimal", KeyCode.KeypadPeriod }, { "NumpadEnter", KeyCode.KeypadEnter },
            { "NumpadEqual", KeyCode.KeypadEquals },
            { "F1", KeyCode.F1 }, { "F2", KeyCode.F2 }, { "F3", KeyCode.F3 }, { "F4", KeyCode.F4 }, { "F5", KeyCode.F5 },
            { "F6", KeyCode.F6 }, { "F7", KeyCode.F7 }, { "F8", KeyCode.F8 }, { "F9", KeyCode.F9 }, { "F10", KeyCode.F10 },
            { "F11", KeyCode.F11 }, { "F12", KeyCode.F12 }, { "F13", KeyCode.F13 }, { "F14", KeyCode.F14 }, { "F15", KeyCode.F15 },
            { "ArrowUp", KeyCode.UpArrow }, { "ArrowDown", KeyCode.DownArrow }, { "ArrowLeft", KeyCode.LeftArrow }, { "ArrowRight", KeyCode.RightArrow },
            { "Enter", KeyCode.Return }, { "Minus", KeyCode.Minus }, { "Equal", KeyCode.Equals },
            { "BracketLeft", KeyCode.LeftBracket }, { "BracketRight", KeyCode.RightBracket }, { "Backquote", KeyCode.BackQuote },
            { "Quote", KeyCode.Quote }, { "Semicolon", KeyCode.Semicolon }, { "Comma", KeyCode.Comma }, { "Period", KeyCode.Period },
            { "Slash", KeyCode.Slash }, { "Backslash", KeyCode.Backslash }, { "Space", KeyCode.Space }, { "Tab", KeyCode.Tab },
            { "Backspace", KeyCode.Backspace }, { "Delete", KeyCode.Delete }, { "Insert", KeyCode.Insert }, { "Home", KeyCode.Home },
            { "End", KeyCode.End }, { "PageUp", KeyCode.PageUp }, { "PageDown", KeyCode.PageDown }, { "CapsLock", KeyCode.CapsLock },
            { "NumLock", KeyCode.Numlock }, { "ScrollLock", KeyCode.ScrollLock }, { "Pause", KeyCode.Pause },
            { "PrintScreen", KeyCode.Print }, { "ContextMenu", KeyCode.Menu }
        };

        private static readonly Dictionary<string, KeyCode> modifiers = new Dictionary<string, KeyCode>
        {
            { "ControlLeft", KeyCode.LeftControl }, { "ControlRight", KeyCode.RightControl },
            { "ShiftLeft", KeyCode.LeftShift }, { "ShiftRight", KeyCode.RightShift },
            { "AltLeft", KeyCode.LeftAlt }, { "AltRight", KeyCode.RightAlt }
        };

        internal static bool TryShortcut(string code, string[] mods, out KeyboardShortcut shortcut)
        {
            shortcut = KeyboardShortcut.Empty;
            KeyCode main;
            if (code == null || !keys.TryGetValue(code, out main)) return false;
            KeyCode[] held = new KeyCode[mods == null ? 0 : mods.Length];
            for (int i = 0; i < held.Length; i++)
            {
                if (mods[i] == null || !modifiers.TryGetValue(mods[i], out held[i])) return false;
            }
            shortcut = new KeyboardShortcut(main, held);
            return true;
        }

        internal static string SelfCheck()
        {
            KeyboardShortcut shortcut;
            if (!TryShortcut("KeyA", null, out shortcut) || !shortcut.Equals(new KeyboardShortcut(KeyCode.A))) return "KeyA";
            if (!TryShortcut("Digit1", null, out shortcut) || !shortcut.Equals(new KeyboardShortcut(KeyCode.Alpha1))) return "Digit1";
            if (!TryShortcut("F5", null, out shortcut) || !shortcut.Equals(new KeyboardShortcut(KeyCode.F5))) return "F5";
            if (!TryShortcut("NumpadEnter", null, out shortcut) || !shortcut.Equals(new KeyboardShortcut(KeyCode.KeypadEnter))) return "NumpadEnter";
            if (!TryShortcut("KeyK", new[] { "ShiftLeft" }, out shortcut) || !shortcut.Equals(new KeyboardShortcut(KeyCode.K, KeyCode.LeftShift))) return "ShiftLeft+KeyK";
            if (TryShortcut("NotAKey", null, out shortcut)) return "unknown code accepted";
            return null;
        }
    }
}
