using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace ObenseuerCore
{
    internal static class WeaponsPatches
    {
        private static readonly Type[] DamageArgs = new[] { typeof(int), typeof(Vector3), typeof(Vector3), typeof(DamageReceiver.DamageType) };
        private static WieldableItem.MeleeWeapon hitter;

        internal static bool InstallFired()
        {
            try
            {
                return Hook(Weapons.Owner, typeof(WieldableItem.Detonator), "Onpressed", Type.EmptyTypes, "PressPrefix", "PressPostfix");
            }
            catch (Exception e)
            {
                CorePatcher.DisableOwner(Weapons.Owner);
                CoreLog.For(Weapons.Owner).Error("weapon hook install failed: " + e.ToString());
                return false;
            }
        }

        internal static bool InstallReload()
        {
            try
            {
                return Hook(Weapons.ReloadOwner, typeof(WieldableItem.Detonator), "Reload", new[] { typeof(float) }, null, "ReloadPostfix");
            }
            catch (Exception e)
            {
                CorePatcher.DisableOwner(Weapons.ReloadOwner);
                CoreLog.For(Weapons.ReloadOwner).Error("weapon hook install failed: " + e.ToString());
                return false;
            }
        }

        internal static bool InstallMelee()
        {
            try
            {
                return Hook(Weapons.MeleeOwner, typeof(WieldableItem.MeleeAnimationEvents), "EnableHit", Type.EmptyTypes, null, "EnableHitPostfix");
            }
            catch (Exception e)
            {
                CorePatcher.DisableOwner(Weapons.MeleeOwner);
                CoreLog.For(Weapons.MeleeOwner).Error("weapon hook install failed: " + e.ToString());
                return false;
            }
        }

        internal static bool InstallHit()
        {
            try
            {
                bool ok = CorePatcher.PatchCore(typeof(WieldableItem.MeleeWeapon), "Hit", new[] { typeof(RaycastHit) }, CorePatcher.Handler(typeof(WeaponsPatches), "MeleeHitPrefix"), null, CorePatcher.Handler(typeof(WeaponsPatches), "MeleeHitFinalizer"), true, Weapons.HitOwner);
                ok &= PatchDamage(typeof(WorkableResourceSource), "WorkableHitPrefix");
                ok &= PatchDamage(typeof(Breakable), "HitPrefix");
                ok &= PatchDamage(typeof(BreakableLock), "HitPrefix");
                ok &= PatchDamage(typeof(SpawnItemOnBreak), "HitPrefix");
                return ok;
            }
            catch (Exception e)
            {
                CorePatcher.DisableOwner(Weapons.HitOwner);
                CoreLog.For(Weapons.HitOwner).Error("weapon hit hook install failed: " + e.ToString());
                return false;
            }
        }

        private static bool PatchDamage(Type type, string prefix)
        {
            return CorePatcher.PatchCore(type, "Damage", DamageArgs, CorePatcher.Handler(typeof(WeaponsPatches), prefix), null, null, true, Weapons.HitOwner, Priority.First);
        }

        private static bool Hook(string owner, Type type, string name, Type[] args, string prefix, string postfix)
        {
            return CorePatcher.PatchCore(type, name, args, CorePatcher.Handler(typeof(WeaponsPatches), prefix), CorePatcher.Handler(typeof(WeaponsPatches), postfix), null, true, owner);
        }

        private static void PressPrefix(OS.Items.ItemAmmodata ___ammo, out float __state)
        {
            __state = ___ammo == null ? 0f : ___ammo.ammo;
        }

        private static void PressPostfix(WieldableItem.Detonator __instance, float __state)
        {
            try
            {
                if (__state <= 0f || !Weapons.WatchesFired) return;
                WeaponInfo weapon = Weapons.Describe(__instance);
                if (weapon != null) Weapons.RaiseFired(weapon);
            }
            catch (Exception e)
            {
                ApiLog.Once(Weapons.Owner, "Fired", e);
            }
        }

        private static void ReloadPostfix(WieldableItem.Detonator __instance)
        {
            if (!Weapons.WatchesReload) return;
            try
            {
                WeaponInfo weapon = Weapons.Describe(__instance);
                if (weapon != null) Weapons.RaiseReloaded(weapon);
            }
            catch (Exception e)
            {
                ApiLog.Once(Weapons.Owner, "Reloaded", e);
            }
        }

        private static void EnableHitPostfix(WieldableItem.MeleeWeapon ___meleeWeapon)
        {
            if (!Weapons.WatchesMelee) return;
            try
            {
                WeaponInfo weapon = Weapons.Describe(___meleeWeapon);
                if (weapon != null) Weapons.RaiseMeleeSwung(weapon);
            }
            catch (Exception e)
            {
                ApiLog.Once(Weapons.Owner, "MeleeSwung", e);
            }
        }

        private static void MeleeHitPrefix(WieldableItem.MeleeWeapon __instance)
        {
            hitter = __instance;
        }

        private static void MeleeHitFinalizer()
        {
            hitter = null;
        }

        private static void HitPrefix(Component __instance, ref int damage, DamageReceiver.DamageType type)
        {
            try
            {
                Weapons.ScaleHit(hitter, __instance, true, ref damage, type);
            }
            catch (Exception e)
            {
                ApiLog.Once(Weapons.Owner, "Hit", e);
            }
        }

        private static void WorkableHitPrefix(WorkableResourceSource __instance, int ___currentStock, ref int damage, DamageReceiver.DamageType type)
        {
            try
            {
                Weapons.ScaleHit(hitter, __instance, ___currentStock > 0, ref damage, type);
            }
            catch (Exception e)
            {
                ApiLog.Once(Weapons.Owner, "Hit", e);
            }
        }
    }
}
