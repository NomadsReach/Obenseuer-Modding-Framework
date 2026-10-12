using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace ObenseuerCore
{
    public static class PlayerLook
    {
        internal const string Owner = "obenseuer.moddingframework.playerlook";
        private static readonly FieldInfo bodyModels = typeof(Appearance).GetField("bodyModels", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        private static readonly HashSet<Character> costumes = new HashSet<Character>();
        private static bool missingLogged;

        public static event Action<Look> Changed;

        public static bool IsAvailable
        {
            get
            {
                return Ready(Player());
            }
        }

        public static Look Capture()
        {
            try
            {
                Appearance ap = Player();
                return Ready(ap) ? Capture(ap) : null;
            }
            catch (Exception e)
            {
                Fail("Capture", e);
                return null;
            }
        }

        public static Look Capture(Appearance appearance)
        {
            if (appearance == null) throw new ArgumentNullException("appearance");
            try
            {
                Look look = new Look();
                look.set = true;
                look.male = appearance.male;
                look.advanced = appearance.advanced;
                foreach (LookDial dial in PlayerLookTable.All) dial.SetLook(look, dial.GetLive(appearance));
                foreach (LookSlot slot in PlayerLookTable.Slots) slot.Set(look, PlayerLookTable.NameOf(slot.Selected(appearance)));
                return look;
            }
            catch (Exception e)
            {
                Fail("Capture", e);
                return null;
            }
        }

        public static bool Apply(Look look)
        {
            if (look == null) throw new ArgumentNullException("look");
            try
            {
                Appearance ap = Player();
                if (!Ready(ap)) return false;
                Put(look, ap);
                Notify(ap);
                return true;
            }
            catch (Exception e)
            {
                Fail("Apply", e);
                return false;
            }
        }

        public static bool Apply(Look look, Appearance appearance)
        {
            if (look == null) throw new ArgumentNullException("look");
            if (appearance == null) throw new ArgumentNullException("appearance");
            try
            {
                if (!Ready(appearance)) return false;
                Put(look, appearance);
                return true;
            }
            catch (Exception e)
            {
                Fail("Apply", e);
                return false;
            }
        }

        public static bool Live(Look look)
        {
            if (look == null) throw new ArgumentNullException("look");
            try
            {
                Appearance ap = Player();
                if (!Ready(ap) || !Live(look, ap)) return false;
                Notify(ap);
                return true;
            }
            catch (Exception e)
            {
                Fail("Live", e);
                return false;
            }
        }

        public static bool Live(Look look, Appearance appearance)
        {
            if (look == null) throw new ArgumentNullException("look");
            if (appearance == null) throw new ArgumentNullException("appearance");
            try
            {
                if (!Ready(appearance)) return false;
                foreach (LookDial dial in PlayerLookTable.All) dial.SetLive(appearance, PlayerLookTable.Clamp(dial, dial.GetLook(look)));
                appearance.UpdateShapes();
                return true;
            }
            catch (Exception e)
            {
                Fail("Live", e);
                return false;
            }
        }

        public static Look Randomise()
        {
            try
            {
                Appearance ap = Player();
                if (!Ready(ap) || Randomise(ap) == null) return null;
                return Notify(ap);
            }
            catch (Exception e)
            {
                Fail("Randomise", e);
                return null;
            }
        }

        public static Look Randomise(Appearance appearance)
        {
            if (appearance == null) throw new ArgumentNullException("appearance");
            try
            {
                if (!Ready(appearance)) return null;
                Look look = Capture(appearance);
                PlayerLookTable.Roll(look, appearance);
                Put(look, appearance);
                return Capture(appearance);
            }
            catch (Exception e)
            {
                Fail("Randomise", e);
                return null;
            }
        }

        public static Look Vanilla()
        {
            try
            {
                Appearance ap = Player();
                return Ready(ap) ? PlayerLookTable.Vanilla(ap) : null;
            }
            catch (Exception e)
            {
                Fail("Vanilla", e);
                return null;
            }
        }

        public static IList<string> Options(string slot, bool male)
        {
            return Options(slot, male, Player());
        }

        public static IList<string> Options(string slot, bool male, Appearance appearance)
        {
            LookSlot found = PlayerLookTable.Find(slot);
            List<string> names = new List<string>();
            try
            {
                if (!Ready(appearance)) return names;
                Look probe = new Look();
                probe.male = male;
                Wearable[] choices = found.Choices(appearance, probe);
                if (choices == null) return names;
                for (int i = 0; i < choices.Length; i++) names.Add(PlayerLookTable.NameOf(choices[i]));
                return names;
            }
            catch (Exception e)
            {
                Fail("Options", e);
                return names;
            }
        }

        private static void Put(Look look, Appearance ap)
        {
            Character costume = Costume(look, ap);
            Release(ap.createdCharacter);
            ap.createdCharacter = costume;
            costumes.Add(costume);
            ap.LoadCharacter();
        }

        private static void Release(Character previous)
        {
            costumes.RemoveWhere(c => c == null);
            if (previous != null && costumes.Remove(previous)) UnityEngine.Object.Destroy(previous);
        }

        private static Character Costume(Look look, Appearance ap)
        {
            Character costume = ScriptableObject.CreateInstance<Character>();
            costume.GenderString = look.male ? "Male" : "Female";
            costume.Advanced = look.advanced;
            foreach (LookDial dial in PlayerLookTable.All) dial.SetCostume(costume, PlayerLookTable.Clamp(dial, dial.GetLook(look)));
            foreach (LookSlot slot in PlayerLookTable.Slots)
            {
                Wearable[] choices = slot.Choices(ap, look);
                int index = PlayerLookTable.Index(slot, look, choices);
                slot.Put(costume, index < 0 ? null : choices[index]);
            }
            if (costume.Head == null) costume.Head = First(look.male ? ap.maleHeads : ap.femaleHeads);
            return costume;
        }

        private static Look Notify(Appearance ap)
        {
            Look look = Capture(ap);
            Events.Raise(Changed, "PlayerLook.Changed", h => h(look));
            return look;
        }

        private static Appearance Player()
        {
            PlayerLocator locator = PlayerLocator.instance;
            return locator == null ? null : locator.GetComponentInChildren<Appearance>(true);
        }

        private static bool Ready(Appearance ap)
        {
            if (!Core.PatchingAllowed || ap == null) return false;
            if (bodyModels == null)
            {
                if (!missingLogged)
                {
                    missingLogged = true;
                    CoreLog.For(Owner).Warn("Appearance.bodyModels not found; looks are not applied");
                }
                return false;
            }
            return bodyModels.GetValue(ap) != null;
        }

        private static Wearable First(Wearable[] list)
        {
            return list != null && list.Length > 0 ? list[0] : null;
        }

        private static void Fail(string what, Exception e)
        {
            CoreLog.For(Owner).Error(what + " failed: " + e.ToString());
        }

        [Serializable]
        public sealed class Look
        {
            public bool set;
            public bool male;
            public bool advanced;
            public float height = 0.8f;
            public float shape;
            public float gender;
            public float headSize = 1f;
            public float faceSize = 1f;
            public float neckLength;
            public float shoulderWidth;
            public float breastSize;
            public float hipWidth;
            public float handSize = 1f;
            public float footSize = 1f;
            public float lipThickness;
            public float jawPosition;
            public float chinPosition;
            public float eyeSize = 1f;
            public float eyeDistance;
            public float eyePosition;
            public float eyeDepth;
            public float earSize = 1f;
            public float earRotation;
            public float noseRootRotation;
            public float noseTipRotation;
            public float noseWidth;
            public string head = "";
            public string hat = "";
            public string hair = "";
            public string glasses = "";
            public string beard = "";
            public string shirtT = "";
            public string shirtB = "";
            public string pantsT = "";
            public string pantsB = "";
            public string socks = "";
            public string shoes = "";
        }
    }
}
