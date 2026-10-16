using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ObenseuerCore
{
    public enum WeaponKind
    {
        None = 0,
        Melee = 1,
        Explosive = 2
    }

    public enum HitTarget
    {
        Other = 0,
        Mining = 1,
        Chopping = 2,
        Breakable = 3,
        Animal = 4
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

    public sealed class HitArgs
    {
        public WeaponInfo Weapon { get; private set; }

        public HitTarget Target { get; private set; }

        public GameObject Receiver { get; private set; }

        public int BaseDamage { get; private set; }

        public int FinalDamage { get; private set; }

        internal HitArgs(WeaponInfo weapon, HitTarget target, GameObject receiver, int baseDamage, int finalDamage)
        {
            Weapon = weapon;
            Target = target;
            Receiver = receiver;
            BaseDamage = baseDamage;
            FinalDamage = finalDamage;
        }
    }

    public static class Weapons
    {
        internal const string Owner = "obenseuer.moddingframework.weapons";
        internal const string ReloadOwner = Owner + ".reload";
        internal const string MeleeOwner = Owner + ".melee";
        internal const string HitOwner = Owner + ".hit";
        private const float MaxHitFactor = 10f;
        private static readonly FieldInfo FlashlightLit = typeof(WieldableItem.Flashlight).GetField("on", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo NodeSkill = typeof(WorkableResourceSource).GetField("skillType", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly PayoutFactors<HitTarget> hitFactors = new PayoutFactors<HitTarget>(Owner);
        private static bool installed;
        private static bool reloadInstalled;
        private static bool meleeInstalled;
        private static bool hitInstalled;

        public static event Action<WeaponFiredArgs> Fired;

        public static event Action<WeaponInfo> MeleeSwung;

        public static event Action<WeaponInfo> Reloaded;

        public static event Action<HitArgs> Hit;

        public static bool IsAvailable
        {
            get
            {
                return installed && reloadInstalled && meleeInstalled && !CorePatcher.IsDisabled(Owner) && !CorePatcher.IsDisabled(ReloadOwner) && !CorePatcher.IsDisabled(MeleeOwner);
            }
        }

        private static bool HitAvailable
        {
            get
            {
                return hitInstalled && !CorePatcher.IsDisabled(HitOwner);
            }
        }

        public static WeaponInfo Equipped()
        {
            return Guard("Equipped", () => Describe(HeldHand()), null);
        }

        public static WeaponInfo EquippedLeft()
        {
            return Guard("EquippedLeft", () => Describe(LeftHand()), null);
        }

        public static int Ammo(int ammoItemId)
        {
            return PlayerInventory.Count(ammoItemId);
        }

        public static bool Reload()
        {
            return Guard("Reload", () => ReloadHeld(), false);
        }

        public static bool? FlashlightOn()
        {
            return Guard<bool?>("FlashlightOn", () => Lit(HeldFlashlight()), null);
        }

        public static float? FlashlightCharge()
        {
            return Guard<float?>("FlashlightCharge", () => Charge(HeldFlashlight()), null);
        }

        public static bool SetFlashlightOn(bool on)
        {
            return Guard("SetFlashlightOn", () => SwitchFlashlight(on), false);
        }

        public static bool SetHitFactor(string modId, HitTarget target, float factor)
        {
            return SetHitFactor(modId, target, factor, false);
        }

        public static bool SetHitFactor(string modId, HitTarget target, float factor, bool chanceRounding)
        {
            if (!ValidMod(modId) || !HitAvailable) return false;
            if (!Enum.IsDefined(typeof(HitTarget), target) || float.IsNaN(factor) || factor < 0f || factor > MaxHitFactor)
            {
                ApiLog.Once(Owner, "hit factor", "factor must be 0 to " + MaxHitFactor + " and the target must be a HitTarget value");
                return false;
            }
            hitFactors.Set(modId, target, factor, chanceRounding);
            return true;
        }

        public static bool ClearHitFactors(string modId)
        {
            if (!ValidMod(modId)) return false;
            return hitFactors.Clear(modId);
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

        internal static bool WatchesHit
        {
            get
            {
                return Hit != null;
            }
        }

        internal static void Init()
        {
            installed = WeaponsPatches.InstallFired();
            reloadInstalled = WeaponsPatches.InstallReload();
            meleeInstalled = WeaponsPatches.InstallMelee();
            hitInstalled = WeaponsPatches.InstallHit();
            Events.SceneLoadedWithMode += OnSceneLoadedWithMode;
            if (!hitInstalled) CoreLog.For(HitOwner).Warn("weapon hit hooks incomplete, hit factors and the Hit event are disabled");
            if (installed && reloadInstalled && meleeInstalled) return;
            CoreLog.For(Owner).Warn("weapon hooks incomplete, IsAvailable is false, the events whose hooks are missing are disabled");
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

        internal static void RaiseHit(HitArgs args)
        {
            Events.Raise(Hit, "Weapons.Hit", h => h(args));
        }

        internal static void ScaleHit(WieldableItem.MeleeWeapon hitter, Component receiver, bool hitLands, ref int damage, DamageReceiver.DamageType type)
        {
            if (!HitAvailable || hitter == null || !hitLands || (!hitFactors.Any && !WatchesHit) || !((IDamageReceiver)receiver).CheckDamageType(type)) return;
            HitTarget target = TargetOf(receiver, type);
            float factor = hitFactors.Of(target);
            int scaled = hitFactors.ChanceRounds(target) ? PayoutFactors<HitTarget>.ScaleChance(damage, factor) : PayoutFactors<HitTarget>.Scale(damage, factor);
            int baseDamage = damage;
            damage = scaled;
            if (WatchesHit)
            {
                WeaponInfo weapon = Describe(hitter);
                if (weapon != null) RaiseHit(new HitArgs(weapon, target, receiver.gameObject, baseDamage, scaled));
            }
        }


        private static PlayerHandItems.IPlayerHandItem HeldHand()
        {
            PlayerHandItems hands = PlayerHandItems.instance;
            return hands == null ? null : hands.righthandItem;
        }

        private static PlayerHandItems.IPlayerHandItem LeftHand()
        {
            PlayerHandItems hands = PlayerHandItems.instance;
            return hands == null ? null : hands.lefthandItem;
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
            PlayerHandItems.IPlayerHandItem hand = HeldHand();
            WeaponInfo weapon = Describe(hand);
            if (SaveGate.Busy || hands == null || hands.itemUseDisabled || MenuVisible() || weapon == null || weapon.Kind == WeaponKind.None || weapon.AmmoCapacity <= 0) return false;
            int before = AmmoItemsCarried(hand);
            hands.ReloadUse();
            return AmmoItemsCarried(hand) < before;
        }

        private static int AmmoItemsCarried(PlayerHandItems.IPlayerHandItem hand)
        {
            OS.Items.ItemAmmodata ammo = hand.meta == null ? null : ItemMetaUtilities.GetMetaOfType<OS.Items.ItemAmmodata>(hand.meta);
            int total = 0;
            if (ammo != null && ammo.ammoItems != null)
            {
                foreach (ItemReference reference in ammo.ammoItems) total += PlayerInventory.Count(reference.ID);
            }
            return total;
        }

        private static WieldableItem.Flashlight HeldFlashlight()
        {
            PlayerHandItems hands = PlayerHandItems.instance;
            if (hands == null) return null;
            return (hands.righthandItem as WieldableItem.Flashlight) ?? (hands.lefthandItem as WieldableItem.Flashlight);
        }

        private static bool? Lit(WieldableItem.Flashlight light)
        {
            if (light == null) return null;
            OS.Items.ItemAmmodata ammo = light.meta == null ? null : ItemMetaUtilities.GetMetaOfType<OS.Items.ItemAmmodata>(light.meta);
            return (bool)FlashlightLit.GetValue(light) && ammo != null && ammo.ammo > 0f;
        }

        private static float? Charge(WieldableItem.Flashlight light)
        {
            if (light == null || light.meta == null) return null;
            OS.Items.ItemAmmodata ammo = ItemMetaUtilities.GetMetaOfType<OS.Items.ItemAmmodata>(light.meta);
            if (ammo == null || ammo.maxAmmo <= 0f) return null;
            return ammo.ammo / ammo.maxAmmo;
        }

        private static bool SwitchFlashlight(bool on)
        {
            PlayerHandItems hands = PlayerHandItems.instance;
            WieldableItem.Flashlight light = HeldFlashlight();
            if (hands == null || light == null || hands.itemUseDisabled || MenuVisible() || SaveGate.Busy) return false;
            if (on) light.TurnOn();
            else light.TurnOff();
            return Lit(light) == on;
        }

        private static bool MenuVisible()
        {
            return GameUIController.instance != null && GameUIController.instance.IsMenuVisible;
        }

        private static HitTarget TargetOf(Component receiver, DamageReceiver.DamageType type)
        {
            if (receiver is WorkableResourceSource)
            {
                if (IsMiningNode(receiver)) return HitTarget.Mining;
                return type == DamageReceiver.DamageType.Chop ? HitTarget.Chopping : HitTarget.Other;
            }
            SpawnItemOnBreak smashable = receiver as SpawnItemOnBreak;
            if (smashable != null && smashable.objectType == SpawnItemOnBreak.Type.Animal) return HitTarget.Animal;
            return HitTarget.Breakable;
        }

        private static bool IsMiningNode(Component node)
        {
            return NodeSkill != null && (PlayerStats.PlayerSkillType)NodeSkill.GetValue(node) == PlayerStats.PlayerSkillType.Mining;
        }

        private static void OnSceneLoadedWithMode(string scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Single) hitFactors.Reset();
        }

        private static bool ValidMod(string modId)
        {
            if (ModIds.IsValid(modId)) return true;
            ApiLog.Once(Owner, "modId", "modId must be 1 to 64 characters of letters, digits, '.', '_' or '-'");
            return false;
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
