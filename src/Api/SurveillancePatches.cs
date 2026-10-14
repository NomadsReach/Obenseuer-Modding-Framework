using System;
using System.Reflection;
using UnityEngine;

namespace ObenseuerCore
{
    internal static class SurveillancePatches
    {
        private const string WitnessOwner = Surveillance.Owner + ".witness";
        private const string TrespassOwner = Surveillance.Owner + ".trespass";

        internal static SurveillanceGroup Install()
        {
            try
            {
                bool witness = Hook(WitnessOwner, typeof(Witness), "Witnessed", new[] { typeof(bool), typeof(WitnessType) }, "WitnessedPostfix");
                bool trespass = Hook(TrespassOwner, typeof(TrespassingArea), "OnTriggerStay", new[] { typeof(Collider) }, "TrespassStayPostfix");
                trespass &= Hook(TrespassOwner, typeof(TrespassingArea), "OnTriggerExit", new[] { typeof(Collider) }, "TrespassExitPostfix");
                return (witness ? SurveillanceGroup.Witness : SurveillanceGroup.None) | (trespass ? SurveillanceGroup.Trespass : SurveillanceGroup.None);
            }
            catch (Exception e)
            {
                CoreLog.For(Surveillance.Owner).Error("surveillance hook install failed: " + e.ToString());
                return SurveillanceGroup.None;
            }
        }

        private static bool Hook(string owner, Type type, string name, Type[] args, string postfix)
        {
            return CorePatcher.PatchCore(type, name, args, null, CorePatcher.Handler(typeof(SurveillancePatches), postfix), null, true, owner);
        }

        private static void WitnessedPostfix(Witness __instance, bool __result, bool invoke)
        {
            try
            {
                if (__result && invoke && Surveillance.SpotWatched) Surveillance.Spot(__instance);
            }
            catch (Exception e)
            {
                ApiLog.Once(Surveillance.Owner, "Witnessed postfix", e);
            }
        }

        private static void TrespassStayPostfix(TrespassingArea __instance, Collider other, bool ___trespassing)
        {
            try
            {
                if (___trespassing && Surveillance.TrespassWatched && other.CompareTag("Player")) Surveillance.Enter(__instance, other.transform.position);
            }
            catch (Exception e)
            {
                ApiLog.Once(Surveillance.Owner, "OnTriggerStay postfix", e);
            }
        }

        private static void TrespassExitPostfix(TrespassingArea __instance, Collider other)
        {
            try
            {
                if (other.CompareTag("Player")) Surveillance.Leave(__instance);
            }
            catch (Exception e)
            {
                ApiLog.Once(Surveillance.Owner, "OnTriggerExit postfix", e);
            }
        }
    }
}
