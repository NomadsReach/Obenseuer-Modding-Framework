using System;
using System.Linq;
using UnityEngine;

namespace ObenseuerCore
{
    internal sealed class LookDial
    {
        internal readonly string Label;
        internal readonly float Min;
        internal readonly float Max;
        internal readonly Func<PlayerLook.Look, float> GetLook;
        internal readonly Action<PlayerLook.Look, float> SetLook;
        internal readonly Func<Appearance, float> GetLive;
        internal readonly Action<Appearance, float> SetLive;
        internal readonly Action<Character, float> SetCostume;

        internal LookDial(string label, float min, float max, Func<PlayerLook.Look, float> getLook, Action<PlayerLook.Look, float> setLook, Func<Appearance, float> getLive, Action<Appearance, float> setLive, Action<Character, float> setCostume)
        {
            Label = label;
            Min = min;
            Max = max;
            GetLook = getLook;
            SetLook = setLook;
            GetLive = getLive;
            SetLive = setLive;
            SetCostume = setCostume;
        }
    }

    internal sealed class LookSlot
    {
        internal readonly string Label;
        internal readonly Func<Appearance, PlayerLook.Look, Wearable[]> Choices;
        internal readonly Func<PlayerLook.Look, string> Get;
        internal readonly Action<PlayerLook.Look, string> Set;
        internal readonly Func<Appearance, Wearable> Selected;
        internal readonly Action<Character, Wearable> Put;

        internal LookSlot(string label, Func<Appearance, PlayerLook.Look, Wearable[]> choices, Func<PlayerLook.Look, string> get, Action<PlayerLook.Look, string> set, Func<Appearance, Wearable> selected, Action<Character, Wearable> put)
        {
            Label = label;
            Choices = choices;
            Get = get;
            Set = set;
            Selected = selected;
            Put = put;
        }
    }

    internal static class PlayerLookTable
    {
        internal static readonly LookDial[] Body = new LookDial[]
        {
            new LookDial("Height", 0f, 1f, l => l.height, (l, v) => l.height = v, a => a.height, (a, v) => a.height = v, (c, v) => c.Height = v),
            new LookDial("Shape", -1f, 1f, l => l.shape, (l, v) => l.shape = v, a => a.shape, (a, v) => a.shape = v, (c, v) => c.Weight = v),
            new LookDial("Gender", 0f, 1f, l => l.gender, (l, v) => l.gender = v, a => a.gender, (a, v) => a.gender = v, (c, v) => c.Gender = v),
            new LookDial("Head size", 0f, 1f, l => l.headSize, (l, v) => l.headSize = v, a => a.headSize, (a, v) => a.headSize = v, (c, v) => c.HeadSize = v),
            new LookDial("Face size", 0f, 1f, l => l.faceSize, (l, v) => l.faceSize = v, a => a.faceSize, (a, v) => a.faceSize = v, (c, v) => c.FaceSize = v),
            new LookDial("Neck length", 0f, 1f, l => l.neckLength, (l, v) => l.neckLength = v, a => a.neckLength, (a, v) => a.neckLength = v, (c, v) => c.NeckLength = v),
            new LookDial("Shoulder width", 0f, 1f, l => l.shoulderWidth, (l, v) => l.shoulderWidth = v, a => a.shoulderWidth, (a, v) => a.shoulderWidth = v, (c, v) => c.ShoulderWidth = v),
            new LookDial("Breast size", 0f, 1f, l => l.breastSize, (l, v) => l.breastSize = v, a => a.breastSize, (a, v) => a.breastSize = v, (c, v) => c.BreastSize = v),
            new LookDial("Hip width", 0f, 1f, l => l.hipWidth, (l, v) => l.hipWidth = v, a => a.hipWidth, (a, v) => a.hipWidth = v, (c, v) => c.HipWidth = v),
            new LookDial("Hand size", 0f, 1f, l => l.handSize, (l, v) => l.handSize = v, a => a.handSize, (a, v) => a.handSize = v, (c, v) => c.HandSize = v),
            new LookDial("Foot size", 0f, 1f, l => l.footSize, (l, v) => l.footSize = v, a => a.footSize, (a, v) => a.footSize = v, (c, v) => c.FootSize = v),
        };

