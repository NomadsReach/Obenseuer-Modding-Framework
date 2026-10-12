using System;
using System.Reflection;

namespace ObenseuerCore
{
    internal static class WeaponsPatches
    {
        internal static bool Install()
        {
            try
            {
                bool ok = Hook(typeof(WieldableItem.Detonator), "Onpressed", Type.EmptyTypes, "PressPrefix", "PressPostfix");
                ok &= Hook(typeof(WieldableItem.Detonator), "Reload", new[] { typeof(float) }, null, "ReloadPostfix");
                ok &= Hook(typeof(WieldableItem.MeleeAnimationEvents), "EnableHit", Type.EmptyTypes, null, "EnableHitPostfix");
                return ok;
            }
            catch (Exception e)
            {
                CoreLog.For(Weapons.Owner).Error("weapon hook install failed: " + e.ToString());
                return false;
            }
        }

        private static bool Hook(Type type, string name, Type[] args, string prefix, string postfix)
        {
            return CorePatcher.PatchCore(type, name, args, CorePatcher.Handler(typeof(WeaponsPatches), prefix), CorePatcher.Handler(typeof(WeaponsPatches), postfix), null, true, Weapons.Owner);
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
    }
}
