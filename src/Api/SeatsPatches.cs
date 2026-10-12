using System;
using UnityEngine;

namespace ObenseuerCore
{
    internal static class SeatsPatches
    {
        internal static bool Install()
        {
            try
            {
                return CorePatcher.PatchCore(typeof(InteractableChair), "Sit", new[] { typeof(bool), typeof(bool) }, null, CorePatcher.Handler(typeof(SeatsPatches), "SitPostfix"), null, true, Seats.Owner)
                    && CorePatcher.PatchCore(typeof(InteractableChair), "StopSitEnd", Type.EmptyTypes, CorePatcher.Handler(typeof(SeatsPatches), "StopSitPrefix"), CorePatcher.Handler(typeof(SeatsPatches), "StopSitPostfix"), null, true, Seats.Owner)
                    && CorePatcher.PatchCore(typeof(InteractableChair), "NPCSit", new[] { typeof(GameObject), typeof(bool) }, null, CorePatcher.Handler(typeof(SeatsPatches), "NpcSitPostfix"), null, true, Seats.Owner)
                    && CorePatcher.PatchCore(typeof(InteractableChair), "StopNPCSit", new[] { typeof(GameObject) }, null, CorePatcher.Handler(typeof(SeatsPatches), "NpcStopSitPostfix"), null, true, Seats.Owner);
            }
            catch (Exception e)
            {
                ApiLog.Once(Seats.Owner, "install", e);
                return false;
            }
        }

        private static void StopSitPrefix(InteractableChair __instance, out bool __state)
        {
            __state = Seats.HasHandlers && __instance.PlayerSittingHere;
        }

        private static void SitPostfix(InteractableChair __instance, bool onLoadGame)
        {
            if (onLoadGame || !Seats.HasHandlers || SaveGate.Loading) return;
            try
            {
                Seats.RaiseSat(__instance, true);
            }
            catch (Exception e)
            {
                ApiLog.Once(Seats.Owner, "Sit postfix", e);
            }
        }

        private static void StopSitPostfix(InteractableChair __instance, bool __state)
        {
            if (!__state || SaveGate.Loading) return;
            try
            {
                Seats.RaiseLeft(__instance, true);
            }
            catch (Exception e)
            {
                ApiLog.Once(Seats.Owner, "StopSitEnd postfix", e);
            }
        }

        private static void NpcSitPostfix(InteractableChair __instance, bool instantly)
        {
            if (instantly || !Seats.HasHandlers || SaveGate.Loading) return;
            try
            {
                Seats.RaiseSat(__instance, false);
            }
            catch (Exception e)
            {
                ApiLog.Once(Seats.Owner, "NPCSit postfix", e);
            }
        }

        private static void NpcStopSitPostfix(InteractableChair __instance)
        {
            if (!Seats.HasHandlers || SaveGate.Loading) return;
            try
            {
                Seats.RaiseLeft(__instance, false);
            }
            catch (Exception e)
            {
                ApiLog.Once(Seats.Owner, "StopNPCSit postfix", e);
            }
        }
    }
}