        internal static readonly LookDial[] Face = new LookDial[]
        {
            new LookDial("Eye size", 0f, 1f, l => l.eyeSize, (l, v) => l.eyeSize = v, a => a.eyeSize, (a, v) => a.eyeSize = v, (c, v) => c.EyeSize = v),
            new LookDial("Eye distance", 0f, 1f, l => l.eyeDistance, (l, v) => l.eyeDistance = v, a => a.eyeDistance, (a, v) => a.eyeDistance = v, (c, v) => c.EyeDistance = v),
            new LookDial("Eye position", 0f, 1f, l => l.eyePosition, (l, v) => l.eyePosition = v, a => a.eyePosition, (a, v) => a.eyePosition = v, (c, v) => c.EyePosition = v),
            new LookDial("Eye depth", 0f, 1f, l => l.eyeDepth, (l, v) => l.eyeDepth = v, a => a.eyeDepth, (a, v) => a.eyeDepth = v, (c, v) => c.EyeDepth = v),
            new LookDial("Ear size", 0f, 1f, l => l.earSize, (l, v) => l.earSize = v, a => a.earSize, (a, v) => a.earSize = v, (c, v) => c.EarSize = v),
            new LookDial("Ear rotation", 0f, 1f, l => l.earRotation, (l, v) => l.earRotation = v, a => a.earRotation, (a, v) => a.earRotation = v, (c, v) => c.EarRotation = v),
            new LookDial("Nose root", 0f, 1f, l => l.noseRootRotation, (l, v) => l.noseRootRotation = v, a => a.noseRootRotation, (a, v) => a.noseRootRotation = v, (c, v) => c.NoseRootRotation = v),
            new LookDial("Nose tip", 0f, 1f, l => l.noseTipRotation, (l, v) => l.noseTipRotation = v, a => a.noseTipRotation, (a, v) => a.noseTipRotation = v, (c, v) => c.NoseTipRotation = v),
            new LookDial("Nose width", 0f, 1f, l => l.noseWidth, (l, v) => l.noseWidth = v, a => a.noseWidth, (a, v) => a.noseWidth = v, (c, v) => c.NoseWidth = v),
            new LookDial("Lip thickness", 0f, 1f, l => l.lipThickness, (l, v) => l.lipThickness = v, a => a.lipThickness, (a, v) => a.lipThickness = v, (c, v) => c.LipThickness = v),
            new LookDial("Jaw", 0f, 1f, l => l.jawPosition, (l, v) => l.jawPosition = v, a => a.jawPosition, (a, v) => a.jawPosition = v, (c, v) => c.JawPosition = v),
            new LookDial("Chin", 0f, 1f, l => l.chinPosition, (l, v) => l.chinPosition = v, a => a.chinPosition, (a, v) => a.chinPosition = v, (c, v) => c.ChinPosition = v),
        };

        internal static readonly LookDial[] All = Body.Concat(Face).ToArray();

        internal static readonly LookSlot[] Slots = new LookSlot[]
        {
            new LookSlot("Head", (a, l) => l.male ? a.maleHeads : a.femaleHeads, l => l.head, (l, v) => l.head = v, a => a.selectedHead, (c, w) => c.Head = w),
            new LookSlot("Hat", (a, l) => a.hats, l => l.hat, (l, v) => l.hat = v, a => a.selectedHat, (c, w) => c.Hat = w),
            new LookSlot("Hair", (a, l) => a.hairs, l => l.hair, (l, v) => l.hair = v, a => a.selectedHair, (c, w) => c.Hair = w),
            new LookSlot("Glasses", (a, l) => a.glasses, l => l.glasses, (l, v) => l.glasses = v, a => a.selectedGlasses, (c, w) => c.Glasses = w),
            new LookSlot("Beard", (a, l) => a.beards, l => l.beard, (l, v) => l.beard = v, a => a.selectedBeard, (c, w) => c.Beard = w),
            new LookSlot("Jacket", (a, l) => a.shirtsT, l => l.shirtT, (l, v) => l.shirtT = v, a => a.selectedShirtT, (c, w) => c.Jacket = w),
            new LookSlot("Shirt", (a, l) => a.shirtsB, l => l.shirtB, (l, v) => l.shirtB = v, a => a.selectedShirtB, (c, w) => c.ShirtB = w),
            new LookSlot("Pants", (a, l) => a.pantsT, l => l.pantsT, (l, v) => l.pantsT = v, a => a.selectedPantsT, (c, w) => c.PantsT = w),
            new LookSlot("Leggings", (a, l) => a.pantsB, l => l.pantsB, (l, v) => l.pantsB = v, a => a.selectedPantsB, (c, w) => c.PantsB = w),
            new LookSlot("Socks", (a, l) => a.socks, l => l.socks, (l, v) => l.socks = v, a => a.selectedSocks, (c, w) => c.Socks = w),
            new LookSlot("Shoes", (a, l) => a.shoes, l => l.shoes, (l, v) => l.shoes = v, a => a.selectedShoes, (c, w) => c.Shoes = w),
        };

        internal static LookSlot Find(string label)
        {
            foreach (LookSlot slot in Slots)
            {
                if (slot.Label == label) return slot;
            }
            throw new ArgumentException("unknown slot '" + label + "'", "slot");
        }

        internal static float Clamp(LookDial dial, float value)
        {
            return Mathf.Clamp(value, dial.Min, dial.Max);
        }

