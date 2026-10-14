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
                bool ok = Hook(Housing.Owner, typeof(TenementController), "MoveResident", new[] { typeof(TenementApartment) }, null, "MoveInPostfix");
                ok &= Hook(Housing.Owner, typeof(TenementController), "EvictResident", new[] { typeof(TenementApartment), typeof(bool), typeof(bool), typeof(bool) }, "EvictPrefix", "EvictPostfix");
                return ok & Hook(Housing.Owner, typeof(TenementEvent), "OnTrigger", new[] { typeof(int) }, null, "EventPostfix");
            }
            catch (Exception e)
            {
                CorePatcher.DisableOwner(Housing.Owner);
                ApiLog.Once(Housing.Owner, "install", e);
                return false;
            }
        }

        internal static bool InstallRent()
        {
            try
            {
                bool rent = Hook(Housing.RentOwner, typeof(TenementController), "UpdateRentPayment", new[] { typeof(int) }, "RentPrefix", "RentPostfix");
                rent &= Hook(Housing.RentOwner, typeof(DifficultyInfo), "GetRentPayment", new[] { typeof(TimeAndDay) }, null, "RentDuePostfix");
                return rent;
            }
            catch (Exception e)
            {
                CorePatcher.DisableOwner(Housing.RentOwner);
                ApiLog.Once(Housing.Owner, "install rent", e);
                return false;
            }
        }

        internal static bool InstallFurniture()
        {
            try
            {
                bool placed = Hook(Housing.FurnitureOwner, typeof(FurnitureManager), "PlacedFurniture", new[] { typeof(GameObject) }, null, "PlacedPostfix");
                bool took = Hook(Housing.FurnitureOwner, typeof(FurnitureManager), "TookFurniture", new[] { typeof(GameObject) }, null, "TookPostfix");
                bool built = Hook(Housing.FurnitureOwner, typeof(BuildingSystem), "Place", Type.EmptyTypes, "BuildPrefix", null, "BuildFinalizer");
                bool info = Hook(Housing.FurnitureOwner, typeof(FurniturePlaceable), "SetFurnitureInfo", new[] { typeof(BuildingSystem.FurnitureInfo), typeof(FurnitureManager) }, null, "SetInfoPostfix");
                return placed & took & built & info;
            }
            catch (Exception e)
            {
                CorePatcher.DisableOwner(Housing.FurnitureOwner);
                ApiLog.Once(Housing.Owner, "install furniture", e);
                return false;
            }
        }

        internal static bool InstallEviction()
        {
            try
            {
                bool started = Hook(Housing.EvictionOwner, typeof(TenementController), "StartEviction", new[] { typeof(TenementApartment), typeof(float) }, "EvictionPrefix", null, "StartEvictionFinalizer");
                return started & Hook(Housing.EvictionOwner, typeof(TenementController), "CancelEviction", new[] { typeof(TenementApartment) }, "EvictionPrefix", null, "CancelEvictionFinalizer");
            }
            catch (Exception e)
            {
                CorePatcher.DisableOwner(Housing.EvictionOwner);
                ApiLog.Once(Housing.Owner, "install eviction", e);
                return false;
            }
        }

        private static bool Hook(string owner, Type type, string name, Type[] args, string prefix, string postfix)
        {
            return Hook(owner, type, name, args, prefix, postfix, null);
        }

        private static bool Hook(string owner, Type type, string name, Type[] args, string prefix, string postfix, string finalizer)
        {
            return CorePatcher.PatchCore(type, name, args, CorePatcher.Handler(typeof(HousingPatches), prefix), CorePatcher.Handler(typeof(HousingPatches), postfix), CorePatcher.Handler(typeof(HousingPatches), finalizer), true, owner);
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

        private static void BuildPrefix()
        {
            Building.Placing = true;
            Building.Placed = null;
        }

        private static void BuildFinalizer(Exception __exception)
        {
            try
            {
                FurniturePlaceable placed = Building.Placed;
                if (__exception == null && placed != null && placed.manager != null) Building.RaiseFurniturePlaced(Building.Describe(placed.manager.GUID, placed.gameObject));
            }
            catch (Exception e)
            {
                ApiLog.Once(Housing.Owner, "BuildingSystem.Place finalizer", e);
            }
            finally
            {
                Building.Placing = false;
                Building.Placed = null;
            }
        }

        private static void SetInfoPostfix(FurniturePlaceable __instance)
        {
            if (Building.Placing) Building.Placed = __instance;
        }

        private static void EvictionPrefix(TenementApartment apartment, out bool __state)
        {
            __state = false;
            try
            {
                __state = apartment != null && apartment.evictionStarted;
            }
            catch (Exception e)
            {
                ApiLog.Once(Housing.Owner, "eviction prefix", e);
            }
        }

        private static void StartEvictionFinalizer(TenementApartment apartment, bool __state)
        {
            try
            {
                if (__state || SaveGate.Busy || apartment == null || !apartment.evictionStarted) return;
                TenantInfo tenant = Housing.ActiveTenant(apartment);
                if (tenant != null) Housing.RaiseEvictionStarted(new TenantArgs(apartment.apartmentNumber, tenant));
            }
            catch (Exception e)
            {
                ApiLog.Once(Housing.Owner, "StartEviction finalizer", e);
            }
        }

        private static void CancelEvictionFinalizer(TenementApartment apartment, bool __state)
        {
            try
            {
                if (!__state || SaveGate.Busy || apartment == null || apartment.evictionStarted) return;
                TenantInfo tenant = Housing.ActiveTenant(apartment);
                if (tenant != null) Housing.RaiseEvictionCancelled(new TenantArgs(apartment.apartmentNumber, tenant));
            }
            catch (Exception e)
            {
                ApiLog.Once(Housing.Owner, "CancelEviction finalizer", e);
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
