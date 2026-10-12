using System;
using System.Reflection;
using UnityEngine;

namespace ObenseuerCore
{
    internal static class HousingPatches
    {
        private static bool rentDue;

        internal static bool Install()
        {
            try
            {
                bool ok = Hook(typeof(TenementController), "MoveResident", new[] { typeof(TenementApartment) }, null, "MoveInPostfix");
                ok &= Hook(typeof(TenementController), "EvictResident", new[] { typeof(TenementApartment), typeof(bool), typeof(bool), typeof(bool) }, "EvictPrefix", "EvictPostfix");
                ok &= Hook(typeof(TenementController), "UpdateRentPayment", new[] { typeof(int) }, "RentPrefix", "RentPostfix");
                ok &= Hook(typeof(DifficultyInfo), "GetRentPayment", new[] { typeof(TimeAndDay) }, null, "RentDuePostfix");
                ok &= Hook(typeof(TenementEvent), "OnTrigger", new[] { typeof(int) }, null, "EventPostfix");
                ok &= Hook(typeof(FurnitureManager), "PlacedFurniture", new[] { typeof(GameObject) }, null, "PlacedPostfix");
                ok &= Hook(typeof(FurnitureManager), "TookFurniture", new[] { typeof(GameObject) }, null, "TookPostfix");
                return ok;
            }
            catch (Exception e)
            {
                ApiLog.Once(Housing.Owner, "install", e);
                return false;
            }
        }

        private static bool Hook(Type type, string name, Type[] args, string prefix, string postfix)
        {
            return CorePatcher.PatchCore(type, name, args, CorePatcher.Handler(typeof(HousingPatches), prefix), CorePatcher.Handler(typeof(HousingPatches), postfix), null, false, Housing.Owner);
        }

        private static void MoveInPostfix(TenementApartment apartment)
        {
            try
            {
                TenantInfo tenant = Housing.ActiveTenant(apartment);
                if (tenant != null) Housing.RaiseTenantMovedIn(new TenantArgs(apartment.apartmentNumber, tenant));
            }
            catch (Exception e)
            {
                ApiLog.Once(Housing.Owner, "MoveResident postfix", e);
            }
        }

        private static void EvictPrefix(TenementApartment apartment, out TenantInfo __state)
        {
            __state = null;
            try
            {
                __state = Housing.ActiveTenant(apartment);
            }
            catch (Exception e)
            {
                ApiLog.Once(Housing.Owner, "EvictResident prefix", e);
            }
        }

        private static void EvictPostfix(TenementApartment apartment, TenantInfo __state)
        {
            try
            {
                if (__state != null && Housing.ActiveTenant(apartment) == null) Housing.RaiseTenantMovedOut(new TenantArgs(apartment.apartmentNumber, __state));
            }
            catch (Exception e)
            {
                ApiLog.Once(Housing.Owner, "EvictResident postfix", e);
            }
        }

        private static void RentPrefix()
        {
            rentDue = false;
        }

        private static void RentDuePostfix(bool __result)
        {
            rentDue = __result;
        }

        private static void RentPostfix()
        {
            if (!rentDue) return;
            rentDue = false;
            try
            {
                int oc = 0;
                float rm = 0f;
                foreach (TenementApartment apartment in TenementController.instance.apartments)
                {
                    oc += apartment.GetOcIncome();
                    rm += apartment.GetRmIncome();
                }
                Housing.RaiseRentPaid(new RentPayment(oc, rm));
            }
            catch (Exception e)
            {
                ApiLog.Once(Housing.Owner, "UpdateRentPayment postfix", e);
            }
        }

        private static void EventPostfix(TenementEvent __instance, int targetNPC)
        {
            try
            {
                int type = (int)__instance.type;
                TenementEventKind kind = Enum.IsDefined(typeof(TenementEventKind), type) ? (TenementEventKind)type : TenementEventKind.None;
                Housing.RaiseTenementEventStarted(new TenementEventArgs(__instance.id, kind, targetNPC));
            }
            catch (Exception e)
            {
                ApiLog.Once(Housing.Owner, "OnTrigger postfix", e);
            }
        }

        private static void PlacedPostfix(FurnitureManager __instance, GameObject f)
        {
            try
            {
                if (f != null) Building.RaiseFurniturePlaced(Building.Describe(__instance.GUID, f));
            }
            catch (Exception e)
            {
                ApiLog.Once(Housing.Owner, "PlacedFurniture postfix", e);
            }
        }

        private static void TookPostfix(FurnitureManager __instance, GameObject f)
        {
            try
            {
                if (f != null) Building.RaiseFurnitureRemoved(Building.Describe(__instance.GUID, f));
            }
            catch (Exception e)
            {
                ApiLog.Once(Housing.Owner, "TookFurniture postfix", e);
            }
        }
    }
}