        internal static bool PinnedToFirst(LookSlot slot, PlayerLook.Look look)
        {
            return slot.Label == "Beard" && !look.male;
        }

        internal static int Index(LookSlot slot, PlayerLook.Look look, Wearable[] choices)
        {
            if (choices == null || choices.Length == 0) return -1;
            return IndexOf(choices, slot.Get(look));
        }

        internal static int Pick(LookSlot slot, PlayerLook.Look look, Wearable[] choices)
        {
            if (slot.Label == "Leggings" || PinnedToFirst(slot, look)) return 0;
            return UnityEngine.Random.Range(0, choices.Length);
        }

        internal static void Roll(PlayerLook.Look look, Appearance ap)
        {
            foreach (LookDial dial in All)
            {
                if (dial != Body[0]) dial.SetLook(look, UnityEngine.Random.Range(dial.Min, dial.Max));
            }
            Wearables(look, ap);
        }

        internal static PlayerLook.Look Vanilla(Appearance ap)
        {
            PlayerLook.Look look = new PlayerLook.Look();
            look.set = true;
            look.male = UnityEngine.Random.value > 0.5f;
            look.advanced = UnityEngine.Random.value > 0.5f;
            foreach (LookDial dial in All)
            {
                float value = UnityEngine.Random.Range(dial.Min, dial.Max);
                if (dial.Label == "Breast size") value = look.male ? UnityEngine.Random.Range(0f, 0.3f) : UnityEngine.Random.Range(0.3f, 1f);
                if (dial.Label == "Gender") value = Mathf.RoundToInt(value);
                dial.SetLook(look, value);
            }
            Wearables(look, ap);
            return look;
        }

        internal static int IndexOf(Wearable[] list, string name)
        {
            if (list == null || string.IsNullOrEmpty(name)) return -1;
            for (int i = 0; i < list.Length; i++)
            {
                if (list[i] != null && list[i].name == name) return i;
            }
            return -1;
        }

        internal static string NameOf(Wearable wearable)
        {
            return wearable == null ? "" : wearable.name;
        }

        private static void Wearables(PlayerLook.Look look, Appearance ap)
        {
            foreach (LookSlot slot in Slots)
            {
                Wearable[] choices = slot.Choices(ap, look);
                slot.Set(look, choices == null || choices.Length == 0 ? "" : NameOf(choices[Pick(slot, look, choices)]));
            }
        }
    }

    internal static class PlayerLookCheck
    {
        internal static string Run()
        {
            Wearable[] males = Make("m0", "m1", "m2");
            Wearable[] beards = Make("b0", "b1", "b2");
            try
            {
                return Verify(males, beards);
            }
            finally
            {
                Discard(males);
                Discard(beards);
            }
        }

        private static string Verify(Wearable[] males, Wearable[] beards)
        {
            foreach (LookDial dial in PlayerLookTable.All)
            {
                float middle = (dial.Min + dial.Max) / 2f;
                if (PlayerLookTable.Clamp(dial, dial.Max + 1f) != dial.Max) return "clamp above " + dial.Label;
                if (PlayerLookTable.Clamp(dial, dial.Min - 1f) != dial.Min) return "clamp below " + dial.Label;
                if (PlayerLookTable.Clamp(dial, middle) != middle) return "clamp inside " + dial.Label;
            }
            LookSlot head = PlayerLookTable.Find("Head");
            LookSlot beard = PlayerLookTable.Find("Beard");
            LookSlot leggings = PlayerLookTable.Find("Leggings");
            PlayerLook.Look man = new PlayerLook.Look();
            man.male = true;
            man.head = "m1";
            man.beard = "b2";
            PlayerLook.Look woman = new PlayerLook.Look();
            woman.male = false;
            woman.beard = "b2";
            if (PlayerLookTable.Index(head, man, males) != 1) return "head by name";
            man.head = "f1";
            if (PlayerLookTable.Index(head, man, males) != -1) return "head from the other gender";
            if (PlayerLookTable.Index(beard, man, beards) != 2) return "male beard by name";
            if (PlayerLookTable.Index(beard, woman, beards) != 2) return "female beard by name";
            if (PlayerLookTable.Pick(beard, woman, beards) != 0) return "female beard roll index 0";
            if (PlayerLookTable.Pick(leggings, man, beards) != 0) return "leggings roll index 0";
            if (PlayerLookTable.Index(beard, woman, new Wearable[0]) != -1) return "empty choices";
            return null;
        }

        private static Wearable[] Make(params string[] names)
        {
            Wearable[] list = new Wearable[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                list[i] = ScriptableObject.CreateInstance<Wearable>();
                list[i].name = names[i];
            }
            return list;
        }

        private static void Discard(Wearable[] list)
        {
            foreach (Wearable wearable in list) UnityEngine.Object.Destroy(wearable);
        }
    }
}
