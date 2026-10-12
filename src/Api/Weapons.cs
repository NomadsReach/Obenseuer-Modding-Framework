using System;
using System.Collections.Generic;
using UnityEngine;

namespace ObenseuerCore
{
    public enum WeaponKind
    {
        None = 0,
        Melee = 1,
        Explosive = 2
    }

    public sealed class WeaponInfo
    {
        public int ItemId { get; private set; }

        public WeaponKind Kind { get; private set; }

        public int AmmoLoaded { get; private set; }

        public int AmmoCapacity { get; private set; }

        internal WeaponInfo(int itemId, WeaponKind kind, int ammoLoaded, int ammoCapacity)
        {
            ItemId = itemId;
            Kind = kind;
            AmmoLoaded = ammoLoaded;
            AmmoCapacity = ammoCapacity;
        }
    }

    public sealed class WeaponFiredArgs
    {
        public int ItemId { get; private set; }

        public WeaponKind Kind { get; private set; }

        public int AmmoLoaded { get; private set; }

        internal WeaponFiredArgs(int itemId, WeaponKind kind, int ammoLoaded)
        {
            ItemId = itemId;
            Kind = kind;
            AmmoLoaded = ammoLoaded;
        }
    }

    public static class Weapons
    {
        internal const string Owner = "obenseuer.moddingframework.weapons";
        private static bool installed;

        public static event Action<WeaponFiredArgs> Fired;

        public static event Action<WeaponInfo> MeleeSwung;

        public static event Action<WeaponInfo> Reloaded;

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        public static WeaponInfo Equipped()
        {
            return Guard("Equipped", () => Describe(HeldHand()), null);
        }

        public static int Ammo(int ammoItemId)
        {
            return PlayerInventory.Count(ammoItemId);
        }

        public static bool Reload()
        {
            return Guard("Reload", () => ReloadHeld(), false);
        }

        internal static bool WatchesFired
        {
            get
            {
                return Fired != null;
            }
        }

        internal static bool WatchesMelee
        {
            get
            {
                return MeleeSwung != null;
            }
        }

        internal static bool WatchesReload
        {
            get
            {
                return Reloaded != null;
            }
        }

        internal static void Init()
        {
            installed = WeaponsPatches.Install();
            if (installed) return;
            CorePatcher.DisableOwner(Owner);
            CoreLog.For(Owner).Warn("weapon hooks incomplete, weapon events are disabled");
        }

        internal static WeaponInfo Describe(PlayerHandItems.IPlayerHandItem hand)
        {
            if (hand == null || hand.item == null) return null;
            OS.Items.ItemAmmodata ammo = hand.meta == null ? null : ItemMetaUtilities.GetMetaOfType<OS.Items.ItemAmmodata>(hand.meta);
            return new WeaponInfo(hand.item.ID, KindOf(hand), ammo == null ? 0 : (int)ammo.ammo, ammo == null ? 0 : (int)ammo.maxAmmo);
        }

        internal static void RaiseFired(WeaponInfo weapon)
        {
            WeaponFiredArgs args = new WeaponFiredArgs(weapon.ItemId, weapon.Kind, weapon.AmmoLoaded);
            Events.Raise(Fired, "Weapons.Fired", h => h(args));
        }

        internal static void RaiseMeleeSwung(WeaponInfo weapon)
        {
            Events.Raise(MeleeSwung, "Weapons.MeleeSwung", h => h(weapon));
        }

        internal static void RaiseReloaded(WeaponInfo weapon)
        {
            Events.Raise(Reloaded, "Weapons.Reloaded", h => h(weapon));
        }


        private static PlayerHandItems.IPlayerHandItem HeldHand()
        {
            PlayerHandItems hands = PlayerHandItems.instance;
            return hands == null ? null : hands.righthandItem;
        }

        private static WeaponKind KindOf(PlayerHandItems.IPlayerHandItem hand)
        {
            Component component = hand as Component;
            if (component == null) return WeaponKind.None;
            if (component is WieldableItem.MeleeWeapon) return WeaponKind.Melee;
            if (component is WieldableItem.Detonator) return WeaponKind.Explosive;
            return WeaponKind.None;
        }

        private static bool ReloadHeld()
        {
            PlayerHandItems hands = PlayerHandItems.instance;
            WeaponInfo weapon = Describe(HeldHand());
            if (hands == null || hands.itemUseDisabled || MenuVisible() || weapon == null || weapon.Kind == WeaponKind.None || weapon.AmmoCapacity <= 0) return false;
            hands.ReloadUse();
            return true;
        }

        private static bool MenuVisible()
        {
            return GameUIController.instance != null && GameUIController.instance.IsMenuVisible;
        }

        private static T Guard<T>(string member, Func<T> read, T fallback)
        {
            try
            {
                return read();
            }
            catch (Exception e)
            {
                ApiLog.Once(Weapons.Owner, member, e);
                return fallback;
            }
        }
    }
}
