using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using BepInEx.Unity.Mono.Configuration;
using UnityEngine;

namespace ObenseuerCore
{
    internal static class KeyChordSelfTest
    {
        internal static string Run()
        {
            try
            {
                KeyboardShortcut plain = new KeyboardShortcut(KeyCode.F6);
                KeyboardShortcut chord = new KeyboardShortcut(KeyCode.K, KeyCode.LeftControl);
                HashSet<KeyCode> none = new HashSet<KeyCode>();
                HashSet<KeyCode> f6 = new HashSet<KeyCode> { KeyCode.F6 };
                HashSet<KeyCode> f6WithWasd = new HashSet<KeyCode> { KeyCode.F6, KeyCode.W, KeyCode.A };
                HashSet<KeyCode> kWithCtrl = new HashSet<KeyCode> { KeyCode.K, KeyCode.LeftControl };
                HashSet<KeyCode> kWithCtrlAndWasd = new HashSet<KeyCode> { KeyCode.K, KeyCode.LeftControl, KeyCode.W, KeyCode.D };
                HashSet<KeyCode> kOnly = new HashSet<KeyCode> { KeyCode.K };
                if (!KeyChord.Down(plain, f6.Contains, f6WithWasd.Contains)) return "down: plain key not matched";
                if (KeyChord.Down(plain, none.Contains, f6WithWasd.Contains)) return "down: a held key matched without a press";
                if (!KeyChord.Down(chord, kWithCtrlAndWasd.Contains, kWithCtrlAndWasd.Contains)) return "down: chord blocked by other held keys";
                if (KeyChord.Down(chord, kOnly.Contains, kOnly.Contains)) return "down: chord matched without its modifier";
                if (KeyChord.Down(chord, kWithCtrl.Contains, none.Contains)) return "down: chord matched with the modifier up";
                if (KeyChord.Down(chord, none.Contains, kWithCtrl.Contains)) return "down: chord matched with the main key not pressed";
                if (KeyChord.Down(new KeyboardShortcut(KeyCode.None), k => true, k => true)) return "down: empty shortcut matched";
                if (!KeyChord.Up(plain, f6.Contains, none.Contains)) return "up: released plain key not matched";
                if (KeyChord.Up(plain, none.Contains, f6.Contains)) return "up: a held key matched as released";
                if (!KeyChord.Up(chord, kWithCtrlAndWasd.Contains, kWithCtrlAndWasd.Contains)) return "up: chord blocked by other held keys";
                if (KeyChord.Up(chord, kWithCtrl.Contains, none.Contains)) return "up: chord matched with the modifier up";
                if (!KeyChord.Held(plain, f6WithWasd.Contains)) return "held: plain key not matched";
                if (KeyChord.Held(plain, none.Contains)) return "held: plain key matched while up";
                if (!KeyChord.Held(chord, kWithCtrlAndWasd.Contains)) return "held: chord blocked by other held keys";
                if (KeyChord.Held(chord, kOnly.Contains)) return "held: chord matched without its modifier";
            }
            catch (Exception e)
            {
                return "exception: " + e.Message;
            }
            return "keychord self-check ok";
        }
    }
}
