using System;
using System.Reflection;
using UnityEngine;

namespace ObenseuerCore
{
    internal static class LawPatches
    {
        private static int lastSpottedFrame = -1;

        internal static bool Install()
        {
            try
            {
                bool ok = Hook(typeof(Crime), "CommitCrime", new[] { typeof(NPC.NPCReference), typeof(int), typeof(Crime.CrimeType), typeof(bool) }, "CommitPrefix", "CommitPostfix", true);
                Hook(typeof(Crime), "PayFine", Type.EmptyTypes, "PayFinePrefix", "PayFinePostfix", false);
                Hook(typeof(Witness), "IsWitnessed", new[] { typeof(bool), typeof(WitnessType) }, null, "SpottedPostfix", false);
                Hook(typeof(Prison), "Release", Type.EmptyTypes, "ReleasePrefix", "ReleasePostfix", false);
                return ok;
            }
            catch (Exception e)
            {
                ApiLog.Once(Law.Owner, "install", e);
                return false;
            }
        }

        private static bool Hook(Type type, string name, Type[] args, string prefix, string postfix, bool core)
        {
            return CorePatcher.PatchCore(type, name, args, CorePatcher.Handler(typeof(LawPatches), prefix), CorePatcher.Handler(typeof(LawPatches), postfix), null, core, Law.Owner);
        }

        private static bool CommitPrefix(NPC.NPCReference victim, ref int severity, ref Crime.CrimeType type, ref bool silent, out CrimeArgs __state)
        {
            __state = null;
            try
            {
                CrimeArgs args = new CrimeArgs((CrimeKind)(int)type, severity, victim == null || victim.ID <= 0 ? -1 : victim.ID, silent);
                Law.RunInterceptors(args);
                __state = args;
                if (args.Cancel) return false;
                if (args.Severity != severity) args.Severity = Math.Max(1, Math.Min(args.Severity, Law.MaxSeverity));
                severity = args.Severity;
                if (!Enum.IsDefined(typeof(CrimeKind), args.Kind) || args.Kind == CrimeKind.None) args.Kind = (CrimeKind)(int)type;
                type = (Crime.CrimeType)(int)args.Kind;
                silent = args.Silent;
                return true;
            }
            catch (Exception e)
            {
                __state = null;
                ApiLog.Once(Law.Owner, "CommitCrime prefix", e);
                return true;
            }
        }

        private static void CommitPostfix(CrimeArgs __state)
        {
            try
            {
                Law.OnCommitted(__state);
            }
            catch (Exception e)
            {
                ApiLog.Once(Law.Owner, "CommitCrime postfix", e);
            }
        }

        private static void PayFinePrefix(Crime __instance, out int __state)
        {
            __state = -1;
            try
            {
                __state = (int)Math.Round(__instance.GetCrimeFineAmount());
            }
            catch (Exception e)
            {
                ApiLog.Once(Law.Owner, "PayFine prefix", e);
            }
        }

        private static void PayFinePostfix(bool __result, int __state)
        {
            try
            {
                if (__result && __state > 0) Law.OnFined(__state);
            }
            catch (Exception e)
            {
                ApiLog.Once(Law.Owner, "PayFine postfix", e);
            }
        }

        private static void SpottedPostfix(bool __result, bool invoke, WitnessType type)
        {
            try
            {
                if (!invoke || !__result || type != WitnessType.Crime || Time.frameCount == lastSpottedFrame) return;
                lastSpottedFrame = Time.frameCount;
                Law.OnSpotted();
            }
            catch (Exception e)
            {
                ApiLog.Once(Law.Owner, "IsWitnessed postfix", e);
            }
        }

        private static void ReleasePrefix(Prison __instance, out bool __state)
        {
            __state = false;
            try
            {
                __state = Law.PrisonFlag(__instance);
            }
            catch (Exception e)
            {
                ApiLog.Once(Law.Owner, "Release prefix", e);
            }
        }

        private static void ReleasePostfix(Prison __instance, bool __state)
        {
            try
            {
                if (__state && !Law.PrisonFlag(__instance)) Law.OnReleased();
            }
            catch (Exception e)
            {
                ApiLog.Once(Law.Owner, "Release postfix", e);
            }
        }
    }
}
