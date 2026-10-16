using System;
using System.Reflection;

namespace ObenseuerCore
{
    public static class WorldRadiation
    {
        internal const string Owner = "obenseuer.moddingframework.worldradiation";
        private static readonly FieldInfo DosimeterSwitch = typeof(Dosimeter).GetField("on", BindingFlags.Instance | BindingFlags.NonPublic);
        private static float doseRate;

        public static float Dose
        {
            get
            {
                return PlayerStats.instance == null || PlayerStats.instance.radiation == null ? 0f : PlayerStats.instance.radiation.Dose;
            }
        }

        public static float DoseDamageThreshold
        {
            get
            {
                return PlayerStats.instance == null || PlayerStats.instance.radiation == null ? 0f : PlayerStats.instance.radiation.DoseDamageThreshold;
            }
        }

        public static float BackgroundLevel
        {
            get
            {
                return RadiationController.instance == null || SMVEffects.instance == null ? 0f : RadiationController.instance.GetBackgroundRadiation();
            }
        }

        public static bool IsWasteland
        {
            get
            {
                return SMVEffects.instance != null && SMVEffects.instance.IsWastelandIsActive();
            }
        }

        public static float DoseRate
        {
            get
            {
                if (PlayerLocator.instance == null || RadiationController.instance == null) return 0f;
                PlayerStats stats = PlayerStats.instance;
                return stats == null || stats.StatsProgressDisabled ? 0f : doseRate;
            }
        }

        public static bool DosimeterEquipped
        {
            get
            {
                PlayerHandItems hands = PlayerHandItems.instance;
                return hands != null && (IsEquippedDosimeter(hands.righthandItem) || IsEquippedDosimeter(hands.lefthandItem));
            }
        }

        public static bool? DosimeterOn()
        {
            return Guard<bool?>("DosimeterOn", () => Lit(HeldDosimeter()), null);
        }

        public static float? DosimeterCharge()
        {
            return Guard<float?>("DosimeterCharge", () => Charge(HeldDosimeter()), null);
        }

        internal static void Init()
        {
            Events.Loaded += OnLoaded;
            if (WorldRadiationPatches.Apply()) return;
            CoreLog.For(Owner).Warn("dosimeter rate hook missing, DoseRate stays 0");
        }

        internal static void RecordRate(float value)
        {
            doseRate = value;
        }

        private static void OnLoaded()
        {
            doseRate = 0f;
        }

        private static bool IsEquippedDosimeter(PlayerHandItems.IPlayerHandItem hand)
        {
            Dosimeter dosimeter = hand as Dosimeter;
            return dosimeter != null && dosimeter.equipped;
        }

        private static Dosimeter HeldDosimeter()
        {
            PlayerHandItems hands = PlayerHandItems.instance;
            if (hands == null) return null;
            return (hands.righthandItem as Dosimeter) ?? (hands.lefthandItem as Dosimeter);
        }

        private static bool? Lit(Dosimeter dosimeter)
        {
            if (dosimeter == null || DosimeterSwitch == null) return null;
            OS.Items.ItemAmmodata ammo = AmmoOf(dosimeter);
            return (bool)DosimeterSwitch.GetValue(dosimeter) && ammo != null && ammo.ammo > 0f;
        }

        private static float? Charge(Dosimeter dosimeter)
        {
            if (dosimeter == null) return null;
            OS.Items.ItemAmmodata ammo = AmmoOf(dosimeter);
            if (ammo == null || ammo.maxAmmo <= 0f) return null;
            return ammo.ammo / ammo.maxAmmo;
        }

        private static OS.Items.ItemAmmodata AmmoOf(Dosimeter dosimeter)
        {
            return dosimeter.meta == null ? null : ItemMetaUtilities.GetMetaOfType<OS.Items.ItemAmmodata>(dosimeter.meta);
        }

        private static T Guard<T>(string member, Func<T> read, T fallback)
        {
            try
            {
                return read();
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, member, e);
                return fallback;
            }
        }
    }
}
